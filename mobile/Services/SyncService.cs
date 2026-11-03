using System.Net;
using FleetTrack.Mobile.Models;

namespace FleetTrack.Mobile.Services;

public record SyncResult(int Pending, int Discarded);

/// <summary>
/// Vacia la cola de operaciones pendientes en orden. Se detiene en el primer fallo de red (sigue offline) para no reordenar;
/// descarta las que el servidor rechaza de forma definitiva (4xx), porque reintentarlas nunca va a funcionar.
/// </summary>
public class SyncService(LocalDatabase db, RouteService routes)
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task EnqueueAsync(PendingOperation operation) => await db.EnqueueAsync(operation);

    public async Task<SyncResult> FlushAsync()
    {
        // Un solo vaciado a la vez: evita enviar dos veces la misma operacion si el timer y la reconexion coinciden.
        if (!await _gate.WaitAsync(0))
        {
            return new SyncResult(await db.CountPendingAsync(), 0);
        }

        var discarded = 0;
        try
        {
            foreach (var operation in await db.GetPendingAsync())
            {
                try
                {
                    await SendAsync(operation);
                    await Completed(operation);
                }
                catch (HttpRequestException ex) when (IsPermanent(ex.StatusCode))
                {
                    discarded++;
                    await Completed(operation);
                }
                catch (FileNotFoundException)
                {
                    discarded++;
                    await db.RemovePendingAsync(operation);
                }
                catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException)
                {
                    operation.Attempts++;
                    operation.LastError = ex.Message;
                    await db.UpdatePendingAsync(operation);
                    break;
                }
            }
        }
        finally
        {
            _gate.Release();
        }

        return new SyncResult(await db.CountPendingAsync(), discarded);
    }

    private async Task Completed(PendingOperation operation)
    {
        await db.RemovePendingAsync(operation);
        if (operation.FilePath is not null && File.Exists(operation.FilePath))
        {
            File.Delete(operation.FilePath);
        }
    }

    private Task SendAsync(PendingOperation op) => op.Kind switch
    {
        OperationKind.StartDelivery => routes.StartDeliveryAsync(op.DeliveryId!.Value),
        OperationKind.CompleteDelivery => routes.CompleteDeliveryAsync(op.DeliveryId!.Value),
        OperationKind.FailDelivery => routes.FailDeliveryAsync(op.DeliveryId!.Value, op.Reason),
        OperationKind.UploadEvidence => UploadAsync(op),
        OperationKind.Location => routes.SendLocationAsync(op.Latitude, op.Longitude, op.EventId,
            new DateTimeOffset(DateTime.SpecifyKind(op.RecordedAtUtc, DateTimeKind.Utc))),
        _ => throw new InvalidOperationException($"Operacion desconocida: {op.Kind}")
    };

    private async Task UploadAsync(PendingOperation op)
    {
        await using var stream = File.OpenRead(op.FilePath!);
        await routes.UploadEvidenceAsync(op.DeliveryId!.Value, stream, op.ContentType ?? "image/jpeg", Path.GetFileName(op.FilePath!));
    }

    // 401 (token vencido), 408 y 429 se reintentan; el resto de 4xx son definitivos (ownership, conflicto, dato invalido).
    private static bool IsPermanent(HttpStatusCode? status) =>
        status is >= HttpStatusCode.BadRequest and < HttpStatusCode.InternalServerError
        && status is not (HttpStatusCode.Unauthorized or HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests);
}

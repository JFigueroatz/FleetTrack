using System.Security.Claims;
using FleetTrack.Api.Persistence;
using FleetTrack.Domain;
using FleetTrack.Domain.Entities;
using FleetTrack.Domain.Enums;
using FleetTrack.Domain.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FleetTrack.Api.Controllers;

public record DeliveryStatusResponse(Guid Id, string Status);
public record FailDeliveryRequest(string? Reason);

[ApiController]
[Route("api/deliveries")]
[Authorize(Roles = Roles.Driver)]
public class DeliveriesController(FleetTrackDbContext db, IWebHostEnvironment env) : ControllerBase
{
    private static readonly HashSet<string> AllowedEvidenceTypes = ["image/jpeg", "image/png"];
    private const long MaxEvidenceBytes = 5 * 1024 * 1024;

    [HttpPost("{id:guid}/start")]
    public Task<ActionResult<DeliveryStatusResponse>> Start(Guid id) =>
        TransitionAsync(id, (d, now) =>
        {
            if (d.Status != DeliveryStatus.Pending)
            {
                return null;
            }

            var route = d.Route!;
            d.Status = DeliveryStatus.InProgress;
            d.StartedAt = now;
            db.Enqueue(EventTypes.DeliveryStarted, new DeliveryStarted(d.Id, route.Id, route.DriverId, now), now);

            if (route.Status == RouteStatus.Planned)
            {
                route.Status = RouteStatus.InProgress;
                db.Enqueue(EventTypes.RouteStarted, new RouteStarted(route.Id, route.DriverId, now), now);
            }
            return null;
        });

    [HttpPost("{id:guid}/complete")]
    public Task<ActionResult<DeliveryStatusResponse>> Complete(Guid id) =>
        TransitionAsync(id, (d, now) =>
        {
            // Idempotente: una segunda llamada (reenvio por reconexion o doble clic concurrente) no vuelve a procesar ni a emitir el evento.
            if (d.Status == DeliveryStatus.Completed)
            {
                return null;
            }
            if (d.Status == DeliveryStatus.Failed)
            {
                return "La entrega ya fue marcada como fallida.";
            }

            d.Status = DeliveryStatus.Completed;
            d.CompletedAt = now;
            db.Enqueue(EventTypes.DeliveryCompleted, new DeliveryCompleted(d.Id, d.RouteId, d.Route!.DriverId, now), now);
            return null;
        });

    [HttpPost("{id:guid}/fail")]
    public Task<ActionResult<DeliveryStatusResponse>> Fail(Guid id, [FromBody] FailDeliveryRequest request) =>
        TransitionAsync(id, (d, now) =>
        {
            if (d.Status == DeliveryStatus.Failed)
            {
                return null;
            }
            if (d.Status == DeliveryStatus.Completed)
            {
                return "La entrega ya fue completada.";
            }

            d.Status = DeliveryStatus.Failed;
            d.CompletedAt = now;
            db.Enqueue(EventTypes.DeliveryFailed, new DeliveryFailed(d.Id, d.RouteId, d.Route!.DriverId, request.Reason ?? string.Empty, now), now);
            return null;
        });

    [HttpPost("{id:guid}/evidence")]
    [RequestSizeLimit(MaxEvidenceBytes)]
    public async Task<IActionResult> UploadEvidence(Guid id, IFormFile file)
    {
        var delivery = await LoadOwnedDeliveryAsync(id);
        if (delivery is null)
        {
            return Forbid();
        }

        if (file.Length == 0 || file.Length > MaxEvidenceBytes || !AllowedEvidenceTypes.Contains(file.ContentType))
        {
            return BadRequest("Evidencia invalida: solo JPEG/PNG hasta 5 MB.");
        }

        // Idempotente: el movil puede reenviar la foto tras un corte; ya hay evidencia, no se guarda otra copia.
        if (await db.DeliveryEvidences.AnyAsync(e => e.DeliveryId == delivery.Id))
        {
            return Ok();
        }

        var extension = file.ContentType == "image/png" ? ".png" : ".jpg";
        var fileName = $"{Guid.NewGuid()}{extension}";
        var evidenceDir = Path.Combine(env.ContentRootPath, "evidence");
        Directory.CreateDirectory(evidenceDir);

        await using (var stream = System.IO.File.Create(Path.Combine(evidenceDir, fileName)))
        {
            await file.CopyToAsync(stream);
        }

        db.DeliveryEvidences.Add(new DeliveryEvidence
        {
            Id = Guid.NewGuid(),
            DeliveryId = delivery.Id,
            FileName = fileName,
            ContentType = file.ContentType,
            SizeBytes = file.Length
        });
        await db.SaveChangesAsync();

        return Ok();
    }

    /// <summary>
    /// Aplica un cambio de estado bajo bloqueo de la fila de la ruta, para que dos peticiones concurrentes
    /// sobre la misma entrega se serialicen y solo la primera emita evento. El cambio y el evento (outbox) van en una transaccion.
    /// <paramref name="apply"/> devuelve un mensaje de conflicto, o null si aplico el cambio o no habia nada que hacer.
    /// </summary>
    private async Task<ActionResult<DeliveryStatusResponse>> TransitionAsync(Guid id, Func<Delivery, DateTimeOffset, string?> apply)
    {
        var delivery = await LoadOwnedDeliveryAsync(id);
        if (delivery is null)
        {
            return Forbid();
        }

        await using var tx = await db.Database.BeginTransactionAsync();
        var routeId = delivery.RouteId;
        await db.Database.ExecuteSqlAsync($"""SELECT 1 FROM "Routes" WHERE "Id" = {routeId} FOR UPDATE""");
        await db.Entry(delivery).ReloadAsync();
        await db.Entry(delivery.Route!).ReloadAsync();

        var conflict = apply(delivery, DateTimeOffset.UtcNow);
        if (conflict is not null)
        {
            return Conflict(conflict);
        }

        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return Ok(new DeliveryStatusResponse(delivery.Id, delivery.Status.ToString()));
    }

    private async Task<Delivery?> LoadOwnedDeliveryAsync(Guid deliveryId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

        return await db.Deliveries
            .Include(d => d.Route)
            .Where(d => d.Id == deliveryId && d.Route!.Driver!.UserId == userId)
            .FirstOrDefaultAsync();
    }
}

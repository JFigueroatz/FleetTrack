using SQLite;

namespace FleetTrack.Mobile.Models;

public enum OperationKind
{
    StartDelivery,
    CompleteDelivery,
    FailDelivery,
    UploadEvidence,
    Location
}

/// <summary>
/// Operacion hecha por el conductor que aun no se confirmo en el servidor. Se reenvia en orden de Id.
/// EventId (ubicaciones) hace el reenvio idempotente en la API; start/complete/fail/evidencia ya lo son por estado.
/// </summary>
public class PendingOperation
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public OperationKind Kind { get; set; }
    public Guid? DeliveryId { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public Guid EventId { get; set; } = Guid.NewGuid();
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;

    public string? FilePath { get; set; }
    public string? ContentType { get; set; }
    public string? Reason { get; set; }

    public int Attempts { get; set; }
    public string? LastError { get; set; }
}

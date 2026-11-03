namespace FleetTrack.Domain.Entities;

public class DeliveryEvidence
{
    public Guid Id { get; set; }
    public Guid DeliveryId { get; set; }
    public Delivery? Delivery { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public long SizeBytes { get; set; }
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
}

namespace FleetTrack.Domain.Entities;

public class Vehicle
{
    public Guid Id { get; set; }
    public required string Plate { get; set; }
    public required string Model { get; set; }
    public int CapacityKg { get; set; }
}

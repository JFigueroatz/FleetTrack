namespace FleetTrack.Domain.Entities;

public class Customer
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Address { get; set; }
    public string? Phone { get; set; }
}

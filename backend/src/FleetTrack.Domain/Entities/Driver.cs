namespace FleetTrack.Domain.Entities;

public class Driver
{
    public Guid Id { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public string? LicenseNumber { get; set; }
}

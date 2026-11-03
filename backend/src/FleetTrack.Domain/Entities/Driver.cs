namespace FleetTrack.Domain.Entities;

public class Driver
{
    public Guid Id { get; set; }

    /// <summary>Cuenta de Identity con la que el conductor inicia sesion.</summary>
    public Guid UserId { get; set; }

    public required string FullName { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public string? LicenseNumber { get; set; }
}

using Microsoft.AspNetCore.Identity;

namespace FleetTrack.Domain.Entities;

public class AppUser : IdentityUser<Guid>
{
    public required string FullName { get; set; }
}

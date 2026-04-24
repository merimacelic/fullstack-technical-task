using Microsoft.AspNetCore.Identity;

namespace TaskManagement.Infrastructure.Identity;

// Identity-backed user persisted via EF Core. Lives in Infrastructure so the Domain
// layer remains independent of ASP.NET Core Identity.
public sealed class ApplicationUser : IdentityUser<Guid>
{
    public DateTime CreatedAtUtc { get; set; }
}

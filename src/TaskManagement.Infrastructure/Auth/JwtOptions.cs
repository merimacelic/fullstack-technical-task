using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    [MinLength(32, ErrorMessage = "JWT signing key must be at least 32 characters (256 bits).")]
    public string SecretKey { get; set; } = string.Empty;

    [Required]
    public string Issuer { get; set; } = "TaskManagement.Api";

    [Required]
    public string Audience { get; set; } = "TaskManagement.Client";

    [Range(1, 1440)]
    public int AccessTokenMinutes { get; set; } = 15;

    [Range(1, 60)]
    public int RefreshTokenDays { get; set; } = 7;
}

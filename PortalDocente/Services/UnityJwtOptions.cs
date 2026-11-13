namespace PortalDocente.Services;

public sealed class UnityJwtOptions
{
    public const string SectionName = "Authentication:Jwt";

    public string Issuer { get; set; } = "PortalDocente";
    public string Audience { get; set; } = "GestionInteligente.Unity";
    public string? SigningKey { get; set; }
    public int TokenLifetimeMinutes { get; set; } = 60;
}

public sealed record UnityIssuedToken(string AccessToken, DateTimeOffset ExpiresAt);

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PortalDocente.Security;

namespace PortalDocente.Services;

public sealed class UnityTokenService : IUnityTokenService
{
    private readonly UnityJwtOptions _options;

    public UnityTokenService(IOptions<UnityJwtOptions> options)
    {
        _options = options.Value;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.SigningKey)
        && Encoding.UTF8.GetByteCount(_options.SigningKey) >= 32;

    public UnityIssuedToken Issue(UsuarioEstudianteAutenticado estudiante)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("La firma JWT de Unity no está configurada.");

        var ahora = DateTimeOffset.UtcNow;
        var expira = ahora.AddMinutes(Math.Clamp(_options.TokenLifetimeMinutes, 5, 120));
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, estudiante.UsuarioId.ToString()),
            new Claim(ClaimTypes.Name, estudiante.NombreCompleto),
            new Claim(ClaimTypes.Email, estudiante.Correo),
            new Claim(ClaimTypes.Role, ClaimsPrincipalExtensions.RolEstudiante),
            new Claim("estudiante_id", estudiante.EstudianteId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: ahora.UtcDateTime,
            expires: expira.UtcDateTime,
            signingCredentials: credentials);

        return new UnityIssuedToken(new JwtSecurityTokenHandler().WriteToken(token), expira);
    }
}

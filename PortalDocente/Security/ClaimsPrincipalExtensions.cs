using System.Security.Claims;

namespace PortalDocente.Security;

public static class ClaimsPrincipalExtensions
{
    public const string DocenteIdClaim = "docente_id";
    public const string RolDocente = "docente";

    public static bool TryGetDocenteId(this ClaimsPrincipal user, out Guid docenteId)
    {
        docenteId = Guid.Empty;
        var valor = user.FindFirstValue(DocenteIdClaim);
        return Guid.TryParse(valor, out docenteId);
    }
}

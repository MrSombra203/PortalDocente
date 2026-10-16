using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using PortalDocente.Data;

namespace PortalDocente.Security;

public sealed class ValidarUsuarioCookieEvents : CookieAuthenticationEvents
{
    private readonly PortalDbContext _db;

    public ValidarUsuarioCookieEvents(PortalDbContext db)
    {
        _db = db;
    }

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var id = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(id, out var usuarioId))
        {
            await RechazarAsync(context);
            return;
        }

        var usuario = await _db.Usuarios
            .AsNoTracking()
            .Include(u => u.Docente)
            .Include(u => u.Estudiante)
            .Include(u => u.Roles)
                .ThenInclude(ur => ur.Rol)
            .SingleOrDefaultAsync(u => u.Id == usuarioId && u.Activo);

        if (usuario == null || usuario.Roles.Count != 1)
        {
            await RechazarAsync(context);
            return;
        }

        var rol = usuario.Roles.Single().Rol.Nombre;
        if (!ClaimsPrincipalExtensions.EsRolPortalValido(rol)
            || rol == ClaimsPrincipalExtensions.RolDocente && usuario.Docente == null
            || rol == ClaimsPrincipalExtensions.RolEstudiante && usuario.Estudiante == null)
        {
            await RechazarAsync(context);
            return;
        }

        var principalActual = context.Principal!;
        var rolesActuales = principalActual.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray();
        var docenteIdActual = principalActual.FindFirstValue(ClaimsPrincipalExtensions.DocenteIdClaim);
        var docenteIdEsperado = rol == ClaimsPrincipalExtensions.RolDocente
            ? usuario.Docente!.Id.ToString()
            : null;
        if (rolesActuales is [var rolActual]
            && rolActual == rol
            && principalActual.FindFirstValue(ClaimTypes.Name) == usuario.NombreCompleto
            && principalActual.FindFirstValue(ClaimTypes.Email) == usuario.Correo
            && docenteIdActual == docenteIdEsperado)
        {
            return;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.NombreCompleto),
            new(ClaimTypes.Email, usuario.Correo),
            new(ClaimTypes.Role, rol)
        };
        if (rol == ClaimsPrincipalExtensions.RolDocente)
            claims.Add(new Claim(ClaimsPrincipalExtensions.DocenteIdClaim, usuario.Docente!.Id.ToString()));

        context.ReplacePrincipal(new ClaimsPrincipal(new ClaimsIdentity(
            claims,
            context.Scheme.Name,
            ClaimTypes.Name,
            ClaimTypes.Role)));
        context.ShouldRenew = true;
    }

    private static async Task RechazarAsync(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(context.Scheme.Name);
    }
}

using Microsoft.EntityFrameworkCore;
using PortalDocente.Data;
using PortalDocente.Security;

namespace PortalDocente.Services;

public sealed class UsuarioAutenticacionService : IUsuarioAutenticacionService
{
    private const string ErrorCredenciales = "Credenciales incorrectas.";
    private const string ErrorEstudiante = "Los estudiantes no tienen acceso al Portal Docente.";
    private readonly PortalDbContext _db;

    public UsuarioAutenticacionService(PortalDbContext db)
    {
        _db = db;
    }

    public async Task<(UsuarioAutenticado? Usuario, string? Error)> AutenticarAsync(
        string correo, string password, CancellationToken cancellationToken = default)
    {
        var correoNormalizado = correo.Trim().ToLowerInvariant();
        var usuario = await _db.Usuarios
            .AsNoTracking()
            .Include(u => u.Docente)
            .Include(u => u.Estudiante)
            .Include(u => u.Roles)
                .ThenInclude(ur => ur.Rol)
            .FirstOrDefaultAsync(
                u => u.Correo.ToLower() == correoNormalizado && u.Activo,
                cancellationToken);

        if (usuario == null || usuario.Roles.Count != 1)
            return (null, ErrorCredenciales);

        var rol = usuario.Roles.Single().Rol.Nombre;
        if (!ClaimsPrincipalExtensions.EsRolPortalValido(rol)
            || rol == ClaimsPrincipalExtensions.RolDocente && usuario.Docente == null
            || rol == ClaimsPrincipalExtensions.RolEstudiante && usuario.Estudiante == null)
        {
            return (null, ErrorCredenciales);
        }

        bool claveValida;
        try
        {
            claveValida = BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            claveValida = false;
        }

        if (!claveValida)
            return (null, ErrorCredenciales);

        if (rol == ClaimsPrincipalExtensions.RolEstudiante)
            return (null, ErrorEstudiante);

        return (new UsuarioAutenticado(
            usuario.Id,
            usuario.Correo,
            usuario.NombreCompleto,
            rol,
            usuario.Docente?.Id), null);
    }
}

namespace PortalDocente.Services;

/// Contrato que verifica credenciales y devuelve identidad de sesión o un mensaje de rechazo.
public interface IUsuarioAutenticacionService
{
    Task<(UsuarioAutenticado? Usuario, string? Error)> AutenticarAsync(
        string correo, string password, CancellationToken cancellationToken = default);
}

/// <summary>Datos mínimos que el controlador convierte en claims de la cookie.</summary>
public sealed record UsuarioAutenticado(
    Guid Id,
    string Correo,
    string NombreCompleto,
    string Rol,
    Guid? DocenteId);

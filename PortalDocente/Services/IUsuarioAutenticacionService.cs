namespace PortalDocente.Services;

public interface IUsuarioAutenticacionService
{
    Task<(UsuarioAutenticado? Usuario, string? Error)> AutenticarAsync(
        string correo, string password, CancellationToken cancellationToken = default);

    Task<(UsuarioEstudianteAutenticado? Usuario, string? Error)> AutenticarEstudianteAsync(
        string correo, string password, CancellationToken cancellationToken = default);
}

public sealed record UsuarioAutenticado(
    Guid Id,
    string Correo,
    string NombreCompleto,
    string Rol,
    Guid? DocenteId);

public sealed record UsuarioEstudianteAutenticado(
    Guid UsuarioId,
    Guid EstudianteId,
    string Correo,
    string NombreCompleto);

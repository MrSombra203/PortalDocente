using PortalDocente.Models.ViewModels;

namespace PortalDocente.Services;

public interface IUsuarioAdministracionService
{
    Task<IReadOnlyList<UsuarioAdministracionItemViewModel>> ListarAsync(
        CancellationToken cancellationToken = default);

    Task<UsuarioAdministracionEditViewModel?> ObtenerAsync(
        Guid id, CancellationToken cancellationToken = default);

    Task<string?> CrearAsync(
        UsuarioAdministracionEditViewModel modelo, CancellationToken cancellationToken = default);

    Task<string?> ActualizarAsync(
        UsuarioAdministracionEditViewModel modelo, CancellationToken cancellationToken = default);

    Task<string?> EliminarAsync(
        Guid id, Guid administradorId, CancellationToken cancellationToken = default);
}

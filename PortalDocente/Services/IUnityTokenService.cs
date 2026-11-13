namespace PortalDocente.Services;

public interface IUnityTokenService
{
    bool IsConfigured { get; }
    UnityIssuedToken Issue(UsuarioEstudianteAutenticado estudiante);
}

namespace PortalDocente.Data.Entities;

/// <summary>Cuenta de acceso del portal, vinculada opcionalmente a un perfil docente o estudiante.</summary>
public class Usuario
{
    public Guid Id { get; set; }
    public string Correo { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public DateTimeOffset? UltimoAccesoEn { get; set; }
    public DateTimeOffset CreadoEn { get; set; }
    public DateTimeOffset ActualizadoEn { get; set; }

    public Estudiante? Estudiante { get; set; }
    public Docente? Docente { get; set; }
    public ICollection<UsuarioRol> Roles { get; set; } = new List<UsuarioRol>();
}

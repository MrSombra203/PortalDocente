namespace PortalDocente.Data.Entities;

/// <summary>Rol de acceso reconocido por el portal, relacionado con las cuentas mediante UsuarioRol.</summary>
public class Rol
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public DateTimeOffset CreadoEn { get; set; }

    public ICollection<UsuarioRol> Usuarios { get; set; } = new List<UsuarioRol>();
}

namespace PortalDocente.Data.Entities;

public class UsuarioRol
{
    public Guid UsuarioId { get; set; }
    public Guid RolId { get; set; }
    public DateTimeOffset AsignadoEn { get; set; }

    public Usuario Usuario { get; set; } = null!;
    public Rol Rol { get; set; } = null!;
}

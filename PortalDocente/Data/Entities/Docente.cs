namespace PortalDocente.Data.Entities;

/// <summary>Perfil docente asociado a una cuenta y propietario de sus cohortes.</summary>
public class Docente
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public string? TituloAcademico { get; set; }
    public DateTimeOffset CreadoEn { get; set; }

    public Usuario Usuario { get; set; } = null!;
}

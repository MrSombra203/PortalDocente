namespace PortalDocente.Data.Entities;

public class Estudiante
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public string? CodigoInstitucional { get; set; }
    public DateTimeOffset CreadoEn { get; set; }

    public Usuario Usuario { get; set; } = null!;
    public ICollection<CursoEstudiante> Matriculas { get; set; } = new List<CursoEstudiante>();
    public ICollection<Partida> Partidas { get; set; } = new List<Partida>();
}

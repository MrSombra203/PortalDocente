namespace PortalDocente.Models.ViewModels;

public class ProgresoEstudianteViewModel
{
    public Guid EstudianteId { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string? CodigoInstitucional { get; set; }
    public string? CursoCohorte { get; set; }
    public bool TieneProgreso { get; set; }
    public bool UsaDatosDemo { get; set; }

    public int TotalPartidas { get; set; }
    public int PartidasFinalizadas { get; set; }
    public DateTimeOffset? UltimaActividad { get; set; }
    public string? NivelAlcanzado { get; set; }
    public string? EscenarioAlcanzado { get; set; }
    public string? Dificultad { get; set; }

    public decimal? UltimoCostoTotal { get; set; }
    public decimal? UltimoPctEntregasATiempo { get; set; }
    public decimal? UltimaUtilizacionRecursos { get; set; }
    public decimal? UltimaEficienciaRutas { get; set; }
    public decimal? UltimaPuntuacionGlobal { get; set; }

    public decimal? PromedioPuntuacion { get; set; }
    public decimal? MejorPuntuacion { get; set; }

    public IReadOnlyList<PartidaHistorialItemViewModel> Historial { get; set; } = Array.Empty<PartidaHistorialItemViewModel>();
    public IReadOnlyList<EvolucionPuntoViewModel> Evolucion { get; set; } = Array.Empty<EvolucionPuntoViewModel>();
}

public class PartidaHistorialItemViewModel
{
    public Guid PartidaId { get; set; }
    public DateTimeOffset Fecha { get; set; }
    public int NivelNumero { get; set; }
    public string EscenarioNombre { get; set; } = string.Empty;
    public string Dificultad { get; set; } = string.Empty;
    public bool Finalizada { get; set; }
    public decimal? CostoTotal { get; set; }
    public decimal? PctEntregasATiempo { get; set; }
    public decimal? UtilizacionRecursos { get; set; }
    public decimal? EficienciaRutas { get; set; }
    public decimal? PuntuacionGlobal { get; set; }
}

public class EvolucionPuntoViewModel
{
    public DateTimeOffset Fecha { get; set; }
    public decimal Puntuacion { get; set; }
    public string Etiqueta { get; set; } = string.Empty;
}

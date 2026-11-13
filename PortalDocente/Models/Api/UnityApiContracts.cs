using System.ComponentModel.DataAnnotations;

namespace PortalDocente.Models.Api;

/// <summary>Credentials used by a student to sign in to the Unity game.</summary>
public sealed record UnityLoginRequest
{
    [Required, EmailAddress, StringLength(320)]
    public required string Correo { get; init; }

    [Required, StringLength(256)]
    public required string Password { get; init; }
}

/// <summary>Short-lived access token and student profile returned to Unity.</summary>
public sealed record UnityLoginResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    Guid EstudianteId,
    string Nombre,
    string Correo,
    string Rol);

/// <summary>Game result sent after a student completes a mission.</summary>
public sealed record RegistrarResultadoRequest
{
    [Required]
    public required Guid IdEstudiante { get; init; }

    [Required, StringLength(100)]
    public required string Escenario { get; init; }

    [Required, RegularExpression(@"^Nivel\s+[1-9][0-9]*$")]
    public required string Nivel { get; init; }

    [Required]
    public required DateTimeOffset? Fecha { get; init; }

    [Range(typeof(decimal), "0", "100")]
    public required decimal PuntajeGlobal { get; init; }

    [Range(typeof(decimal), "0", "100")]
    public required decimal EntregasATiempo { get; init; }

    [Range(typeof(decimal), "0", "100")]
    public required decimal UtilizacionRecursos { get; init; }

    [Range(typeof(decimal), "0", "100")]
    public required decimal EficienciaRutas { get; init; }

    [Range(typeof(decimal), "0", "9999999999.99")]
    public required decimal CostoTotal { get; init; }

    [Required]
    public required Guid IdempotencyKey { get; init; }
}

/// <summary>Confirmation returned after a game result is stored.</summary>
public sealed record ResultadoPartidaResponse(
    Guid PartidaId,
    string Escenario,
    string Nivel,
    DateTimeOffset Fecha,
    DateTimeOffset FechaRecepcion,
    decimal PuntajeGlobal,
    decimal EntregasATiempo,
    decimal UtilizacionRecursos,
    decimal EficienciaRutas,
    decimal CostoTotal,
    bool Duplicada);

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalDocente.Models.Api;
using PortalDocente.Security;
using PortalDocente.Services;

namespace PortalDocente.Controllers.Api;

[ApiController]
[Route("api/resultados")]
[Authorize(
    AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
    Roles = ClaimsPrincipalExtensions.RolEstudiante)]
[Produces("application/json")]
public sealed class ResultadosController : ControllerBase
{
    private readonly IResultadoRegistroService _registroService;
    private readonly ILogger<ResultadosController> _logger;

    public ResultadosController(
        IResultadoRegistroService registroService,
        ILogger<ResultadosController> logger)
    {
        _registroService = registroService;
        _logger = logger;
    }

    /// <summary>Stores the completed mission result for the authenticated student.</summary>
    [HttpPost("registrar")]
    [ProducesResponseType(typeof(ResultadoPartidaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResultadoPartidaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Registrar(
        RegistrarResultadoRequest request,
        CancellationToken cancellationToken)
    {
        if (request.IdEstudiante == Guid.Empty)
            ModelState.AddModelError(nameof(request.IdEstudiante), "El identificador del estudiante es obligatorio.");
        if (request.IdempotencyKey == Guid.Empty)
            ModelState.AddModelError(nameof(request.IdempotencyKey), "Se requiere una clave de idempotencia válida.");
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (!Guid.TryParse(User.FindFirstValue("estudiante_id"), out var estudianteId)
            || request.IdEstudiante != estudianteId
            || !Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
        {
            _logger.LogWarning("Una solicitud de Unity intentó registrar un resultado para otro estudiante.");
            return StatusCode(
                StatusCodes.Status403Forbidden,
                Problema("El identificador no corresponde al estudiante autenticado.", StatusCodes.Status403Forbidden));
        }

        var outcome = await _registroService.RegistrarAsync(usuarioId, request, cancellationToken);
        return outcome.Estado switch
        {
            RegistroResultadoEstado.Registrada =>
                CreatedAtAction(nameof(Obtener), new { id = outcome.Resultado!.PartidaId }, outcome.Resultado),
            RegistroResultadoEstado.Repetida => Ok(outcome.Resultado),
            RegistroResultadoEstado.EstudianteNoEncontrado => NotFound(Problema(
                "No se encontró el estudiante autenticado.", StatusCodes.Status404NotFound)),
            RegistroResultadoEstado.EscenarioNoDisponible => NotFound(Problema(
                "El escenario y nivel no están disponibles para este estudiante.", StatusCodes.Status404NotFound)),
            RegistroResultadoEstado.EscenarioAmbiguo => Conflict(Problema(
                "El escenario coincide con más de una cohorte activa.", StatusCodes.Status409Conflict)),
            RegistroResultadoEstado.ClaveIdempotenciaEnUso => Conflict(Problema(
                "La clave de idempotencia ya se utilizó con datos diferentes.", StatusCodes.Status409Conflict)),
            RegistroResultadoEstado.SolicitudInvalida => BadRequest(Problema(
                "La solicitud no contiene identificadores o una fecha válidos.", StatusCodes.Status400BadRequest)),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError)
        };
    }

    /// <summary>Returns a result belonging to the authenticated student.</summary>
    [HttpGet("{id:guid}", Name = nameof(Obtener))]
    [ProducesResponseType(typeof(ResultadoPartidaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue("estudiante_id"), out var estudianteId))
            return Unauthorized();

        var resultado = await _registroService.ObtenerAsync(estudianteId, id, cancellationToken);
        return resultado == null ? NotFound() : Ok(resultado);
    }

    private static ProblemDetails Problema(string detalle, int statusCode) =>
        new()
        {
            Title = statusCode switch
            {
                StatusCodes.Status404NotFound => "Resultado no encontrado",
                StatusCodes.Status403Forbidden => "Acceso denegado",
                StatusCodes.Status400BadRequest => "Solicitud inválida",
                _ => "Conflicto"
            },
            Detail = detalle,
            Status = statusCode
        };
}

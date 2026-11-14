using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalDocente.Infrastructure.HealthChecks;

namespace PortalDocente.Controllers.Api;

/// Reporta el estado agregado de PostgreSQL y de los servicios Neon configurados,
/// sin exponer este diagnóstico a una sesión autenticada.
[ApiController]
[Route("health/neon")]
[AllowAnonymous]
public sealed class NeonHealthController : ControllerBase
{
    private readonly NeonServicesHealthService _healthService;

    public NeonHealthController(NeonServicesHealthService healthService)
    {
        _healthService = healthService;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var services = await _healthService.CheckAsync(cancellationToken);
        var statusCode = services.Values.All(status => status == "ok")
            ? StatusCodes.Status200OK
            : StatusCodes.Status503ServiceUnavailable;
        return StatusCode(statusCode, new { services });
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalDocente.Security;
using PortalDocente.Services;

namespace PortalDocente.Controllers;

[Authorize(Roles = ClaimsPrincipalExtensions.RolDocente)]
public class EstudiantesController : Controller
{
    private readonly IProgresoConsultaService _progresoService;

    public EstudiantesController(IProgresoConsultaService progresoService)
    {
        _progresoService = progresoService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q, int pagina = 1, CancellationToken cancellationToken = default)
    {
        if (!User.TryGetDocenteId(out var docenteId))
            return Forbid();

        var model = await _progresoService.ListarEstudiantesAsync(docenteId, q, pagina, cancellationToken);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Progreso(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
            return NotFound();

        if (!User.TryGetDocenteId(out var docenteId))
            return Forbid();

        var model = await _progresoService.ObtenerProgresoAsync(docenteId, id, cancellationToken);
        if (model == null)
            return NotFound();

        return View(model);
    }
}

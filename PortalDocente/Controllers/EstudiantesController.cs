using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using PortalDocente.Services;

namespace PortalDocente.Controllers;

[Authorize(Roles = "docente")]
public class EstudiantesController : Controller
{
    private readonly IProgresoConsultaService _progresoService;

    public EstudiantesController(IProgresoConsultaService progresoService)
    {
        _progresoService = progresoService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue("docente_id"), out var docenteId))
            return Forbid();

        var model = await _progresoService.ListarEstudiantesAsync(docenteId, q, cancellationToken);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Progreso(Guid id, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue("docente_id"), out var docenteId))
            return Forbid();

        var model = await _progresoService.ObtenerProgresoAsync(docenteId, id, cancellationToken);
        if (model == null)
            return NotFound();

        return View(model);
    }
}

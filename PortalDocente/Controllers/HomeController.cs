using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalDocente.Models;
using PortalDocente.Services;

namespace PortalDocente.Controllers;

[Authorize(Roles = "docente")]
public class HomeController : Controller
{
    private readonly IProgresoConsultaService _progresoService;

    public HomeController(IProgresoConsultaService progresoService)
    {
        _progresoService = progresoService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue("docente_id"), out var docenteId))
            return Forbid();

        var model = await _progresoService.ObtenerDashboardAsync(docenteId, cancellationToken);
        return View(model);
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

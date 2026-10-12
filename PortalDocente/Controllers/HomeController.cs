using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalDocente.Models;
using PortalDocente.Security;
using PortalDocente.Services;

namespace PortalDocente.Controllers;

[Authorize(Roles = ClaimsPrincipalExtensions.RolDocente)]
public class HomeController : Controller
{
    private readonly IProgresoConsultaService _progresoService;

    public HomeController(IProgresoConsultaService progresoService)
    {
        _progresoService = progresoService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!User.TryGetDocenteId(out var docenteId))
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

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PortalDocente.Models.ViewModels;
using PortalDocente.Security;
using PortalDocente.Services;

namespace PortalDocente.Controllers;

/// Expone la gestión de cuentas solo a administradores y prepara los datos auxiliares
/// que necesitan los formularios; las reglas de persistencia pertenecen al servicio.
[Authorize(Roles = ClaimsPrincipalExtensions.RolAdmin)]
[Route("Admin/Usuarios")]
public class AdminUsuariosController : Controller
{
    private readonly IUsuarioAdministracionService _usuariosService;

    public AdminUsuariosController(IUsuarioAdministracionService usuariosService)
    {
        _usuariosService = usuariosService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var usuarios = await _usuariosService.ListarAsync(cancellationToken);
        return View(usuarios);
    }

    [HttpGet("Crear")]
    public async Task<IActionResult> Crear(CancellationToken cancellationToken)
    {
        PrepararRoles();
        var modelo = new UsuarioAdministracionEditViewModel
        {
            Rol = ClaimsPrincipalExtensions.RolDocente
        };
        await PrepararCohortesAsync(modelo, null, cancellationToken);
        return View(modelo);
    }

    [HttpPost("Crear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(
        UsuarioAdministracionEditViewModel modelo, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            PrepararRoles();
            await PrepararCohortesAsync(modelo, null, cancellationToken);
            return View(modelo);
        }

        var error = await _usuariosService.CrearAsync(modelo, cancellationToken);
        if (error != null)
        {
            ModelState.AddModelError(string.Empty, error);
            PrepararRoles();
            await PrepararCohortesAsync(modelo, null, cancellationToken);
            return View(modelo);
        }

        TempData["Mensaje"] = "La cuenta se creó correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:guid}/Editar")]
    public async Task<IActionResult> Editar(Guid id, CancellationToken cancellationToken)
    {
        var modelo = await _usuariosService.ObtenerAsync(id, cancellationToken);
        if (modelo == null)
            return NotFound();

        PrepararRoles();
        await PrepararCohortesAsync(modelo, id, cancellationToken);
        return View(modelo);
    }

    [HttpPost("{id:guid}/Editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(
        Guid id, UsuarioAdministracionEditViewModel modelo, CancellationToken cancellationToken)
    {
        if (id != modelo.Id)
            return BadRequest();

        if (!ModelState.IsValid)
        {
            PrepararRoles();
            await PrepararCohortesAsync(modelo, id, cancellationToken);
            return View(modelo);
        }

        var error = await _usuariosService.ActualizarAsync(modelo, cancellationToken);
        if (error != null)
        {
            ModelState.AddModelError(string.Empty, error);
            PrepararRoles();
            await PrepararCohortesAsync(modelo, id, cancellationToken);
            return View(modelo);
        }

        TempData["Mensaje"] = "La cuenta se actualizó correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:guid}/Eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var administradorId))
            return Forbid();

        var error = await _usuariosService.EliminarAsync(id, administradorId, cancellationToken);
        if (error != null)
            TempData["Error"] = error;
        else
            TempData["Mensaje"] = "La cuenta se eliminó correctamente.";

        return RedirectToAction(nameof(Index));
    }

    private void PrepararRoles()
    {
        ViewBag.Roles = new[]
        {
            new SelectListItem("Administrador", ClaimsPrincipalExtensions.RolAdmin),
            new SelectListItem("Docente", ClaimsPrincipalExtensions.RolDocente),
            new SelectListItem("Estudiante", ClaimsPrincipalExtensions.RolEstudiante)
        };
    }

    private async Task PrepararCohortesAsync(
        UsuarioAdministracionEditViewModel modelo, Guid? usuarioId, CancellationToken cancellationToken)
    {
        // El servicio excluye de la lista las cohortes que ya pertenecen al docente editado.
        modelo.CohortesDisponibles =
            await _usuariosService.CargarCohortesDisponiblesAsync(usuarioId, cancellationToken);
    }
}

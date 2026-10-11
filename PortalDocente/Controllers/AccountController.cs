using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalDocente.Data;
using PortalDocente.Models.ViewModels;
using PortalDocente.Services;

namespace PortalDocente.Controllers;

public class AccountController : Controller
{
    private readonly IProgresoConsultaService _progresoService;
    private readonly IConfiguration _configuration;

    public AccountController(IProgresoConsultaService progresoService, IConfiguration configuration)
    {
        _progresoService = progresoService;
        _configuration = configuration;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        var usaDemo = _configuration.GetValue("Demo:UseInMemoryData", true);
        return View(new LoginViewModel
        {
            MensajeDemo = usaDemo
                ? $"Acceso temporal de demo: {DemoDataSeeder.DemoDocenteCorreo} / {DemoDataSeeder.DemoDocentePassword}"
                : "Ingresa con una cuenta docente registrada en la base central."
        });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        model.MensajeDemo = _configuration.GetValue("Demo:UseInMemoryData", true)
            ? $"Acceso temporal de demo: {DemoDataSeeder.DemoDocenteCorreo} / {DemoDataSeeder.DemoDocentePassword}"
            : null;

        if (!ModelState.IsValid)
            return View(model);

        var (ok, docenteId, nombre, error) = await _progresoService.ValidarDocenteAsync(
            model.Correo.Trim(), model.Password, cancellationToken);
        if (!ok || docenteId is null)
        {
            ModelState.AddModelError(string.Empty, error ?? "No se pudo iniciar sesión.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, nombre ?? model.Correo),
            new(ClaimTypes.Email, model.Correo.Trim()),
            new(ClaimTypes.Role, "docente"),
            new("docente_id", docenteId.Value.ToString()),
            new("demo_temporal", _configuration.GetValue("Demo:UseInMemoryData", true) ? "true" : "false")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = false,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(4)
            });

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    [Authorize(Roles = "docente")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }
}

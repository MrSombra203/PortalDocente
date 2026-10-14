using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PortalDocente.Security;
using PortalDocente.Models.ViewModels;
using PortalDocente.Services;

namespace PortalDocente.Controllers;

public class AccountController : Controller
{
    private readonly IProgresoConsultaService _progresoService;

    public AccountController(IProgresoConsultaService progresoService)
    {
        _progresoService = progresoService;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null, int? limitado = null)
    {
        if (User.Identity?.IsAuthenticated == true && User.IsInRole(ClaimsPrincipalExtensions.RolDocente))
            return RedirectToAction("Index", "Home");

        var model = new LoginViewModel();
        if (limitado == 1)
            ModelState.AddModelError(string.Empty, "Demasiados intentos de ingreso. Espera unos minutos e inténtalo de nuevo.");

        ViewData["ReturnUrl"] = returnUrl;
        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        var (ok, usuarioId, docenteId, correo, nombre, rol, error) = await _progresoService.ValidarDocenteAsync(
            model.Correo, model.Password, cancellationToken);
        if (!ok
            || usuarioId is null
            || docenteId is null
            || string.IsNullOrWhiteSpace(correo)
            || string.IsNullOrWhiteSpace(nombre)
            || string.IsNullOrWhiteSpace(rol))
        {
            ModelState.AddModelError(string.Empty, error ?? "No se pudo iniciar sesión.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuarioId.Value.ToString()),
            new(ClaimTypes.Name, nombre),
            new(ClaimTypes.Email, correo),
            new(ClaimTypes.Role, rol),
            new(ClaimsPrincipalExtensions.DocenteIdClaim, docenteId.Value.ToString())
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

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }
}

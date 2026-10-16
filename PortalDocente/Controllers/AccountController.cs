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
    private readonly IUsuarioAutenticacionService _autenticacionService;

    public AccountController(IUsuarioAutenticacionService autenticacionService)
    {
        _autenticacionService = autenticacionService;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null, int? limitado = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirigirSegunRol();

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

        var (usuario, error) = await _autenticacionService.AutenticarAsync(
            model.Correo, model.Password, cancellationToken);
        if (usuario == null)
        {
            ModelState.AddModelError(string.Empty, error ?? "No se pudo iniciar sesión.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.NombreCompleto),
            new(ClaimTypes.Email, usuario.Correo),
            new(ClaimTypes.Role, usuario.Rol)
        };
        if (usuario.DocenteId.HasValue)
            claims.Add(new Claim(ClaimsPrincipalExtensions.DocenteIdClaim, usuario.DocenteId.Value.ToString()));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = false,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(4)
            });

        if (usuario.Rol == ClaimsPrincipalExtensions.RolAdmin)
            return RedirectToAction("Index", "AdminUsuarios");

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        if (User.Identity?.IsAuthenticated == true)
            Response.StatusCode = StatusCodes.Status403Forbidden;

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

    private IActionResult RedirigirSegunRol()
    {
        if (User.IsInRole(ClaimsPrincipalExtensions.RolAdmin))
            return RedirectToAction("Index", "AdminUsuarios");

        if (User.IsInRole(ClaimsPrincipalExtensions.RolDocente))
            return RedirectToAction("Index", "Home");

        return RedirectToAction(nameof(AccessDenied));
    }
}

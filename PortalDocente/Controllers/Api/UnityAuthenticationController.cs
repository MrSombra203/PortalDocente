using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PortalDocente.Models.Api;
using PortalDocente.Services;

namespace PortalDocente.Controllers.Api;

[ApiController]
[Route("api/unity")]
[AllowAnonymous]
[Produces("application/json")]
public sealed class UnityAuthenticationController : ControllerBase
{
    private readonly IUsuarioAutenticacionService _autenticacionService;
    private readonly IUnityTokenService _tokenService;

    public UnityAuthenticationController(
        IUsuarioAutenticacionService autenticacionService,
        IUnityTokenService tokenService)
    {
        _autenticacionService = autenticacionService;
        _tokenService = tokenService;
    }

    /// <summary>Authenticates a student and issues a short-lived access token for Unity.</summary>
    [HttpPost("login")]
    [EnableRateLimiting("unity-login")]
    [ProducesResponseType(typeof(UnityLoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Login(
        UnityLoginRequest request,
        CancellationToken cancellationToken)
    {
        if (!_tokenService.IsConfigured)
        {
            return Problem(
                title: "La autenticación del videojuego no está configurada.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var (estudiante, _) = await _autenticacionService.AutenticarEstudianteAsync(
            request.Correo,
            request.Password,
            cancellationToken);
        if (estudiante == null)
            return Problem(
                title: "Las credenciales no son válidas para el acceso al videojuego.",
                statusCode: StatusCodes.Status401Unauthorized);

        var token = _tokenService.Issue(estudiante);
        return Ok(new UnityLoginResponse(
            token.AccessToken,
            token.ExpiresAt,
            estudiante.EstudianteId,
            estudiante.NombreCompleto,
            estudiante.Correo,
            "estudiante"));
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PortalDocente.Controllers;
using PortalDocente.Controllers.Api;
using PortalDocente.Data;
using PortalDocente.Security;
using PortalDocente.Tests.Services;
using Xunit;

namespace PortalDocente.Tests.Security;

public class AutorizacionRolesTests
{
    [Fact]
    public void ControladoresProtegidosDeclaranElRolPermitido()
    {
        Assert.Equal(
            ClaimsPrincipalExtensions.RolDocente,
            typeof(HomeController).GetCustomAttributes(typeof(AuthorizeAttribute), true)
                .Cast<AuthorizeAttribute>().Single().Roles);
        Assert.Equal(
            ClaimsPrincipalExtensions.RolDocente,
            typeof(EstudiantesController).GetCustomAttributes(typeof(AuthorizeAttribute), true)
                .Cast<AuthorizeAttribute>().Single().Roles);
        Assert.Equal(
            ClaimsPrincipalExtensions.RolAdmin,
            typeof(AdminUsuariosController).GetCustomAttributes(typeof(AuthorizeAttribute), true)
                .Cast<AuthorizeAttribute>().Single().Roles);
    }

    [Fact]
    public async Task ValidarUsuarioCookieEvents_ActualizaElRolDesdeLaBaseDeDatos()
    {
        var options = new DbContextOptionsBuilder<PortalDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new PortalDbContext(options);
        await ProgresoConsultaTestData.SeedAsync(db, 0);
        var adminRole = new PortalDocente.Data.Entities.Rol
        {
            Id = Guid.NewGuid(),
            Nombre = ClaimsPrincipalExtensions.RolAdmin,
            CreadoEn = DateTimeOffset.UtcNow
        };
        db.Roles.Add(adminRole);
        db.UsuariosRoles.Remove(await db.UsuariosRoles.SingleAsync());
        db.UsuariosRoles.Add(new PortalDocente.Data.Entities.UsuarioRol
        {
            UsuarioId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            RolId = adminRole.Id,
            AsignadoEn = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "11111111-1111-1111-1111-111111111111"),
            new Claim(ClaimTypes.Name, "Nombre anterior"),
            new Claim(ClaimTypes.Role, ClaimsPrincipalExtensions.RolDocente),
            new Claim(ClaimsPrincipalExtensions.DocenteIdClaim, ProgresoConsultaTestData.DocenteId.ToString())
        ],
        CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var scheme = new AuthenticationScheme(
            CookieAuthenticationDefaults.AuthenticationScheme,
            CookieAuthenticationDefaults.AuthenticationScheme,
            typeof(CookieAuthenticationHandler));
        var ticket = new AuthenticationTicket(principal, scheme.Name);
        var context = new CookieValidatePrincipalContext(
            new DefaultHttpContext(),
            scheme,
            new CookieAuthenticationOptions(),
            ticket);
        var events = new ValidarUsuarioCookieEvents(db);

        await events.ValidatePrincipal(context);

        Assert.NotNull(context.Principal);
        Assert.True(context.Principal!.IsInRole(ClaimsPrincipalExtensions.RolAdmin));
        Assert.False(context.Principal.IsInRole(ClaimsPrincipalExtensions.RolDocente));
        Assert.Null(context.Principal.FindFirst(ClaimsPrincipalExtensions.DocenteIdClaim));
        Assert.True(context.ShouldRenew);
    }

    [Fact]
    public void AccionesDeLoginYAccesoDenegadoPermitenUsuariosAnonimos()
    {
        var loginGet = typeof(AccountController).GetMethods()
            .Single(method => method.Name == nameof(AccountController.Login)
                              && method.GetParameters().Length == 2);
        Assert.NotNull(loginGet
            .GetCustomAttributes(typeof(AllowAnonymousAttribute), true).SingleOrDefault());
        var accesoDenegado = typeof(AccountController).GetMethod(nameof(AccountController.AccessDenied))!;
        Assert.NotNull(accesoDenegado
            .GetCustomAttributes(typeof(AllowAnonymousAttribute), true).SingleOrDefault());
    }
}

using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalDocente.Controllers;
using PortalDocente.Data.Entities;
using PortalDocente.Security;
using PortalDocente.Services;
using PortalDocente.Tests.Services;
using Xunit;

namespace PortalDocente.Tests.Controllers;

public class EstudiantesControllerTests
{
    [Fact]
    public async Task DescargarCsv_ExportaHistorialDelEstudianteSinModificarDatos()
    {
        var options = new DbContextOptionsBuilder<PortalDocente.Data.PortalDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new PortalDocente.Data.PortalDbContext(options);
        await ProgresoConsultaTestData.SeedAsync(db, 1);

        var cohorte = await db.Cohortes.SingleAsync();
        var estudiante = await db.Estudiantes.SingleAsync();
        var fecha = DateTimeOffset.UtcNow;
        var escenario = new Escenario
        {
            Id = Guid.NewGuid(),
            CohorteId = cohorte.Id,
            Nombre = "=SUM(1,2)",
            CreadoEn = fecha,
            ActualizadoEn = fecha
        };
        var nivel = new Nivel
        {
            Id = Guid.NewGuid(),
            EscenarioId = escenario.Id,
            Numero = 1
        };
        db.Escenarios.Add(escenario);
        db.Niveles.Add(nivel);
        db.Partidas.Add(new Partida
        {
            Id = Guid.NewGuid(),
            EstudianteId = estudiante.Id,
            NivelId = nivel.Id,
            FechaCreacion = fecha,
            FechaRecepcion = fecha,
            Finalizada = true
        });
        await db.SaveChangesAsync();
        var partidasAntes = await db.Partidas.CountAsync();

        var controller = new EstudiantesController(new ProgresoConsultaService(db));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimsPrincipalExtensions.DocenteIdClaim, ProgresoConsultaTestData.DocenteId.ToString()),
                    new Claim(ClaimTypes.Role, ClaimsPrincipalExtensions.RolDocente)
                ], "test"))
            }
        };

        var resultado = await controller.DescargarCsv(estudiante.Id, CancellationToken.None);

        var archivo = Assert.IsType<FileContentResult>(resultado);
        Assert.Equal("text/csv; charset=utf-8", archivo.ContentType);
        Assert.EndsWith(".csv", archivo.FileDownloadName);
        var contenido = Encoding.UTF8.GetString(archivo.FileContents).TrimStart('\uFEFF');
        Assert.Contains("Estudiante,Correo,Código institucional", contenido);
        Assert.Contains("\"'=SUM(1,2)\"", contenido);
        Assert.Contains("Finalizada", contenido);
        Assert.Equal(partidasAntes, await db.Partidas.CountAsync());
    }
}

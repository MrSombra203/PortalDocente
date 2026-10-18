using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalDocente.Security;
using PortalDocente.Services;

namespace PortalDocente.Controllers;

[Authorize(Roles = ClaimsPrincipalExtensions.RolDocente)]
public class EstudiantesController : Controller
{
    private readonly IProgresoConsultaService _progresoService;

    public EstudiantesController(IProgresoConsultaService progresoService)
    {
        _progresoService = progresoService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q, int pagina = 1, CancellationToken cancellationToken = default)
    {
        if (!User.TryGetDocenteId(out var docenteId))
            return Forbid();

        var model = await _progresoService.ListarEstudiantesAsync(docenteId, q, pagina, cancellationToken);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Progreso(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
            return NotFound();

        if (!User.TryGetDocenteId(out var docenteId))
            return Forbid();

        var model = await _progresoService.ObtenerProgresoAsync(docenteId, id, cancellationToken);
        if (model == null)
            return NotFound();

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> DescargarCsv(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
            return NotFound();

        if (!User.TryGetDocenteId(out var docenteId))
            return Forbid();

        var model = await _progresoService.ObtenerProgresoAsync(docenteId, id, cancellationToken);
        if (model == null)
            return NotFound();

        var csv = new StringBuilder();
        AgregarFilaCsv(csv,
            "Estudiante", "Correo", "Código institucional", "Curso / cohorte",
            "Total de partidas", "Partidas finalizadas", "Fecha de partida", "Escenario",
            "Nivel", "Dificultad", "Estado", "Costo total", "Entregas a tiempo (%)",
            "Utilización de recursos (%)", "Eficiencia de rutas (%)", "Puntuación global");

        if (model.Historial.Count == 0)
        {
            AgregarFilaCsv(csv,
                EscaparCampoCsv(model.NombreCompleto, protegerFormula: true),
                EscaparCampoCsv(model.Correo, protegerFormula: true),
                EscaparCampoCsv(model.CodigoInstitucional, protegerFormula: true),
                EscaparCampoCsv(model.CursoCohorte, protegerFormula: true),
                model.TotalPartidas.ToString(CultureInfo.InvariantCulture),
                model.PartidasFinalizadas.ToString(CultureInfo.InvariantCulture),
                string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
        }
        else
        {
            foreach (var partida in model.Historial)
            {
                AgregarFilaCsv(csv,
                    EscaparCampoCsv(model.NombreCompleto, protegerFormula: true),
                    EscaparCampoCsv(model.Correo, protegerFormula: true),
                    EscaparCampoCsv(model.CodigoInstitucional, protegerFormula: true),
                    EscaparCampoCsv(model.CursoCohorte, protegerFormula: true),
                    model.TotalPartidas.ToString(CultureInfo.InvariantCulture),
                    model.PartidasFinalizadas.ToString(CultureInfo.InvariantCulture),
                    partida.Fecha.ToString("O", CultureInfo.InvariantCulture),
                    EscaparCampoCsv(partida.EscenarioNombre, protegerFormula: true),
                    partida.NivelNumero.ToString(CultureInfo.InvariantCulture),
                    EscaparCampoCsv(partida.Dificultad, protegerFormula: true),
                    partida.Finalizada ? "Finalizada" : "En curso",
                    FormatearNumero(partida.CostoTotal),
                    FormatearNumero(partida.PctEntregasATiempo),
                    FormatearNumero(partida.UtilizacionRecursos),
                    FormatearNumero(partida.EficienciaRutas),
                    FormatearNumero(partida.PuntuacionGlobal));
            }
        }

        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        var contenido = encoding.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        var nombreArchivo = CrearNombreArchivo(model.NombreCompleto, model.EstudianteId);
        return File(contenido, "text/csv; charset=utf-8", nombreArchivo);
    }

    private static void AgregarFilaCsv(StringBuilder csv, params string?[] valores)
    {
        csv.AppendJoin(',', valores.Select(valor => valor ?? string.Empty));
        csv.Append("\r\n");
    }

    private static string EscaparCampoCsv(string? valor, bool protegerFormula = false)
    {
        valor ??= string.Empty;
        var primerCaracter = valor.FirstOrDefault(caracter => !char.IsWhiteSpace(caracter));
        if (protegerFormula && primerCaracter is '=' or '+' or '-' or '@' or '\t' or '\r')
            valor = "'" + valor;

        return $"\"{valor.Replace("\"", "\"\"")}\"";
    }

    private static string FormatearNumero(decimal? valor) =>
        valor?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    private static string CrearNombreArchivo(string nombreCompleto, Guid estudianteId)
    {
        var caracteresInvalidos = Path.GetInvalidFileNameChars();
        var nombreSeguro = new string(nombreCompleto
            .Where(caracter => !char.IsControl(caracter) && !caracteresInvalidos.Contains(caracter))
            .ToArray())
            .Trim(' ', '.');

        if (string.IsNullOrWhiteSpace(nombreSeguro))
            nombreSeguro = "estudiante";

        return $"progreso-{nombreSeguro}-{estudianteId.ToString("N")[..8]}.csv";
    }
}

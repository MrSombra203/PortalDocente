using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PortalDocente.Data;

namespace PortalDocente.Services;

public sealed class NeonServicesHealthService
{
    private const string FunctionResponse = "Hello from Neon Functions";
    private readonly PortalDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NeonServicesHealthService> _logger;

    public NeonServicesHealthService(
        PortalDbContext db,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<NeonServicesHealthService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IReadOnlyDictionary<string, string>> CheckAsync(
        CancellationToken cancellationToken)
    {
        var results = new Dictionary<string, string>
        {
            ["postgres"] = await CheckPostgresAsync(cancellationToken),
            ["auth"] = await CheckJwksAsync(cancellationToken),
            ["function"] = await CheckFunctionAsync(cancellationToken)
        };
        return results;
    }

    private async Task<string> CheckPostgresAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _db.Database.CanConnectAsync(cancellationToken) ? "ok" : "unavailable";
        }
        catch (NpgsqlException exception)
        {
            _logger.LogError(exception, "The Neon PostgreSQL health check failed.");
            return "unavailable";
        }
    }

    private async Task<string> CheckJwksAsync(CancellationToken cancellationToken)
    {
        var url = _configuration["NEON_AUTH_JWKS_URL"];
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            _logger.LogError("NEON_AUTH_JWKS_URL is missing or invalid.");
            return "not_configured";
        }

        try
        {
            using var response = await _httpClientFactory
                .CreateClient("NeonServicesHealth")
                .GetAsync(uri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("The Neon Auth JWKS endpoint returned HTTP {StatusCode}.",
                    (int)response.StatusCode);
                return "unavailable";
            }

            await using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(
                content, cancellationToken: cancellationToken);
            return document.RootElement.TryGetProperty("keys", out var keys)
                   && keys.ValueKind == JsonValueKind.Array
                ? "ok"
                : "invalid_response";
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "The Neon Auth JWKS health check failed.");
            return "unavailable";
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(exception, "The Neon Auth JWKS health check timed out.");
            return "timeout";
        }
        catch (JsonException exception)
        {
            _logger.LogError(exception, "The Neon Auth JWKS endpoint returned invalid JSON.");
            return "invalid_response";
        }
    }

    private async Task<string> CheckFunctionAsync(CancellationToken cancellationToken)
    {
        var url = _configuration["NEON_FUNCTION_HELLO_BASE_URL"];
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            _logger.LogError("NEON_FUNCTION_HELLO_BASE_URL is missing or invalid.");
            return "not_configured";
        }

        try
        {
            using var response = await _httpClientFactory
                .CreateClient("NeonServicesHealth")
                .GetAsync(uri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("The Neon Function health check returned HTTP {StatusCode}.",
                    (int)response.StatusCode);
                return "unavailable";
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return body == FunctionResponse ? "ok" : "invalid_response";
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "The Neon Function health check failed.");
            return "unavailable";
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(exception, "The Neon Function health check timed out.");
            return "timeout";
        }
    }
}

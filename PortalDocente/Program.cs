using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PortalDocente.Data;
using PortalDocente.Security;
using PortalDocente.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IProgresoConsultaService, ProgresoConsultaService>();
builder.Services.AddScoped<IUsuarioAdministracionService, UsuarioAdministracionService>();
builder.Services.AddScoped<IUsuarioAutenticacionService, UsuarioAutenticacionService>();
builder.Services.AddScoped<ValidarUsuarioCookieEvents>();

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "PortalDocente.AntiForgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

var connectionString = builder.Configuration.GetConnectionString("PortalCentral");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Configura ConnectionStrings:PortalCentral para iniciar el portal.");
}

builder.Services.AddSingleton(_ =>
{
    var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
    dataSourceBuilder.EnableUnmappedTypes();
    return dataSourceBuilder.Build();
});
builder.Services.AddDbContext<PortalDbContext>((services, options) =>
    options.UseNpgsql(services.GetRequiredService<NpgsqlDataSource>()));

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(4);
        options.Cookie.Name = "PortalDocente.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.EventsType = typeof(ValidarUsuarioCookieEvents);
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonimo",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 8,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0
            }));
    options.OnRejected = async (context, token) =>
    {
        if (context.HttpContext.Request.Path.StartsWithSegments("/Account/Login"))
        {
            context.HttpContext.Response.Redirect("/Account/Login?limitado=1");
            return;
        }

        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsync("Demasiados intentos. Espera unos minutos.", token);
    };
});

var app = builder.Build();

try
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<PortalDbContext>();
    if (!await db.Database.CanConnectAsync())
        app.Logger.LogWarning("PostgreSQL no está disponible. El portal permanecerá iniciado y registrará errores de solicitudes.");
}
catch (NpgsqlException exception)
{
    app.Logger.LogError(exception, "No fue posible conectar con PostgreSQL al iniciar. El portal permanecerá iniciado.");
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseExceptionHandler("/Home/Error");
app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        var ruta = context.Request.Path;
        var esEstatico = ruta.StartsWithSegments("/css")
                         || ruta.StartsWithSegments("/js")
                         || ruta.StartsWithSegments("/lib");
        context.Response.Headers.XContentTypeOptions = "nosniff";
        context.Response.Headers.XFrameOptions = "DENY";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        if (!esEstatico)
            context.Response.Headers.CacheControl = "no-store";
        return Task.CompletedTask;
    });
    await next();
});
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

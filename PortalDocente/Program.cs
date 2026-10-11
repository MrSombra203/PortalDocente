using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using PortalDocente.Data;
using PortalDocente.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IProgresoConsultaService, ProgresoConsultaService>();

var useInMemory = builder.Configuration.GetValue("Demo:UseInMemoryData", true);
var connectionString = builder.Configuration.GetConnectionString("PortalCentral");

if (!useInMemory && string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Configura ConnectionStrings:PortalCentral o habilita Demo:UseInMemoryData para iniciar el portal.");
}

builder.Services.AddDbContext<PortalDbContext>(options =>
{
    if (useInMemory)
    {
        options.UseInMemoryDatabase("PortalDocenteDemo");
    }
    else
    {
        options.UseNpgsql(connectionString);
    }
});

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(4);
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (useInMemory || string.IsNullOrWhiteSpace(connectionString))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PortalDbContext>();
    await DemoDataSeeder.SeedAsync(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

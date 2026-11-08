
using Npgsql;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Takvim.Api.Data;
using Takvim.Api.Models;

var builder = WebApplication.CreateBuilder(args);

// Controller ve OpenAPI
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// PostgreSQL bağlantısı
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Veritabanı bağlantısı bulunamadı.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// ASP.NET Core Identity
builder.Services
    .AddIdentity<AppUser, IdentityRole<int>>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;

        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(10);

        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

var app = builder.Build();

// OpenAPI
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// PostgreSQL bağlantı kontrolü
app.MapGet("/api/health/database", async (
    IConfiguration configuration,
    ILogger<Program> logger) =>
{
    var connectionString =
        configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return Results.Problem(
            "Veritabanı bağlantısı yapılandırılmamış.",
            statusCode: 503);
    }

    try
    {
        await using var connection =
            new NpgsqlConnection(connectionString);

        await connection.OpenAsync();

        await using var command =
            new NpgsqlCommand("SELECT 1", connection);

        var result = await command.ExecuteScalarAsync();

        if (Convert.ToInt32(result) != 1)
        {
            return Results.Problem(
                "Veritabanı sorgusu başarısız.",
                statusCode: 503);
        }

        return Results.Ok(new
        {
            status = "healthy",
            database = "connected"
        });
    }
    catch (Exception ex)
    {
        logger.LogError(
            ex, "Veritabanı bağlantısı başarısız.");

        return Results.Problem(
            "Veritabanına bağlanılamadı.",
            statusCode: 503);
    }
});

// Admin ve User rollerini oluştur

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole<int>>>();

    var userManager = scope.ServiceProvider
        .GetRequiredService<UserManager<AppUser>>();

    // Admin ve User rollerini oluştur
    string[] roles = ["Admin", "User"];

    foreach (var roleName in roles)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            var result = await roleManager.CreateAsync(
                new IdentityRole<int>(roleName));

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Rol oluşturulamadı: {roleName}");
            }
        }
    }

    // Admin hesabını yalnızca geliştirme ortamında oluştur
    if (app.Environment.IsDevelopment())
    {
        var adminEmail =
            app.Configuration["AdminSeed:Email"];

        var adminPassword =
            app.Configuration["AdminSeed:Password"];

        if (!string.IsNullOrWhiteSpace(adminEmail) &&
            !string.IsNullOrWhiteSpace(adminPassword))
        {
            var admin = await userManager.FindByEmailAsync(
                adminEmail);

            // Hesap mevcut değilse yeni Admin oluştur
            if (admin is null)
            {
                admin = new AppUser
                {
                    Name = "Takvim+ Yöneticisi",
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var createResult =
                    await userManager.CreateAsync(
                        admin, adminPassword);

                if (!createResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Admin hesabı oluşturulamadı: " +
                        string.Join(", ",
                            createResult.Errors.Select(
                                x => x.Description)));
                }

                var roleResult =
                    await userManager.AddToRoleAsync(
                        admin, "Admin");

                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Admin rolü atanamadı.");
                }
            }
            else
            {
                // Mevcut hesabın rolünü otomatik yükseltme
                if (!await userManager.IsInRoleAsync(
                    admin, "Admin"))
                {
                    throw new InvalidOperationException(
                        "Admin e-postası farklı bir hesaba ait.");
                }
            }
        }
    }
}

app.Run();


app.Run();

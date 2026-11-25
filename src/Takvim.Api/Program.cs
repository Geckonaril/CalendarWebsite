
using Npgsql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Takvim.Api.Data;
using Takvim.Api.Models;
using Takvim.Api.OpenApi;
using Takvim.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Controller ve OpenAPI
builder.Services.AddControllers();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

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

// JWT (Web uygulaması ve Swagger API'ye Bearer token ile erişir)
var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>() ?? new JwtOptions();

if (jwtOptions.Key.Length < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key yapılandırılmamış veya 32 karakterden kısa.");
}

builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));

builder.Services.AddSingleton<TokenService>();

// AddIdentity varsayılan şemayı çerez yapar; API için JWT'ye çeviriyoruz.
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = jwtOptions.GetSigningKey(),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = "name",
            RoleClaimType = "role"
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// OpenAPI
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Swagger UI: http://localhost:5055/swagger
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Takvim+ API v1");
        options.DocumentTitle = "Takvim+ API";
    });
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

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

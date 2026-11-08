using Microsoft.EntityFrameworkCore;
using Takvim.Api.Data;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.


var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Veritabanı bağlantısı bulunamadı.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));


builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();


app.MapGet("/api/health/database", async (
    IConfiguration configuration,
    ILogger<Program> logger) =>
{
    var connectionString =
        configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return Results.Problem(
            "Veritabani baglantisi yapilandirilmamis.",
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
                "Veritabani sorgusu basarisiz.",
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
        logger.LogError(ex, "Veritabani baglantisi basarisiz.");

        return Results.Problem(
            "Veritabanina baglanilamadi.",
            statusCode: 503);
    }
});


app.Run();

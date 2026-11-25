
using Microsoft.AspNetCore.Authentication.Cookies;
using Takvim.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// Web API bağlantısı (oturumdaki JWT otomatik eklenir)
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<ApiTokenHandler>();

builder.Services
    .AddHttpClient("TakvimApi", client =>
    {
        client.BaseAddress = new Uri(
            builder.Configuration["ApiBaseUrl"]
            ?? "http://localhost:5055");
    })
    .AddHttpMessageHandler<ApiTokenHandler>();

// AJAX istekleri antiforgery token'ı bu başlıkla gönderir
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});

// Kullanıcı oturumu
builder.Services
    .AddAuthentication("WebCookie")
    .AddCookie("WebCookie", options =>
    {
        options.Cookie.Name = "TakvimPlus.Session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;

        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";

        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// HTTP Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

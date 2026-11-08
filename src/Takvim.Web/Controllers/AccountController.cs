
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Takvim.Web.Controllers;

public class RegisterForm
{
    [Required(ErrorMessage = "Adınızı girin.")]
    public string Name { get; set; } = "";

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, MinLength(8)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required, Compare(nameof(Password))]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = "";
}

public class LoginForm
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";
}

public record ApiUser(
    int Id, string Name, string Email, string Role);

public class AccountController : Controller
{
    private readonly IHttpClientFactory _http;

    public AccountController(IHttpClientFactory http)
    {
        _http = http;
    }

    [AllowAnonymous, HttpGet]
    public IActionResult Register() => View(new RegisterForm());

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterForm form)
    {
        if (!ModelState.IsValid)
            return View(form);

        var client = _http.CreateClient("TakvimApi");

        try
        {
            var response = await client.PostAsJsonAsync(
                "/api/account/register",
                new { form.Name, form.Email, form.Password });

            
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();

                Console.WriteLine(
                $"Kayıt API yanıtı: {(int)response.StatusCode}");

                Console.WriteLine(errorBody);

                ModelState.AddModelError(
                    "",
                "Kayıt işlemi başarısız. Ayrıntılar sunucu terminalinde.");

            return View(form);
}

        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError("", "API bağlantısı kurulamadı.");
            return View(form);
        }

        TempData["Success"] = "Hesabınız oluşturuldu. Giriş yapabilirsiniz.";
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous, HttpGet]
    public IActionResult Login() => View(new LoginForm());

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginForm form)
    {
        if (!ModelState.IsValid)
            return View(form);

        var client = _http.CreateClient("TakvimApi");
        ApiUser? user;

        try
        {
            var response = await client.PostAsJsonAsync(
                "/api/account/login",
                new { form.Email, form.Password });

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError(
                    "", "Giriş başarısız. Bilgilerinizi kontrol edin.");
                return View(form);
            }

            user = await response.Content
                .ReadFromJsonAsync<ApiUser>();
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError("", "API bağlantısı kurulamadı.");
            return View(form);
        }

        if (user is null)
        {
            ModelState.AddModelError("", "Giriş yanıtı alınamadı.");
            return View(form);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        };

        var identity = new ClaimsIdentity(claims, "WebCookie");

        await HttpContext.SignInAsync(
            "WebCookie",
            new ClaimsPrincipal(identity));

        return RedirectToAction("Index", "Home");
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("WebCookie");
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();
}

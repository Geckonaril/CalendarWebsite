using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Takvim.Web.Controllers;

/// <summary>
/// Etkinlik sayfası ve tarayıcıdaki JavaScript için Takvim+ API'sine
/// giden etkinlik isteklerini oturum token'ı ile ileten uç noktalar.
/// </summary>
[Authorize]
[Route("Events")]
public class EventsController : Controller
{
    private readonly IHttpClientFactory _http;
    private readonly ILogger<EventsController> _logger;

    public EventsController(
        IHttpClientFactory http,
        ILogger<EventsController> logger)
    {
        _http = http;
        _logger = logger;
    }

    // Etkinliklerim sayfası
    [HttpGet("")]
    public IActionResult Index() => View();

    // GET /Events/Data?from=...&to=...
    [HttpGet("Data")]
    public Task<IActionResult> List(DateTime? from, DateTime? to)
    {
        var query = new List<string>();

        if (from is not null)
            query.Add("from=" + Uri.EscapeDataString(ToIso(from.Value)));

        if (to is not null)
            query.Add("to=" + Uri.EscapeDataString(ToIso(to.Value)));

        var url = "/api/events" +
            (query.Count > 0 ? "?" + string.Join("&", query) : "");

        return ForwardAsync(new HttpRequestMessage(HttpMethod.Get, url));
    }

    [HttpGet("Data/{id:int}")]
    public Task<IActionResult> Get(int id) =>
        ForwardAsync(new HttpRequestMessage(
            HttpMethod.Get, $"/api/events/{id}"));

    [HttpPost("Data"), ValidateAntiForgeryToken]
    public Task<IActionResult> Create([FromBody] JsonElement body) =>
        ForwardAsync(new HttpRequestMessage(HttpMethod.Post, "/api/events")
        {
            Content = JsonContent.Create(body)
        });

    [HttpPut("Data/{id:int}"), ValidateAntiForgeryToken]
    public Task<IActionResult> Update(int id, [FromBody] JsonElement body) =>
        ForwardAsync(new HttpRequestMessage(HttpMethod.Put, $"/api/events/{id}")
        {
            Content = JsonContent.Create(body)
        });

    [HttpDelete("Data/{id:int}"), ValidateAntiForgeryToken]
    public Task<IActionResult> Delete(int id) =>
        ForwardAsync(new HttpRequestMessage(
            HttpMethod.Delete, $"/api/events/{id}"));

    /// <summary>
    /// İsteği API'ye iletir; durum kodunu ve gövdeyi aynen döndürür.
    /// API token'ı geçersiz sayarsa web oturumu da kapatılır.
    /// </summary>
    private async Task<IActionResult> ForwardAsync(HttpRequestMessage request)
    {
        var client = _http.CreateClient("TakvimApi");
        HttpResponseMessage response;

        try
        {
            response = await client.SendAsync(
                request, HttpContext.RequestAborted);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Takvim+ API'sine bağlanılamadı.");

            return Problem(
                title: "API bağlantısı kurulamadı.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await HttpContext.SignOutAsync("WebCookie");

                return Problem(
                    title: "Oturumunuzun süresi doldu. Lütfen tekrar giriş yapın.",
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var body = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(body))
                return StatusCode((int)response.StatusCode);

            return new ContentResult
            {
                StatusCode = (int)response.StatusCode,
                Content = body,
                ContentType = response.Content.Headers.ContentType?.ToString()
                    ?? "application/json"
            };
        }
    }

    private static string ToIso(DateTime value) =>
        (value.Kind == DateTimeKind.Local
            ? value.ToUniversalTime()
            : DateTime.SpecifyKind(value, DateTimeKind.Utc))
        .ToString("o");
}

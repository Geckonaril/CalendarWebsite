using System.Net.Http.Headers;

namespace Takvim.Web.Services;

/// <summary>
/// Oturum çerezinde saklanan API erişim token'ını
/// TakvimApi isteklerine "Authorization: Bearer" olarak ekler.
/// </summary>
public class ApiTokenHandler : DelegatingHandler
{
    public const string TokenClaimType = "api_access_token";

    private readonly IHttpContextAccessor _httpContext;

    public ApiTokenHandler(IHttpContextAccessor httpContext)
    {
        _httpContext = httpContext;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = _httpContext.HttpContext?.User
            .FindFirst(TokenClaimType)?.Value;

        if (!string.IsNullOrEmpty(token) &&
            request.Headers.Authorization is null)
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}

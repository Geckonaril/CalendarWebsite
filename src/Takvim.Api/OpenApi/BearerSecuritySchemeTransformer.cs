using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Takvim.Api.OpenApi;

/// <summary>
/// Swagger arayüzüne "Authorize" (JWT Bearer) desteği ekler.
/// </summary>
internal sealed class BearerSecuritySchemeTransformer
    : IOpenApiDocumentTransformer
{
    private const string SchemeName = "Bearer";

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Info.Title = "Takvim+ API";
        document.Info.Description =
            "Takvim+ RESTful Web API. Korumalı uç noktaları denemek için " +
            "önce POST /api/account/login ile giriş yapın, dönen " +
            "accessToken değerini Authorize penceresine yapıştırın.";

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??=
            new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes[SchemeName] =
            new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Yalnızca token değerini girin (Bearer öneki olmadan)."
            };

        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(SchemeName, document)] = []
        });

        return Task.CompletedTask;
    }
}

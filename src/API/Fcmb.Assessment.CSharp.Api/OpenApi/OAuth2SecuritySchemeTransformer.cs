using Fcmb.Assessment.CSharp.Modules.Users.Infrastructure.Configurations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

namespace Fcmb.Assessment.CSharp.Api.OpenApi;

internal sealed class OAuth2SecuritySchemeTransformer(
    IAuthenticationSchemeProvider authenticationSchemeProvider,
    IOptions<KeyCloakSettings> keyCloak) : IOpenApiDocumentTransformer
{
    private readonly KeyCloakSettings _keyCloak = keyCloak.Value;

    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        IEnumerable<AuthenticationScheme> authenticationSchemes =
            await authenticationSchemeProvider.GetAllSchemesAsync();

        if (authenticationSchemes.Any(authScheme => authScheme.Name == "Bearer"))
        {
            var securitySchemes = new Dictionary<string, IOpenApiSecurityScheme>
            {
                ["Keycloak"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    Description = "Keycloak OpenID Connect Authentication",
                    Flows = new OpenApiOAuthFlows
                    {
                        AuthorizationCode = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = new Uri(_keyCloak.AuthorizationUrl),
                            TokenUrl = new Uri(_keyCloak.TokenUrl),
                            Scopes = new Dictionary<string, string>
                            {
                                ["openid"] = "OpenID Connect scope",
                                ["profile"] = "User profile access"
                            }
                        }
                    }
                }
            };

            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes = securitySchemes;

            foreach (KeyValuePair<HttpMethod, OpenApiOperation> operation in document.Paths.Values.SelectMany(path =>
                         path.Operations))
            {
                operation.Value.Security ??= [];
                operation.Value.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Keycloak", document)] = ["openid", "profile"]
                });
            }
        }
    }
}

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Knowledge.Mcp.Endpoints;

/// <summary>
/// OAuth 2.1 metadata endpoints for MCP authorization server discovery (RFC 9728)
/// </summary>
public static class WellKnownEndpoints
{
    /// <summary>
    /// Registers OAuth authorization server metadata endpoints
    /// </summary>
    /// <param name="app">The web application</param>
    /// <param name="auth0Domain">Auth0 tenant domain (e.g., "genai-44942055728411057.eu.auth0.com")</param>
    public static void MapOAuthMetadataEndpoints(this WebApplication app, string auth0Domain)
    {
        var auth0BaseUrl = $"https://{auth0Domain}";

        // RFC 9728: OAuth 2.1 Authorization Server Metadata
        // This endpoint tells clients where to get authorization
        app.MapGet("/.well-known/oauth-authorization-server", () =>
        {
            return Results.Json(new
            {
                issuer = $"{auth0BaseUrl}/",
                authorization_endpoint = $"{auth0BaseUrl}/authorize",
                token_endpoint = $"{auth0BaseUrl}/oauth/token",
                jwks_uri = $"{auth0BaseUrl}/.well-known/jwks.json",
                response_types_supported = new[] { "code" },
                grant_types_supported = new[] { "authorization_code", "client_credentials" },
                code_challenge_methods_supported = new[] { "S256" }, // PKCE required
                token_endpoint_auth_methods_supported = new[] { "client_secret_basic", "client_secret_post" }
            });
        })
        .AllowAnonymous() // Must be accessible without authentication
        .WithName("OAuthAuthorizationServerMetadata")
        .WithTags("OAuth Metadata");

        Console.WriteLine("OAuth metadata endpoint registered: /.well-known/oauth-authorization-server");
    }
}

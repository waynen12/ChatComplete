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
    /// <param name="apiAudience">The API audience identifier registered in Auth0 (e.g., "https://knowledge-manager-mcp")</param>
    public static void MapOAuthMetadataEndpoints(this WebApplication app, string auth0Domain, string apiAudience)
    {
        var auth0BaseUrl = $"https://{auth0Domain}";
        var audienceParam = Uri.EscapeDataString(apiAudience);

        // RFC 9728: OAuth 2.1 Authorization Server Metadata
        // This endpoint tells clients where to get authorization
        app.MapGet("/.well-known/oauth-authorization-server", () =>
        {
            return Results.Json(new
            {
                issuer = $"{auth0BaseUrl}/",
                // Pre-fill audience so clients request a JWT access token for our API (avoids opaque/encrypted tokens)
                authorization_endpoint = $"{auth0BaseUrl}/authorize?audience={audienceParam}",
                token_endpoint = $"{auth0BaseUrl}/oauth/token",
                jwks_uri = $"{auth0BaseUrl}/.well-known/jwks.json",
                response_types_supported = new[] { "code" },
                grant_types_supported = new[] { "authorization_code" }, // ONLY authorization_code for PKCE
                code_challenge_methods_supported = new[] { "S256" }, // PKCE required
                token_endpoint_auth_methods_supported = new[] { "none" }, // Public clients only (no client secret)
                scopes_supported = new[] { "mcp:read", "mcp:execute", "mcp:admin" },
                // Explicitly require PKCE
                require_pushed_authorization_requests = false,
                authorization_response_iss_parameter_supported = true
            });
        })
        .AllowAnonymous() // Must be accessible without authentication
        .WithName("OAuthAuthorizationServerMetadata")
        .WithTags("OAuth Metadata");

        Console.WriteLine("OAuth metadata endpoint registered: /.well-known/oauth-authorization-server");
    }
}

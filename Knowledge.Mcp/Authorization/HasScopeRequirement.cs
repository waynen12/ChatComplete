using Microsoft.AspNetCore.Authorization;

namespace Knowledge.Mcp.Authorization;

/// <summary>
/// Authorization requirement that validates the presence of a specific OAuth scope.
/// Used with Policy-Based Authorization to check if JWT tokens contain required scopes.
/// </summary>
/// <remarks>
/// Auth0 includes scopes in JWT tokens as a space-separated string in the "scope" claim.
/// Example: "scope": "mcp:read mcp:execute"
///
/// This requirement is checked by <see cref="HasScopeHandler"/>.
/// </remarks>
public class HasScopeRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// The issuer (Auth0 tenant) that must have issued the scope claim.
    /// Example: "https://genai-44942055728411057.eu.auth0.com/"
    /// </summary>
    public string Issuer { get; }

    /// <summary>
    /// The required scope that must be present in the JWT token.
    /// Example: "mcp:read", "mcp:execute", "mcp:admin"
    /// </summary>
    public string Scope { get; }

    /// <summary>
    /// Creates a new scope requirement.
    /// </summary>
    /// <param name="scope">The required scope (e.g., "mcp:execute")</param>
    /// <param name="issuer">The Auth0 tenant URL that must have issued the scope</param>
    public HasScopeRequirement(string scope, string issuer)
    {
        Scope = scope ?? throw new ArgumentNullException(nameof(scope));
        Issuer = issuer ?? throw new ArgumentNullException(nameof(issuer));
    }
}

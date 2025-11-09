using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Knowledge.Mcp.Authorization;

/// <summary>
/// Authorization handler that validates OAuth scopes in JWT tokens.
/// Checks if the authenticated user has the required scope specified in <see cref="HasScopeRequirement"/>.
/// </summary>
/// <remarks>
/// Auth0 JWT tokens contain a "scope" claim with space-separated scopes:
/// Example: "scope": "mcp:read mcp:execute mcp:admin"
///
/// This handler:
/// 1. Extracts the "scope" claim from the JWT token
/// 2. Splits the space-separated string into individual scopes
/// 3. Checks if the required scope is present
/// 4. Succeeds authorization if scope is found, fails otherwise
///
/// Used by Policy-Based Authorization with [Authorize("policy-name")] attribute.
/// </remarks>
public class HasScopeHandler : AuthorizationHandler<HasScopeRequirement>
{
    /// <summary>
    /// Handles the authorization requirement by checking if the user has the required scope.
    /// </summary>
    /// <param name="context">The authorization context containing user claims</param>
    /// <param name="requirement">The scope requirement to validate</param>
    /// <returns>Completed task (authorization result set via context.Succeed() or implicit failure)</returns>
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        HasScopeRequirement requirement)
    {
        // If user does not have the scope claim from the correct issuer, authorization fails
        if (!context.User.HasClaim(c => c.Type == "scope" && c.Issuer == requirement.Issuer))
            return Task.CompletedTask;

        // Extract the scope claim and split into individual scopes
        var scopeClaim = context.User
            .FindFirst(c => c.Type == "scope" && c.Issuer == requirement.Issuer);

        if (scopeClaim == null)
            return Task.CompletedTask;

        var scopes = scopeClaim.Value.Split(' ');

        // Succeed if the scope array contains the required scope
        if (scopes.Any(s => s == requirement.Scope))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}

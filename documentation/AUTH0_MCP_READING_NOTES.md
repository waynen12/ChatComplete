# Auth0 MCP Documentation Reading Notes

**Date:** 2025-10-22
**Status:** MCP Auth Guide ✅ | Security Best Practices ✅ | ASP.NET MVC ✅ | **ASP.NET Web API ✅** | AI Agents 📚
**Tenant:** contextbridge.eu.auth0.com
**API Audience:** https://knowledge-manager-mcp

**Critical Findings:**
- ⚠️ **CORRECT SDK:** Use `Microsoft.AspNetCore.Authentication.JwtBearer` for APIs
- ⚠️ **WRONG SDK:** Do NOT use `Auth0.AspNetCore.Authentication` (web apps only)
- ⚠️ **Scope Authorization:** Use Policy-Based Authorization (HasScopeRequirement + HasScopeHandler)
- ⚠️ **Token Validation:** MUST validate audience claim = `https://knowledge-manager-mcp`
- ⚠️ **Session Security:** Bind session IDs to user ID: `<user_id>:<session_id>`
- ✅ **M2M Support:** Client Credentials flow for MCP clients

---

## Reading Queue

### 1. Auth0 MCP Authorization Guide
**URL:** https://auth0.com/ai/docs/mcp/auth-for-mcp
**Status:** ✅ Read
**Purpose:** Understand Auth0's recommended approach for MCP authentication

**Key Points to Extract:**
- [x] Recommended authentication flow for MCP
- [x] How Auth0 handles MCP-specific requirements
- [x] Configuration steps for MCP servers
- [x] Best practices for MCP + Auth0
- [x] Example implementations
- [x] Common pitfalls to avoid

## Critical Findings from Auth0 MCP Guide

### 1. **MANDATORY Server Configuration Requirements**

#### A. Protected Resource Metadata Endpoint (RFC 9728) ✅ REQUIRED
**Must Implement:**
```
GET /.well-known/oauth-authorization-server
```

**Purpose:** Announce where clients should get authorization

**Response Format:**
```json
{
  "authorization_endpoint": "https://contextbridge.eu.auth0.com/authorize",
  "token_endpoint": "https://contextbridge.eu.auth0.com/oauth/token",
  "issuer": "https://contextbridge.eu.auth0.com/",
  "jwks_uri": "https://contextbridge.eu.auth0.com/.well-known/jwks.json"
}
```

**Implementation Location:** Knowledge.Mcp HTTP server
**Status:** ⚠️ NOT YET IMPLEMENTED (Add to Milestone #23)

#### B. WWW-Authenticate Header (401 Responses) ✅ REQUIRED
**Must Return on Unauthorized Requests:**
```http
HTTP/1.1 401 Unauthorized
WWW-Authenticate: Bearer realm="mcp-server",
                  authorization_uri="https://knowledge-mcp.example.com/.well-known/oauth-authorization-server"
```

**Purpose:** Tell clients where to start OAuth flow

**Implementation Location:** ASP.NET Core authentication middleware
**Status:** ⚠️ NOT YET IMPLEMENTED (Add to Milestone #23)

---

### 2. **OAuth 2.1 Features Auth0 Provides**

#### PKCE (Proof Key for Code Exchange) ✅
- **Status:** Mandatory per MCP spec
- **Handled by:** Auth0 SDKs automatically
- **Our Responsibility:** None (client-side concern)

#### Metadata Discovery ✅
- **Endpoint:** `https://contextbridge.eu.auth0.com/.well-known/oauth-authorization-server`
- **Purpose:** Clients dynamically find authorization/token endpoints
- **Our Responsibility:** Point to this in our metadata endpoint

#### Dynamic Client Registration (DCR) 🔄 Optional
- **Purpose:** MCP clients can programmatically register with Auth0
- **Benefit:** No manual app creation in Auth0 Dashboard
- **Status:** Not required for Milestone #23 (future enhancement)

#### Third-Party Identity Provider Delegation ✅
- **Our Approach:** Use Auth0 as the identity provider
- **Benefit:** Centralized user management
- **Status:** This is exactly what we're doing (Resource Server pattern)

---

### 3. **MCP Authorization Flow (7 Steps)**

```
Step 1: Client → MCP Server (no token)
        MCP Server → Client (401 + WWW-Authenticate header)

Step 2: Client → MCP Server /.well-known/oauth-protected-resource
        MCP Server → Client (metadata with Auth0 URL)

Step 3: Client → Auth0 /.well-known/oauth-authorization-server
        Auth0 → Client (Auth0 metadata)
        [Optional: Dynamic Client Registration]

Step 4: Client generates PKCE parameters
        Client → Browser → Auth0 /authorize

Step 5: User authenticates with Auth0
        Auth0 → Browser → Client redirect_uri (with code)

Step 6: Client → Auth0 /oauth/token (exchange code)
        Auth0 → Client (access token + refresh token)

Step 7: Client → MCP Server (with Bearer token)
        MCP Server validates token → Serves tools
```

**Key Insight:** MCP server needs to provide metadata endpoint, not just validate tokens!

---

### 4. **Access Token Validation (CRITICAL)**

#### Required Checks (in order):
1. ✅ **Verify Signature** - Token signed by Auth0
2. ✅ **Check Expiration (exp)** - Token not expired
3. ✅ **Validate Audience (aud)** - MOST IMPORTANT - Must match `https://knowledge-manager-mcp`
4. ✅ **Verify Issuer (iss)** - Must be `https://contextbridge.eu.auth0.com/`

**Implementation:** Auth0 SDKs do this automatically via middleware

**Middleware Pattern:**
```csharp
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://contextbridge.eu.auth0.com";
        options.Audience = "https://knowledge-manager-mcp";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true
        };
    });
```

---

### 5. **Security Best Practices**

#### ⚠️ CRITICAL: No Sessions for Authentication
**MCP Specification Prohibition:**
- DO NOT use sessions for authentication
- Bearer token is the ONLY valid credential
- MUST verify token on EVERY request

**Why:**
- Stateless authentication = more secure
- Prevents session hijacking
- Supports distributed/scaled deployments

**Implementation:**
- Use `[Authorize]` attribute on all protected endpoints
- Middleware validates token on every request
- No session state storage needed

#### ✅ Verify All Inbound Requests
**Pattern:**
```csharp
[Authorize] // Validates token automatically
[HttpPost("/tools/{toolName}")]
public async Task<IActionResult> ExecuteTool(string toolName)
{
    // Token already validated by middleware
    // Extract scopes from ClaimsPrincipal
    var scopes = User.Claims
        .Where(c => c.Type == "scope")
        .SelectMany(c => c.Value.Split(' '));

    // Check required scope
    if (!scopes.Contains("mcp:execute"))
    {
        return Forbid(); // 403 - has token but wrong scope
    }

    // Execute tool
}
```

---

### 6. **What We Need to Add to Implementation**

#### NEW Requirements (not in original plan):

1. **/.well-known/oauth-authorization-server endpoint** ⚠️ MANDATORY
   - Returns Auth0 metadata
   - Points clients to Auth0

2. **/.well-known/oauth-protected-resource endpoint** ⚠️ MANDATORY (from Step 2)
   - Returns list of authorization servers
   - Our case: Just Auth0

3. **WWW-Authenticate header on 401** ⚠️ MANDATORY
   - Middleware must add this header
   - Points to metadata endpoint

4. **Stateless validation on every request** ✅ Already planned
   - No session state
   - Token validation middleware

---

### 7. **Implementation Checklist Updates**

**Week 1 Updates:**
- [x] Add JWT Bearer middleware (already planned)
- [ ] **NEW:** Add `/.well-known/oauth-authorization-server` endpoint
- [ ] **NEW:** Add `/.well-known/oauth-protected-resource` endpoint
- [ ] **NEW:** Configure WWW-Authenticate header in 401 responses
- [ ] **NEW:** Document that sessions are NOT used for auth

**Week 2 Updates:**
- [x] Scope-based authorization (already planned)
- [ ] **NEW:** Ensure [Authorize] on ALL tool/resource endpoints
- [ ] **NEW:** Add explicit scope checking in endpoints
- [ ] **NEW:** Return 403 (not 401) when token valid but wrong scope

**Week 3 Updates:**
- [x] Test with Auth0 tokens (already planned)
- [ ] **NEW:** Test metadata endpoint discovery flow
- [ ] **NEW:** Test WWW-Authenticate header response
- [ ] **NEW:** Verify no session state created


---

## Security Attack Vectors and Mitigations

### Overview
Auth0's MCP Security Best Practices document identifies three critical attack vectors specific to MCP implementations, beyond standard OAuth 2.1 security concerns.

**Document Scope:**
- Developers implementing MCP authorization flows
- MCP server operators
- Security professionals evaluating MCP-based systems
- Should be read alongside OAuth 2.0 security best practices (RFC 9700)

---

### Attack 1: Confused Deputy Problem

**Applies to:** MCP Proxy Servers (servers that act as OAuth clients to third-party APIs)

**⚠️ DOES NOT APPLY TO OUR IMPLEMENTATION**
- Knowledge Manager MCP server does NOT proxy third-party APIs
- We are a direct MCP server exposing our own tools/resources
- No third-party API delegation occurs

**Attack Summary:**
When an MCP proxy server uses a static OAuth client ID to connect to third-party APIs without dynamic client registration support, attackers can exploit consent cookies to steal authorization codes.

**Attack Flow:**
1. User authenticates normally through MCP proxy → third-party API
2. Third-party authorization server sets consent cookie for static client ID
3. Attacker dynamically registers malicious client with attacker.com redirect URI
4. Attacker sends malicious link to user
5. User's browser still has consent cookie → consent screen skipped
6. Authorization code redirected to attacker.com instead of legitimate server
7. Attacker exchanges stolen code for access tokens

**Mitigation (If Applicable):**
- MCP proxy servers using static client IDs MUST obtain user consent for each dynamically registered client
- This applies BEFORE forwarding to third-party authorization servers

**Our Status:** ✅ Not vulnerable - we don't proxy third-party APIs

---

### Attack 2: Token Passthrough (Anti-Pattern)

**⚠️ CRITICAL - APPLIES TO OUR IMPLEMENTATION**

**Definition:**
"Token passthrough" is when an MCP server accepts tokens from clients without validating that tokens were issued TO the MCP server, then passes them to downstream APIs.

**Why It's Forbidden:**
The MCP Authorization specification explicitly forbids this pattern.

**Risks:**

1. **Security Control Circumvention**
   - Bypasses rate limiting, request validation, traffic monitoring
   - Clients can obtain tokens directly from upstream and bypass MCP server controls

2. **Accountability and Audit Trail Issues**
   - MCP server cannot identify/distinguish between clients
   - Logs show requests from different sources than actual origin
   - Makes incident investigation and auditing difficult
   - Enables data exfiltration with stolen tokens

3. **Trust Boundary Issues**
   - Breaks trust assumptions between services
   - Compromised service can use tokens to access other connected services
   - Token accepted by multiple services without proper validation

4. **Future Compatibility Risk**
   - Hard to add security controls later
   - Proper token audience separation needed from the start

**Mitigation (MANDATORY):**
```
MCP servers MUST NOT accept any tokens that were not explicitly issued for the MCP server.
```

**Implementation for Knowledge Manager:**
- ✅ Already configured: `options.Audience = "https://knowledge-manager-mcp"`
- ✅ Middleware MUST validate audience matches our API identifier
- ✅ MUST reject tokens with different audience claims
- ✅ Each token validation checks `aud` claim = `https://knowledge-manager-mcp`

**Code Pattern:**
```csharp
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://contextbridge.eu.auth0.com";
        options.Audience = "https://knowledge-manager-mcp"; // ⚠️ CRITICAL
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true, // ⚠️ MUST BE TRUE
            // ... other validations
        };
    });
```

---

### Attack 3: Session Hijacking

**⚠️ CRITICAL - APPLIES TO OUR IMPLEMENTATION**

**Two Attack Variants:**

#### Variant A: Session Hijack Prompt Injection

**Scenario:** Multiple stateful HTTP servers with shared queue/state

**Attack Flow:**
1. Client connects to Server A, receives session ID
2. Attacker obtains session ID, sends malicious event to Server B with that session ID
3. Server B enqueues event (keyed by session ID) into shared queue
4. Server A polls queue using session ID, retrieves malicious payload
5. Server A sends malicious payload to client as async/resumed response
6. Client acts on malicious payload → compromise

**Attack Vectors:**
- Resumable streams: Attacker terminates request before response, original client resumes and receives malicious payload
- Tool list manipulation: Attacker triggers `notifications/tools/list_changed`, client receives different tools than expected

#### Variant B: Session Hijack Impersonation

**Scenario:** Server uses session IDs for authentication (FORBIDDEN BY MCP)

**Attack Flow:**
1. Client authenticates, receives persistent session ID
2. Attacker obtains session ID
3. Attacker makes API calls using session ID
4. Server doesn't re-authenticate, treats attacker as legitimate user
5. Unauthorized access granted

**Critical Finding - Already Documented:**
```
⚠️ MCP specification PROHIBITS using sessions for authentication
- Bearer token is the ONLY valid credential
- MUST verify token on EVERY request
```

**Mitigations (MANDATORY):**

1. **MUST verify all inbound requests** ✅
   ```csharp
   [Authorize] // Validates bearer token on every request
   public async Task<IActionResult> ExecuteTool(string toolName)
   ```

2. **MUST NOT use sessions for authentication** ✅
   - Already confirmed in previous section
   - We use stateless JWT bearer token validation
   - No session state stored server-side

3. **MUST use secure, non-deterministic session IDs** 🔄
   - Use UUIDs with secure random number generators
   - Avoid predictable/sequential identifiers
   - Rotate/expire session IDs regularly

4. **SHOULD bind session IDs to user-specific information** 🔄
   - Combine session ID with user ID from validated token
   - Key format: `<user_id>:<session_id>`
   - Even if attacker guesses session ID, can't impersonate (no user ID)

**Implementation for Knowledge Manager:**

**Already Secure:**
- ✅ Stateless authentication (JWT bearer tokens)
- ✅ Token validated on every request via [Authorize] attribute
- ✅ No session-based authentication

**Need to Verify:**
- 🔍 **Session ID Generation:** Check that Mcp-Session-Id uses secure random generation
- 🔍 **Session ID Binding:** Consider binding session IDs to user ID from token (`sub` claim)
- 🔍 **Session ID Rotation:** Implement session expiration/rotation

**Recommended Pattern:**
```csharp
// When generating session ID
var userId = User.FindFirst("sub")?.Value; // From validated JWT
var sessionId = Guid.NewGuid().ToString(); // Secure random UUID
var boundSessionId = $"{userId}:{sessionId}";

// Store in exposed header
context.Response.Headers["Mcp-Session-Id"] = boundSessionId;

// When validating session operations
var boundSessionId = context.Request.Headers["Mcp-Session-Id"];
var parts = boundSessionId.Split(':');
var sessionUserId = parts[0];
var tokenUserId = User.FindFirst("sub")?.Value;

if (sessionUserId != tokenUserId)
{
    return Unauthorized(); // Session doesn't match token user
}
```

---

## Auth0 ASP.NET Core Integration Patterns

### Overview
Auth0 provides the `Auth0.AspNetCore.Authentication` SDK for integrating authentication into ASP.NET Core applications. While our MCP server uses **API authentication** (JWT bearer tokens), understanding the web app authentication pattern helps inform our implementation.

**Key Difference:**
- **Web Apps (Auth0.AspNetCore.Authentication):** Cookie-based sessions, user login/logout flows, OIDC
- **APIs/MCP Servers (JWT Bearer):** Stateless token validation, no login UI, bearer tokens only

**Why This Matters:**
- Auth0's web app SDK uses **sessions** (cookies) - which MCP specification PROHIBITS
- We need JWT bearer authentication instead, not OIDC middleware
- But the configuration patterns (Domain, ClientId, Audience) are similar

---

### Auth0 Web App Integration (For Reference)

**1. SDK Installation:**
```bash
dotnet add package Auth0.AspNetCore.Authentication
```

**2. Middleware Configuration (Web Apps):**
```csharp
// Program.cs - WEB APP PATTERN (NOT for MCP server)
builder.Services.AddAuth0WebAppAuthentication(options =>
{
    options.Domain = "contextbridge.eu.auth0.com";
    options.ClientId = "web-app-client-id"; // NOT the same as API audience
});

app.UseAuthentication();
app.UseAuthorization();
```

⚠️ **DO NOT USE FOR MCP SERVER** - This creates cookie-based sessions, which violates MCP spec.

**3. Login Flow (Web Apps):**
```csharp
// Controller action - WEB APP PATTERN
public async Task Login(string returnUrl = "/")
{
    var authenticationProperties = new LoginAuthenticationPropertiesBuilder()
        .WithRedirectUri(returnUrl)
        .Build();

    await HttpContext.ChallengeAsync(
        Auth0Constants.AuthenticationScheme,
        authenticationProperties
    );
}
```

**4. Logout Flow (Web Apps):**
```csharp
// Controller action - WEB APP PATTERN
public async Task Logout()
{
    var authenticationProperties = new LogoutAuthenticationPropertiesBuilder()
        .WithRedirectUri(Url.Action("Index", "Home"))
        .Build();

    // Log out from cookie authentication
    await HttpContext.SignOutAsync(
        CookieAuthenticationDefaults.AuthenticationScheme
    );

    // Log out from Auth0
    await HttpContext.SignOutAsync(
        Auth0Constants.AuthenticationScheme,
        authenticationProperties
    );
}
```

**5. User Profile Access (Web Apps):**
```csharp
// Controller action - WEB APP PATTERN
public IActionResult Profile()
{
    var userName = User.Identity.Name;
    var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
    var userPicture = User.Claims.FirstOrDefault(c => c.Type == "picture")?.Value;

    return View(new UserProfileViewModel
    {
        Name = userName,
        Email = userEmail,
        ProfileImage = userPicture
    });
}
```

---

### MCP Server Integration (What We Actually Need)

**Correct Pattern for API/MCP Server:**

**1. SDK Installation:**
```bash
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
# NOTE: NOT Auth0.AspNetCore.Authentication (that's for web apps)
```

**2. Middleware Configuration (APIs/MCP):**
```csharp
// Program.cs - API/MCP SERVER PATTERN
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://contextbridge.eu.auth0.com"; // Auth0 tenant
        options.Audience = "https://knowledge-manager-mcp"; // API identifier (NOT client ID)
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidateAudience = true, // ⚠️ CRITICAL for token passthrough prevention
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(300)
        };
    });

app.UseAuthentication();
app.UseAuthorization();
```

✅ **USE THIS FOR MCP SERVER** - Stateless JWT validation, no sessions.

**3. No Login/Logout (APIs/MCP):**
- MCP servers don't have login UI
- Clients obtain tokens directly from Auth0
- MCP server only validates tokens

**4. User Information Access (APIs/MCP):**
```csharp
// MCP Tool endpoint - API PATTERN
[Authorize]
[HttpPost("/tools/{toolName}")]
public async Task<IActionResult> ExecuteTool(string toolName)
{
    // Extract user info from validated JWT claims
    var userId = User.FindFirst("sub")?.Value;
    var userEmail = User.FindFirst("email")?.Value;
    var scopes = User.Claims
        .Where(c => c.Type == "scope")
        .SelectMany(c => c.Value.Split(' '));

    // Scope validation
    if (!scopes.Contains("mcp:execute"))
    {
        return Forbid(); // 403
    }

    // Execute tool with user context
    var result = await _toolExecutor.ExecuteAsync(toolName, userId);
    return Ok(result);
}
```

---

### Configuration Comparison

| Aspect | Web App (Cookie Auth) | MCP Server (JWT Bearer) |
|--------|----------------------|-------------------------|
| **Package** | Auth0.AspNetCore.Authentication | Microsoft.AspNetCore.Authentication.JwtBearer |
| **Middleware** | AddAuth0WebAppAuthentication | AddJwtBearer |
| **Configuration** | Domain + ClientId | Authority + Audience |
| **Authentication** | OIDC + Cookies | JWT Bearer Token |
| **Sessions** | ✅ Yes (cookie-based) | ❌ NO (stateless) |
| **Login Flow** | HttpContext.ChallengeAsync | N/A (client handles) |
| **Logout Flow** | HttpContext.SignOutAsync | N/A (token expiration) |
| **Token Source** | Cookie after redirect | Authorization header |
| **User Info** | User.Identity.Name | User.Claims (from JWT) |
| **MCP Compatible** | ❌ NO (violates spec) | ✅ YES (stateless) |

---

### Key Takeaways for MCP Implementation

**1. Use JWT Bearer, Not OIDC Web App SDK:**
- Auth0.AspNetCore.Authentication is for browser-based apps with login UI
- MCP servers are APIs - use Microsoft.AspNetCore.Authentication.JwtBearer

**2. Configuration Mapping:**
- Web App `Domain` → API `Authority` (both use Auth0 tenant URL)
- Web App `ClientId` → API `Audience` (API uses different identifier)
- API Audience = Auth0 API identifier, NOT application client ID

**3. No Login/Logout Endpoints in MCP Server:**
- Clients handle OAuth flow directly with Auth0
- MCP server only validates tokens received in Authorization header

**4. Stateless Architecture:**
- No cookies, no sessions, no server-side state
- Token validated on every request via [Authorize] attribute
- User context extracted from JWT claims

**5. Sample Application Reference:**
- Auth0's sample uses cookie authentication (web app pattern)
- We adapt the **configuration** (Auth0 tenant, domain)
- But use **JWT bearer pattern** instead of OIDC middleware

---

## Auth0 Web API Integration (CORRECT PATTERN FOR MCP)

### Overview
This is the **correct Auth0 integration pattern** for MCP servers. Unlike the web app pattern (cookies), this uses JWT Bearer authentication for APIs.

**Source:** Auth0 ASP.NET Core Web API Quickstart
**Applicability:** ✅ Directly applicable to Knowledge Manager MCP server

---

### Step 1: Define Permissions (Scopes)

**Auth0 Dashboard Configuration:**
- Navigate to: Applications > APIs > [Your API] > Permissions tab
- Define permissions (scopes) for your API

**Our Configuration:**
```
API Name: Knowledge Manager MCP API
API Identifier (Audience): https://knowledge-manager-mcp

Scopes:
- mcp:read        Description: Read-only operations (resources, health checks)
- mcp:execute     Description: Execute tools (search, analytics, recommendations)
- mcp:admin       Description: Administrative operations (manage knowledge bases)
```

**Why This Matters:**
- Scopes appear in JWT token's `scope` claim
- Enable fine-grained authorization (different tools require different scopes)
- Configured in Auth0 Dashboard, enforced in MCP server code

---

### Step 2: Install Dependencies

**NuGet Package:**
```bash
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
```

✅ **This is the correct package** - NOT Auth0.AspNetCore.Authentication (web app SDK)

---

### Step 3: Configure the Middleware

**Configuration in appsettings.json:**
```json
{
  "Auth0": {
    "Domain": "contextbridge.eu.auth0.com",
    "Audience": "https://knowledge-manager-mcp"
  }
}
```

**Middleware Setup in Program.cs:**
```csharp
// Read configuration
var domain = $"https://{builder.Configuration["Auth0:Domain"]}";
var audience = builder.Configuration["Auth0:Audience"];

// Register authentication services
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = domain; // Auth0 tenant
        options.Audience = audience; // API identifier
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidateAudience = true, // ⚠️ CRITICAL for token passthrough prevention
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(300),
            NameClaimType = ClaimTypes.NameIdentifier // Maps 'sub' claim to User.Identity.Name
        };
    });

// Add authentication and authorization middleware
var app = builder.Build();
app.UseAuthentication(); // MUST come before UseAuthorization
app.UseAuthorization();
```

**Important Notes:**
- `options.Authority` = Auth0 tenant URL (automatically fetches JWKS from `/.well-known/jwks.json`)
- `options.Audience` = API identifier from Auth0 Dashboard (NOT client ID)
- `ValidateAudience = true` prevents token passthrough attacks
- `NameClaimType` maps JWT `sub` claim to `User.Identity.Name` (optional, useful for logging)

---

### Step 4: Validate Scopes (Policy-Based Authorization)

Auth0's recommended pattern uses **Policy-Based Authorization** for scope checking.

**Create HasScopeRequirement Class:**
```csharp
// Knowledge.Mcp/Authorization/HasScopeRequirement.cs
using Microsoft.AspNetCore.Authorization;

public class HasScopeRequirement : IAuthorizationRequirement
{
    public string Issuer { get; }
    public string Scope { get; }

    public HasScopeRequirement(string scope, string issuer)
    {
        Scope = scope ?? throw new ArgumentNullException(nameof(scope));
        Issuer = issuer ?? throw new ArgumentNullException(nameof(issuer));
    }
}
```

**Create HasScopeHandler Class:**
```csharp
// Knowledge.Mcp/Authorization/HasScopeHandler.cs
using Microsoft.AspNetCore.Authorization;

public class HasScopeHandler : AuthorizationHandler<HasScopeRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        HasScopeRequirement requirement)
    {
        // If user does not have the scope claim, get out of here
        if (!context.User.HasClaim(c => c.Type == "scope" && c.Issuer == requirement.Issuer))
        {
            return Task.CompletedTask;
        }

        // Split the scopes string into an array
        var scopes = context.User
            .FindFirst(c => c.Type == "scope" && c.Issuer == requirement.Issuer)
            .Value.Split(' ');

        // Succeed if the scope array contains the required scope
        if (scopes.Any(s => s == requirement.Scope))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
```

**Register Authorization Policies in Program.cs:**
```csharp
// After builder.Services.AddAuthentication()...

var domain = $"https://{builder.Configuration["Auth0:Domain"]}";

builder.Services.AddAuthorization(options =>
{
    // Policy for read-only operations
    options.AddPolicy("read:messages", policy =>
        policy.Requirements.Add(new HasScopeRequirement("mcp:read", domain)));

    // Policy for tool execution
    options.AddPolicy("execute:tools", policy =>
        policy.Requirements.Add(new HasScopeRequirement("mcp:execute", domain)));

    // Policy for admin operations
    options.AddPolicy("admin:operations", policy =>
        policy.Requirements.Add(new HasScopeRequirement("mcp:admin", domain)));
});

// Register the scope authorization handler as a singleton
builder.Services.AddSingleton<IAuthorizationHandler, HasScopeHandler>();
```

---

### Step 5: Protect API Endpoints

**Basic Protection (Authentication Only):**
```csharp
[Authorize] // Requires valid JWT token
[HttpGet("/api/health")]
public IActionResult GetHealth()
{
    return Ok(new { status = "healthy" });
}
```

**Scope-Based Protection (Authorization):**
```csharp
[Authorize("execute:tools")] // Requires mcp:execute scope
[HttpPost("/tools/search")]
public async Task<IActionResult> SearchTool([FromBody] SearchRequest request)
{
    // User has valid token AND mcp:execute scope
    var userId = User.FindFirst("sub")?.Value;
    var result = await _searchService.SearchAsync(request, userId);
    return Ok(result);
}
```

**Multiple Scopes (Any):**
```csharp
[Authorize("read:messages")] // Only needs mcp:read
[HttpGet("/resources/{resourceId}")]
public async Task<IActionResult> GetResource(string resourceId)
{
    var resource = await _resourceService.GetAsync(resourceId);
    return Ok(resource);
}
```

**Manual Scope Checking (More Flexible):**
```csharp
[Authorize]
[HttpPost("/tools/{toolName}")]
public async Task<IActionResult> ExecuteTool(string toolName)
{
    // Extract scopes from validated token
    var scopes = User.Claims
        .Where(c => c.Type == "scope")
        .SelectMany(c => c.Value.Split(' '))
        .ToList();

    // Different tools require different scopes
    var requiredScope = GetRequiredScope(toolName);
    if (!scopes.Contains(requiredScope))
    {
        return Forbid(); // 403 - has token but wrong scope
    }

    // Execute tool
    var result = await _toolExecutor.ExecuteAsync(toolName);
    return Ok(result);
}
```

---

### Step 6: Token Acquisition (For Testing)

**Machine-to-Machine (M2M) Flow:**

MCP clients will typically be M2M applications. To get a token for testing:

**1. Create M2M Application in Auth0 Dashboard:**
- Navigate to Applications > Create Application
- Choose "Machine to Machine Applications"
- Authorize the application for your API
- Grant scopes (mcp:read, mcp:execute, mcp:admin)

**2. Get Access Token via OAuth Client Credentials Flow:**
```bash
curl --request POST \
  --url 'https://contextbridge.eu.auth0.com/oauth/token' \
  --header 'content-type: application/x-www-form-urlencoded' \
  --data 'grant_type=client_credentials' \
  --data 'client_id=YOUR_CLIENT_ID' \
  --data 'client_secret=YOUR_CLIENT_SECRET' \
  --data 'audience=https://knowledge-manager-mcp'
```

**Response:**
```json
{
  "access_token": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
  "token_type": "Bearer",
  "expires_in": 86400
}
```

**3. Call Secure Endpoint:**
```bash
# Call endpoint requiring authentication only
curl --request GET \
  --url http://localhost:5001/api/health \
  --header 'authorization: Bearer YOUR_ACCESS_TOKEN'

# Call endpoint requiring mcp:execute scope
curl --request POST \
  --url http://localhost:5001/tools/search \
  --header 'authorization: Bearer YOUR_ACCESS_TOKEN' \
  --header 'content-type: application/json' \
  --data '{"query": "kubernetes deployment"}'
```

---

### Scope-to-Tool Mapping for Knowledge Manager

**Recommended Mapping:**

| Scope | MCP Resources/Tools | Description |
|-------|---------------------|-------------|
| **mcp:read** | • All MCP resources<br>• Health check endpoints<br>• Knowledge base metadata | Read-only operations |
| **mcp:execute** | • All MCP tools<br>• Cross-knowledge search<br>• Analytics queries<br>• Model recommendations | Tool execution |
| **mcp:admin** | • Create/delete knowledge bases<br>• Manage users<br>• System configuration | Administrative (future) |

**Implementation Pattern:**
```csharp
// Resource endpoints - require mcp:read
[Authorize("read:messages")]
[HttpGet("/resources/{uri}")]
public async Task<IActionResult> GetResource(string uri) { }

// Tool endpoints - require mcp:execute
[Authorize("execute:tools")]
[HttpPost("/tools/call")]
public async Task<IActionResult> CallTool([FromBody] ToolRequest request) { }

// Admin endpoints - require mcp:admin
[Authorize("admin:operations")]
[HttpDelete("/knowledge/{knowledgeId}")]
public async Task<IActionResult> DeleteKnowledge(string knowledgeId) { }
```

---

### Key Differences from Web App Pattern

| Aspect | Web App Pattern | Web API Pattern (MCP) |
|--------|----------------|------------------------|
| **Package** | Auth0.AspNetCore.Authentication | Microsoft.AspNetCore.Authentication.JwtBearer |
| **Authentication** | OIDC + Cookies | JWT Bearer Token |
| **Login Flow** | Server-side redirect | Client obtains token from Auth0 |
| **Configuration** | Domain + ClientId | Domain + Audience |
| **Token Location** | Cookie | Authorization header |
| **Scope Validation** | N/A (web app roles) | Policy-based authorization |
| **M2M Support** | ❌ Not applicable | ✅ Client Credentials flow |

---

### Implementation Checklist for Knowledge Manager

Based on Auth0 Web API quickstart:

**Configuration:**
- [ ] Add Auth0:Domain to appsettings.json
- [ ] Add Auth0:Audience to appsettings.json
- [ ] Store securely (not hardcoded)

**Code:**
- [ ] Install Microsoft.AspNetCore.Authentication.JwtBearer package
- [ ] Configure AddAuthentication with JwtBearerDefaults
- [ ] Set Authority and Audience from configuration
- [ ] Enable all token validations (Audience, Issuer, Lifetime, Signature)
- [ ] Add UseAuthentication() before UseAuthorization()

**Authorization:**
- [ ] Create HasScopeRequirement class
- [ ] Create HasScopeHandler class
- [ ] Register authorization policies for each scope
- [ ] Register HasScopeHandler as singleton
- [ ] Add [Authorize] to all protected endpoints
- [ ] Add [Authorize("policy")] to scope-specific endpoints

**Testing:**
- [ ] Create M2M application in Auth0 Dashboard
- [ ] Grant API access and scopes
- [ ] Test token acquisition via Client Credentials flow
- [ ] Test endpoint access with valid token
- [ ] Test scope enforcement (expect 403 with wrong scope)

**Auth0 Dashboard:**
- [ ] Create API: "Knowledge Manager MCP API"
- [ ] Set Audience: `https://knowledge-manager-mcp`
- [ ] Define scopes: mcp:read, mcp:execute, mcp:admin
- [ ] Create M2M test application
- [ ] Authorize test app for API with all scopes

---

### 2. Auth0 MCP Security Best Practices
**URL:** https://auth0.com/ai/docs/mcp/security-best-practices
**Status:** ✅ Read
**Purpose:** Understand security attack vectors and mitigations for MCP servers

**Key Points to Extract:**
- [x] Confused Deputy Problem in MCP proxy servers
- [x] Token passthrough anti-patterns
- [x] Session hijacking attack vectors
- [x] Mitigation strategies for MCP implementations
- [x] Security requirements beyond OAuth 2.1

### 3. Auth0 ASP.NET Core Integration Guide
**URL:** https://auth0.com/docs/quickstart/webapp/aspnet-core-mvc
**Status:** ✅ Read
**Purpose:** Understand Auth0 SDK patterns for ASP.NET Core applications

**Key Points to Extract:**
- [x] Auth0.AspNetCore.Authentication SDK usage
- [x] Middleware configuration patterns
- [x] Login/Logout implementation
- [x] User profile information access
- [x] OIDC authentication handler integration

**Sample Application:**
https://github.com/auth0-samples/auth0-aspnetcore-mvc-samples/tree/master/Quickstart/Sample

### 4. Auth0 ASP.NET Core Web API Integration
**URL:** https://auth0.com/docs/quickstart/backend/aspnet-core-webapi
**Status:** ✅ Read
**Purpose:** Understand JWT Bearer authentication for ASP.NET Core APIs (DIRECTLY APPLICABLE)

**Key Points to Extract:**
- [x] Microsoft.AspNetCore.Authentication.JwtBearer package usage
- [x] JWT Bearer middleware configuration (Authority + Audience)
- [x] Scope-based authorization with Policy-Based Authorization
- [x] HasScopeRequirement pattern for scope validation
- [x] [Authorize] attribute usage on endpoints
- [x] M2M (Machine-to-Machine) token acquisition

**This is the CORRECT pattern for MCP servers** ✅

### 5. Auth0 AI Agents Overview
**URL:** https://auth0.com/ai/docs/intro/overview
**Status:** 📚 To Read
**Purpose:** Understand broader context of AI agent authentication

**Key Points to Extract:**
- [ ] General patterns for AI agent authentication
- [ ] Security considerations for AI systems
- [ ] User consent and privacy
- [ ] Multi-agent authorization patterns
- [ ] Integration with existing identity systems

**Notes:**
(Add notes as you read...)


---

## Key Takeaways

### Authentication Flow Recommendations

**✅ Use Resource Server Pattern Only:**
- MCP server validates tokens, doesn't issue them
- Auth0 handles user authentication, MFA, SSO
- Simpler implementation (2-3 weeks vs 3-6 months)
- Industry best practice confirmed by Okta standards team

**✅ Stateless Token-Based Authentication:**
- No session storage - bearer token is the only credential
- Token validated on every request via middleware
- Supports distributed deployments and horizontal scaling

**✅ Metadata-Driven Discovery:**
- Clients discover authorization endpoints via `/.well-known/oauth-authorization-server`
- Eliminates hardcoded authorization URLs in clients
- Follows RFC 9728 Protected Resource Metadata standard

### Configuration Best Practices

**Auth0 Tenant Setup:**
- Create API with unique audience identifier (e.g., `https://knowledge-manager-mcp`)
- Define granular scopes (mcp:read, mcp:execute, mcp:admin)
- Use RS256 signing algorithm (default)
- Enable RBAC (Role-Based Access Control) for production

**MCP Server Configuration:**
- Store Auth0 domain in configuration: `https://{tenant}.auth0.com`
- Configure audience to match Auth0 API identifier exactly
- Set clock skew tolerance (default 300 seconds)
- Enable all token validation checks (issuer, audience, lifetime, signature)

**CORS Configuration:**
- Required for browser-based MCP clients
- Restrict allowed origins to trusted domains
- Enable credentials for OAuth flows
- Expose necessary headers (Mcp-Session-Id, etc.)

### Security Considerations

**⚠️ CRITICAL - No Session State:**
- MCP specification explicitly prohibits session-based authentication
- Every request must include Bearer token
- Middleware must validate token on every request
- No server-side session storage or cookies

**Token Validation Order:**
1. Signature verification (token signed by Auth0)
2. Expiration check (not expired)
3. Audience validation (most critical - prevents token reuse)
4. Issuer validation (token from correct Auth0 tenant)

**Scope-Based Authorization:**
- Use [Authorize] attribute on all protected endpoints
- Extract scopes from ClaimsPrincipal after token validation
- Return 403 Forbidden (not 401) when token valid but wrong scope
- Log all authorization failures for security monitoring

**WWW-Authenticate Header:**
- Must be returned on all 401 Unauthorized responses
- Tells clients where to get authorization
- Includes realm and authorization_uri parameters
- Enables automatic OAuth flow initiation

### Implementation Patterns

**JWT Bearer Middleware Configuration:**
```csharp
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://contextbridge.eu.auth0.com";
        options.Audience = "https://knowledge-manager-mcp";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(300)
        };
    });
```

**Metadata Endpoint Implementation:**
```csharp
app.MapGet("/.well-known/oauth-authorization-server", () =>
{
    return Results.Json(new
    {
        authorization_endpoint = "https://contextbridge.eu.auth0.com/authorize",
        token_endpoint = "https://contextbridge.eu.auth0.com/oauth/token",
        issuer = "https://contextbridge.eu.auth0.com/",
        jwks_uri = "https://contextbridge.eu.auth0.com/.well-known/jwks.json"
    });
});
```

**Scope Validation Pattern:**
```csharp
[Authorize]
[HttpPost("/tools/{toolName}")]
public async Task<IActionResult> ExecuteTool(string toolName)
{
    // Token already validated by middleware
    var scopes = User.Claims
        .Where(c => c.Type == "scope")
        .SelectMany(c => c.Value.Split(' '));

    if (!scopes.Contains("mcp:execute"))
    {
        return Forbid(); // 403 - has token but wrong scope
    }

    // Execute tool
}
```

**401 Response with WWW-Authenticate:**
```csharp
app.Use(async (context, next) =>
{
    await next();

    if (context.Response.StatusCode == 401)
    {
        context.Response.Headers.WWWAuthenticate =
            "Bearer realm=\"mcp-server\", " +
            "authorization_uri=\"https://knowledge-mcp.example.com/.well-known/oauth-authorization-server\"";
    }
});
```


---

## Questions to Answer

### 1. Does Auth0 recommend Resource Server pattern for MCP?
**Answer:** ✅ YES - Explicitly confirmed

Auth0 documentation shows the MCP server as a "Protected Resource" that validates tokens from an external Authorization Server (Auth0). This aligns perfectly with our Option B decision and Aaron Parecki's recommendation.

**Evidence:**
- Auth0 MCP guide shows 7-step flow with Auth0 as Authorization Server
- MCP server only validates tokens, doesn't issue them
- Matches industry best practice for API protection

### 2. How does Auth0 handle scope enforcement for MCP tools?
**Answer:** Via standard OAuth 2.1 scopes in JWT claims

**Implementation:**
- Define scopes in Auth0 API configuration
- Auth0 includes scopes in JWT token as `scope` claim (space-separated string)
- MCP server extracts scopes from `User.Claims` after token validation
- Each endpoint checks for required scope(s)

**Pattern:**
```csharp
var scopes = User.Claims
    .Where(c => c.Type == "scope")
    .SelectMany(c => c.Value.Split(' '));

if (!scopes.Contains("mcp:execute"))
{
    return Forbid(); // 403
}
```

### 3. Are there Auth0-specific SDKs or libraries for .NET MCP servers?
**Answer:** No MCP-specific SDK needed - use standard Microsoft JWT Bearer middleware

**Libraries Required:**
- `Microsoft.AspNetCore.Authentication.JwtBearer` - Standard .NET JWT validation
- No Auth0-specific packages needed for resource server pattern

**Why:**
- Auth0 issues standard JWT tokens
- Microsoft middleware handles JWKS, signature verification, claims extraction
- Auth0 SDK only needed if implementing Authorization Server (not our case)

### 4. What's the recommended token lifetime for MCP sessions?
**Answer:** Not explicitly specified in docs - use Auth0 defaults

**Auth0 Defaults:**
- Access token: 24 hours (configurable in Auth0 dashboard)
- Refresh token: 30 days (configurable)

**Recommendation:**
- Keep access tokens short (1-24 hours)
- Use refresh tokens for long-running MCP sessions
- Configure via Auth0 API settings, not MCP server

### 5. How should we handle token refresh in MCP clients?
**Answer:** Client-side responsibility, not MCP server concern

**Client Responsibilities:**
1. Detect 401 response from MCP server
2. Use refresh token to get new access token from Auth0
3. Retry request with new access token
4. If refresh fails, restart full OAuth flow

**MCP Server Responsibilities:**
- None - server only validates tokens
- Return 401 when token expired/invalid
- Include WWW-Authenticate header to guide client

### 6. Does Auth0 provide MCP-specific token claims or extensions?
**Answer:** No - uses standard OAuth 2.1 claims

**Standard Claims in Token:**
- `iss` - Issuer (Auth0 tenant URL)
- `aud` - Audience (our API identifier)
- `sub` - Subject (user ID)
- `scope` - Space-separated scopes
- `exp` - Expiration timestamp
- `iat` - Issued at timestamp

**Custom Claims:**
- Can add custom claims via Auth0 Actions/Rules
- Namespace custom claims (e.g., `https://knowledge-mcp/user_role`)
- Not required for basic MCP implementation

### 7. What monitoring/logging does Auth0 recommend for MCP servers?
**Answer:** Not explicitly covered in MCP docs - use standard security monitoring

**Recommended Logging:**
- All authentication failures (401 responses)
- Authorization failures (403 responses - wrong scope)
- Token validation errors (signature, expiration, audience mismatch)
- Scope enforcement decisions

**Auth0 Dashboard Monitoring:**
- View authentication attempts
- Track API usage by client
- Monitor token issuance
- Security anomaly detection

**MCP Server Monitoring:**
- Log all bearer token validation attempts
- Track which scopes are being used
- Monitor for unusual access patterns
- Alert on repeated 401/403 responses

---

## Related to Our Implementation

### Alignment with Our Decisions

**Our Decision:** Resource Server pattern (Option B)
- ✅ **Confirmed:** Auth0 documentation explicitly shows this pattern
- ✅ **No variations:** Standard OAuth 2.1 Resource Server implementation
- ✅ **Best practice:** Matches Okta Director of Identity Standards recommendation

**Our Scopes:** `mcp:read`, `mcp:execute`, `mcp:admin`
- ✅ **Naming is good:** Auth0 docs don't prescribe specific MCP scope names
- ✅ **Standard pattern:** Using `resource:action` format is industry standard
- ✅ **Compatible:** Auth0 supports any custom scope naming

**Scope Purposes:**
- `mcp:read` - Read-only operations (resources, health checks, analytics)
- `mcp:execute` - Tool execution (search, recommendations, system operations)
- `mcp:admin` - Administrative operations (future: manage knowledge bases, users)

**Our Architecture:** Knowledge.Mcp validates tokens from Auth0
- ✅ **Correct approach:** Use `Microsoft.AspNetCore.Authentication.JwtBearer`
- ✅ **No Auth0-specific packages needed:** Standard JWT validation works
- ✅ **JWKS endpoint:** `https://contextbridge.eu.auth0.com/.well-known/jwks.json`

**Configuration Already in Place:**
- [McpServerSettings.cs:277-308](../Knowledge.Mcp/Configuration/McpServerSettings.cs#L277-L308) - OAuthSettings class
- [appsettings.json:133](../Knowledge.Mcp/appsettings.json#L133) - OAuth placeholder
- CORS already configured for credentials (required for OAuth)

---

## Implementation Checklist Updates

Based on Auth0 documentation, updates needed:

- [x] **Scope naming conventions** - Our scopes (mcp:read, mcp:execute, mcp:admin) are good
- [x] **Token validation approach** - Use Microsoft.AspNetCore.Authentication.JwtBearer
- [ ] **Error handling patterns** - Need to add WWW-Authenticate header on 401
- [ ] **Client integration examples** - Need to document OAuth flow for clients
- [ ] **Testing strategies** - Need to test with real Auth0 tokens

**Additional Updates Required:**
- [ ] Add `/.well-known/oauth-authorization-server` endpoint
- [ ] Add `/.well-known/oauth-protected-resource` endpoint
- [ ] Configure WWW-Authenticate header middleware
- [ ] Document stateless authentication (no sessions)
- [ ] Add [Authorize] to all protected endpoints
- [ ] Implement scope checking in tool endpoints

---

## Code Examples from Auth0 Docs

### Example 1: Token Validation Middleware
```csharp
// Startup.cs or Program.cs
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://contextbridge.eu.auth0.com";
        options.Audience = "https://knowledge-manager-mcp";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(300)
        };
    });

app.UseAuthentication();
app.UseAuthorization();
```

### Example 2: Scope Checking in Endpoints
```csharp
[Authorize]
[HttpPost("/tools/{toolName}")]
public async Task<IActionResult> ExecuteTool(string toolName)
{
    // Extract scopes from validated token
    var scopes = User.Claims
        .Where(c => c.Type == "scope")
        .SelectMany(c => c.Value.Split(' '))
        .ToList();

    // Check required scope
    if (!scopes.Contains("mcp:execute"))
    {
        _logger.LogWarning(
            "User {UserId} attempted to execute tool {ToolName} without mcp:execute scope",
            User.FindFirst("sub")?.Value,
            toolName
        );
        return Forbid(); // 403 - has valid token but wrong scope
    }

    // Execute tool
    var result = await _toolExecutor.ExecuteAsync(toolName);
    return Ok(result);
}
```

### Example 3: WWW-Authenticate Header Middleware
```csharp
app.Use(async (context, next) =>
{
    await next();

    // Add WWW-Authenticate header on 401 responses
    if (context.Response.StatusCode == 401)
    {
        var serverUrl = "https://knowledge-mcp.example.com"; // From config
        context.Response.Headers.WWWAuthenticate =
            $"Bearer realm=\"mcp-server\", " +
            $"authorization_uri=\"{serverUrl}/.well-known/oauth-authorization-server\"";
    }
});
```

### Example 4: Metadata Endpoint
```csharp
app.MapGet("/.well-known/oauth-authorization-server", () =>
{
    var auth0Domain = "https://contextbridge.eu.auth0.com";

    return Results.Json(new
    {
        authorization_endpoint = $"{auth0Domain}/authorize",
        token_endpoint = $"{auth0Domain}/oauth/token",
        issuer = $"{auth0Domain}/",
        jwks_uri = $"{auth0Domain}/.well-known/jwks.json"
    });
}).AllowAnonymous(); // Must be accessible without authentication
```

---

## Differences from Generic OAuth 2.1

### MCP-Specific Requirements

1. **Protected Resource Metadata Endpoint (RFC 9728)**
   - MUST implement `/.well-known/oauth-authorization-server`
   - Points clients to authorization server
   - Generic OAuth APIs often skip this

2. **WWW-Authenticate Header on 401**
   - MUST include `authorization_uri` parameter
   - Enables automatic OAuth flow discovery
   - More strict than generic APIs

3. **Stateless Authentication Requirement**
   - MCP specification explicitly prohibits sessions
   - More strict than generic OAuth 2.1
   - MUST validate token on every request

4. **Tool-Level Scope Enforcement**
   - Each MCP tool should check specific scopes
   - More granular than typical API endpoint authorization
   - Enables fine-grained access control

### Auth0-Specific Features

1. **Automatic JWKS Rotation**
   - Auth0 rotates signing keys automatically
   - Middleware fetches new keys from JWKS endpoint
   - No manual key management needed

2. **Built-in PKCE Support**
   - PKCE automatically enforced for public clients
   - No server-side configuration needed
   - Meets MCP requirement

3. **Metadata Discovery Endpoint**
   - Auth0 provides `/.well-known/oauth-authorization-server`
   - Our MCP server returns this URL to clients
   - Clients discover endpoints dynamically

4. **Actions/Rules for Custom Claims**
   - Can add custom claims via Auth0 Actions
   - Example: Add user roles, organization info
   - Claims available in token for authorization decisions

---

## Action Items After Reading

### Immediate Actions (Before Implementation)
- [ ] **Update AUTH0_SETUP_GUIDE.md** with newly discovered requirements
  - Add metadata endpoint configuration steps
  - Document WWW-Authenticate header requirement
  - Emphasize stateless authentication requirement

- [ ] **Complete Auth0 Tenant Configuration**
  - Create API: "Knowledge Manager MCP API"
  - Set audience: `https://knowledge-manager-mcp`
  - Add scopes: `mcp:read`, `mcp:execute`, `mcp:admin`
  - Create test M2M application
  - Test token generation with `curl`

### Documentation Updates
- [ ] **Update OAUTH_RESEARCH_NOTES.md**
  - Add Auth0-specific implementation details
  - Update timeline with newly discovered requirements
  - Add code examples from Auth0 docs

- [ ] **Update CLAUDE.md Milestone #23**
  - Add Week 1 tasks: metadata endpoints, WWW-Authenticate header
  - Add Week 2 tasks: scope enforcement patterns
  - Add Week 3 tasks: metadata discovery testing

### Configuration Updates
- [ ] **Update appsettings.json**
  ```json
  "OAuth": {
    "Enabled": false,
    "AuthorizationServerUrl": "https://contextbridge.eu.auth0.com",
    "Audience": "https://knowledge-manager-mcp",
    "RequirePkce": true,
    "RequiredScopes": ["mcp:read", "mcp:execute"]
  }
  ```

### Code Preparation
- [ ] **Add NuGet Package**
  - `Microsoft.AspNetCore.Authentication.JwtBearer` (latest stable)
  - ⚠️ **NOT** `Auth0.AspNetCore.Authentication` (that's for web apps with cookies)

- [ ] **Create Middleware Files**
  - `Knowledge.Mcp/Middleware/OAuthAuthenticationMiddleware.cs`
  - `Knowledge.Mcp/Middleware/WWWAuthenticateHeaderMiddleware.cs`
  - `Knowledge.Mcp/Middleware/SecureSessionMiddleware.cs` (NEW - session ID binding)

- [ ] **Create Endpoint Files**
  - `Knowledge.Mcp/Endpoints/WellKnownEndpoints.cs` (metadata endpoints)

### Security Verification (NEW - From Security Best Practices)
- [ ] **Verify Audience Validation**
  - Confirm `ValidateAudience = true` in JWT middleware
  - Test rejection of tokens with wrong audience
  - Log audience validation failures

- [ ] **Review Session ID Generation**
  - Check if using secure random UUIDs (Guid.NewGuid())
  - Verify no predictable/sequential IDs
  - Implement session expiration if not present

- [ ] **Implement Session ID Binding**
  - Bind session IDs to user ID from token (`sub` claim)
  - Format: `<user_id>:<session_id>`
  - Validate session user matches token user on operations

- [ ] **Verify Stateless Authentication**
  - Confirm no session state stored server-side
  - All authentication via bearer tokens only
  - [Authorize] on all protected endpoints

### Testing Preparation
- [ ] **Create Test Plan**
  - Test with valid Auth0 token
  - Test with expired token (expect 401 + WWW-Authenticate)
  - Test with wrong audience (expect 401)
  - Test with valid token but wrong scope (expect 403)
  - Test metadata endpoint discovery

- [ ] **Security Attack Testing (NEW)**
  - **Token Passthrough Prevention:**
    - Attempt to use token with different audience → expect 401
    - Verify `ValidateAudience = true` is enforced
    - Test with token missing `aud` claim → expect 401

  - **Session Hijacking Prevention:**
    - Test session ID with different user's token → expect 401
    - Verify session IDs are non-sequential (inspect multiple sessions)
    - Test session expiration after timeout
    - Verify no session-based authentication (only bearer tokens accepted)

  - **Stateless Authentication:**
    - Make request with valid token → success
    - Make same request again (no session state) → must validate token again
    - Verify [Authorize] on all protected endpoints
    - Test removal of Authorization header → expect 401

---

**Last Updated:** 2025-10-22
**Reading Status:**
- ✅ Auth0 MCP Authorization Guide COMPLETE
- ✅ Auth0 MCP Security Best Practices COMPLETE
- ✅ Auth0 ASP.NET Core Integration Guide COMPLETE
- 📚 Auth0 AI Agents Overview (Next)

**Next Step:** Read Auth0 AI Agents Overview (https://auth0.com/ai/docs/intro/overview)

---

## Summary of Critical Findings

### SDK Selection (CRITICAL)

**⚠️ MUST Use JWT Bearer Authentication, NOT Auth0 Web App SDK**

| SDK | Package | Use Case | MCP Compatible |
|-----|---------|----------|----------------|
| **JWT Bearer** | Microsoft.AspNetCore.Authentication.JwtBearer | APIs, MCP Servers | ✅ YES |
| **Auth0 Web App** | Auth0.AspNetCore.Authentication | Web apps with login UI | ❌ NO |

**Why This Matters:**
- Auth0.AspNetCore.Authentication creates **cookie-based sessions** (violates MCP spec)
- MCP servers MUST use **stateless JWT bearer token** authentication
- Configuration similar (Authority, Audience) but middleware is different

**Correct Implementation:**
```csharp
// ✅ CORRECT for MCP server
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://contextbridge.eu.auth0.com";
        options.Audience = "https://knowledge-manager-mcp";
    });

// ❌ WRONG for MCP server (creates sessions)
builder.Services.AddAuth0WebAppAuthentication(options =>
{
    options.Domain = "contextbridge.eu.auth0.com";
    options.ClientId = "client-id"; // This is for web apps
});
```

---

### Three Attack Vectors Identified

**1. Confused Deputy Problem** ✅ Not Applicable
- Only affects MCP proxy servers
- Knowledge Manager is direct MCP server, not proxy
- No mitigation needed for our implementation

**2. Token Passthrough** ⚠️ CRITICAL - Must Implement
- **MUST validate audience claim** = `https://knowledge-manager-mcp`
- **MUST set ValidateAudience = true** in JWT middleware
- **MUST reject tokens issued for other services**
- Prevents security control circumvention and trust boundary issues

**3. Session Hijacking** ⚠️ CRITICAL - Partially Implemented
- **Already Secure:**
  - ✅ Stateless authentication (JWT bearer tokens)
  - ✅ Token validated on every request
  - ✅ No session-based authentication

- **Need to Implement:**
  - 🔄 Bind session IDs to user ID: `<user_id>:<session_id>`
  - 🔄 Verify session ID generation uses secure random (Guid.NewGuid())
  - 🔄 Implement session expiration/rotation

### Implementation Impact on Milestone #23

**Additional Requirements Discovered:**
1. Audience validation enforcement (Week 1)
2. Session ID binding to user ID (Week 2)
3. Security attack testing (Week 3)

**Updated Timeline:**
- Week 1: JWT middleware + metadata endpoints + **audience validation**
- Week 2: Scope authorization + **session ID binding**
- Week 3: Testing + **security attack testing**

**No timeline extension needed** - security features integrate into existing weeks.

# OAuth 2.1 Research Notes for MCP Implementation

**Date:** 2025-10-22
**Status:** Research Phase
**Source:** https://auth0.com/blog/an-introduction-to-mcp-and-authorization/

---

## Key Decision: Third-Party Authorization Flow

**Chosen Architecture:** Leverage existing identity infrastructure by delegating authorization to a third-party identity provider (like Auth0).

### Why Third-Party Auth?

The MCP Authorization Specification explicitly supports flows where the MCP server delegates the actual user login process to a trusted Third-Party Identity Provider. This approach offers:

1. **Separation of Concerns** - MCP server focuses on MCP protocol, not user management
2. **Enterprise Integration** - Use existing corporate identity systems (Azure AD, Okta, Auth0)
3. **Security** - Leverage battle-tested identity providers
4. **Compliance** - Meet enterprise security requirements (SSO, MFA, audit logs)
5. **User Experience** - Users authenticate once, access multiple MCP servers

---

## Authorization Flow Architecture

**Flow Overview:**

```
┌─────────────┐                                    ┌─────────────────────┐
│             │  1. Initiate OAuth Flow            │                     │
│  MCP Client │───────────────────────────────────>│   MCP Server        │
│             │                                     │  (Knowledge.Mcp)    │
└─────────────┘                                    └─────────────────────┘
                                                             │
                                                             │ 2. Redirect to
                                                             │    Auth Server
                                                             v
┌─────────────┐                                    ┌─────────────────────┐
│             │  3. User Authorizes                │   Third-Party       │
│    User     │<──────────────────────────────────>│   Auth Server       │
│  (Browser)  │    (Login + Consent)               │   (Auth0/Azure AD)  │
└─────────────┘                                    └─────────────────────┘
      │                                                      │
      │ 4. Redirect with                                    │
      │    Authorization Code                               │
      v                                                      │
┌─────────────────────┐                                     │
│   MCP Server        │<────────────────────────────────────┘
│  (Knowledge.Mcp)    │
│                     │
│  5. Exchange code   │──────────────────────┐
│     for token       │                      │
│                     │<─────────────────────┘
│  6. Generate own    │
│     access token    │
│     bound to        │
│     third-party     │
│     session         │
└─────────────────────┘
      │
      │ 7. Complete OAuth flow
      │    with MCP client
      v
┌─────────────┐
│  MCP Client │
└─────────────┘
```

---

## Detailed Flow Steps

### Step 1: MCP Client Initiates OAuth Flow with MCP Server
- **Protocol:** OAuth 2.1 Authorization Code Flow with PKCE
- **Client Request:** `GET /authorize?client_id=...&redirect_uri=...&code_challenge=...`
- **MCP Server Role:** Act as OAuth Authorization Server for the MCP client

### Step 2: MCP Server Redirects User to Third-Party Authorization Server
- **MCP Server** → **Auth0/Azure AD**
- **Purpose:** Delegate user authentication to trusted identity provider
- **Parameters:** Include MCP-specific scopes, state, etc.

### Step 3: User Authorizes with Third-Party Server
- **User Experience:** Standard OAuth consent screen
- **Actions:**
  - User logs in to Auth0/Azure AD (if not already)
  - User sees requested permissions (scopes)
  - User grants/denies access
- **Output:** Authorization code from third-party server

### Step 4: Third-Party Server Redirects Back to MCP Server
- **Redirect URI:** MCP Server callback endpoint
- **Payload:** Authorization code from third-party auth server
- **Validation:** MCP server validates state parameter

### Step 5: MCP Server Exchanges Code for Third-Party Access Token
- **MCP Server** → **Auth0/Azure AD Token Endpoint**
- **Request:** POST with authorization code, client credentials, PKCE verifier
- **Response:** Access token, refresh token, ID token from third-party
- **Validation:** MCP server validates third-party token

### Step 6: MCP Server Generates Its Own Access Token
- **Critical Step:** MCP server creates its own access token for the MCP client
- **Token Binding:** Bound to the third-party authentication session
- **Token Contents:**
  - User identity from third-party token
  - MCP-specific scopes (mcp:read, mcp:execute)
  - Expiration matching or shorter than third-party token
  - Session reference to third-party token

### Step 7: MCP Server Completes Original OAuth Flow with MCP Client
- **Response:** MCP server returns authorization code to MCP client
- **Client Exchange:** MCP client exchanges code for MCP access token
- **Final Result:** MCP client has access token to call MCP tools/resources

---

## Key Architectural Decisions

### Decision 1: MCP Server as OAuth Authorization Server

**The MCP server acts in TWO OAuth roles:**

1. **Authorization Server** (for MCP clients)
   - Issues MCP-specific access tokens
   - Validates PKCE challenges
   - Manages MCP sessions

2. **OAuth Client** (for third-party auth servers)
   - Redirects users to Auth0/Azure AD
   - Receives authorization codes
   - Exchanges codes for third-party tokens

**This is sometimes called "OAuth Federation" or "Delegated Authorization"**

### Decision 2: Token Architecture

**Two Types of Tokens:**

1. **Third-Party Access Token** (from Auth0/Azure AD)
   - Validates user identity
   - Contains user profile information
   - Stored server-side, associated with MCP session
   - Used to verify continued authorization

2. **MCP Access Token** (issued by MCP server)
   - Used by MCP clients to access MCP tools/resources
   - Contains MCP-specific scopes
   - References third-party authentication session
   - Shorter lifetime than third-party token

**Token Binding Strategy:**
```csharp
// MCP Access Token Claims
{
  "sub": "user-id-from-auth0",
  "scope": "mcp:read mcp:execute",
  "iss": "https://knowledge-manager.example.com",
  "aud": "mcp-client-id",
  "exp": 1234567890,
  "session_ref": "third-party-session-id"  // Links to stored third-party token
}
```

### Decision 3: Session Management

**Server-Side Session Storage Required:**
- Store mapping between MCP session and third-party token
- Enable token refresh without re-authentication
- Support session revocation
- Audit trail for security

**Database Schema (Conceptual):**
```sql
CREATE TABLE OAuthSessions (
    SessionId TEXT PRIMARY KEY,
    UserId TEXT NOT NULL,
    ThirdPartyAccessToken TEXT NOT NULL,
    ThirdPartyRefreshToken TEXT,
    ThirdPartyTokenExpiry DATETIME NOT NULL,
    McpClientId TEXT NOT NULL,
    Scopes TEXT NOT NULL,
    CreatedAt DATETIME NOT NULL,
    ExpiresAt DATETIME NOT NULL
);
```

---

## Implementation Considerations

### What We Need to Implement in Knowledge.Mcp

**1. OAuth Authorization Server Capabilities:**
- `/oauth/authorize` endpoint - Initiate flow, redirect to third-party
- `/oauth/callback` endpoint - Handle third-party redirect
- `/oauth/token` endpoint - Exchange codes for MCP tokens
- PKCE validation
- Session management

**2. OAuth Client Capabilities (for third-party):**
- HttpClient for third-party token exchange
- Third-party token validation
- Token refresh logic
- Discovery of third-party .well-known endpoints

**3. Middleware:**
- JWT Bearer token validation for MCP tokens
- Scope-based authorization for tools/resources
- Session lookup and validation

**4. Configuration:**
```json
{
  "OAuth": {
    "Enabled": true,
    "ThirdPartyProvider": {
      "Type": "Auth0",  // or "AzureAD", "Cognito"
      "Domain": "your-tenant.auth0.com",
      "ClientId": "your-mcp-server-client-id",
      "ClientSecret": "your-mcp-server-client-secret",
      "Audience": "https://knowledge-manager.example.com"
    },
    "McpServerIssuer": "https://knowledge-manager.example.com",
    "AccessTokenLifetime": 3600,
    "RequiredScopes": ["mcp:read", "mcp:execute"]
  }
}
```

---

## Benefits of This Approach

### 1. Enterprise-Ready
- Integrate with existing corporate identity systems
- Support SSO, MFA, conditional access policies
- Centralized user management

### 2. Security
- No password storage in MCP server
- Leverage Auth0/Azure AD security features
- Token revocation support
- Audit logs from identity provider

### 3. Scalability
- Stateless MCP tokens (JWT)
- Session storage can be distributed (Redis, etc.)
- Support multiple identity providers

### 4. User Experience
- Single sign-on across MCP servers
- Familiar login experience
- Consistent consent screens

### 5. Compliance
- GDPR, HIPAA, SOC2 compliance through identity provider
- Audit trails
- User consent management

---

## Challenges and Considerations

### 1. Complexity
- More moving parts than simple token validation
- Requires OAuth server implementation in MCP server
- Session management overhead

### 2. State Management
- Need to track third-party sessions
- Token refresh logic
- Session expiration handling

### 3. Error Handling
- Third-party auth server unavailable
- Token exchange failures
- Network issues

### 4. Testing
- Harder to test than simple token validation
- Need to mock third-party auth server
- Integration testing requirements

---

## Alternative Simpler Approach (For Comparison)

**If we DIDN'T use third-party auth delegation:**

MCP server would just validate tokens issued by Auth0/Azure AD directly:

```
┌─────────────┐                      ┌─────────────────────┐
│             │  1. Get token from   │   Auth0/Azure AD    │
│  MCP Client │─────────────────────>│                     │
│             │     Auth0 directly   │                     │
└─────────────┘                      └─────────────────────┘
      │                                        │
      │ 2. Token                               │
      │<───────────────────────────────────────┘
      │
      │ 3. Use token to call MCP
      v
┌─────────────────────┐
│   MCP Server        │
│  (Validates token)  │
└─────────────────────┘
```

**Pros:** Simpler implementation, MCP server just validates tokens
**Cons:** MCP client must know about Auth0, no MCP-specific token customization

---

## ✅ DECISION: Option B - Resource Server Only (RECOMMENDED)

**Decision Date:** 2025-10-22
**Decision Maker:** Project team
**Supporting Evidence:** Okta Director of Identity Standards recommendation

### Expert Validation

**Source:** Aaron Parecki (Director of Identity Standards at Okta)
**Video:** https://www.youtube.com/watch?v=mYKMwZcGynw
**Recommendation:** MCP servers should act as **Resource Servers only**, not full OAuth Authorization Servers

### Why Option B is Better

**Option B: Direct Token Validation (MCP Server as Resource Server ONLY)**

**Architecture:**
```
┌─────────────┐                      ┌─────────────────────┐
│             │  1. Get token from   │   Auth0/Azure AD    │
│  MCP Client │─────────────────────>│  (Authorization     │
│             │     Auth0 directly   │   Server)           │
└─────────────┘                      └─────────────────────┘
      │                                        │
      │ 2. Access token                        │
      │    (with mcp:read, mcp:execute scopes) │
      │<───────────────────────────────────────┘
      │
      │ 3. Use token to call MCP tools/resources
      v
┌─────────────────────┐
│   MCP Server        │
│  (Resource Server)  │
│  - Validates token  │
│  - Checks scopes    │
│  - Serves tools     │
└─────────────────────┘
```

**Responsibilities:**

**Authorization Server (Auth0/Azure AD):**
- User authentication (login)
- User consent
- Token issuance
- Token refresh
- User management
- MFA, SSO, etc.

**MCP Server (Knowledge.Mcp):**
- Token validation (signature, expiration, issuer)
- Scope verification (mcp:read, mcp:execute)
- Tool/resource authorization
- Business logic

**MCP Client:**
- OAuth flow with Auth0 (gets token)
- Includes token in MCP requests
- Token refresh when expired

### Benefits of Option B

1. **✅ Simpler Implementation**
   - No OAuth Authorization Server to build
   - Just token validation middleware
   - Weeks to implement, not months

2. **✅ Industry Best Practice**
   - Recommended by identity standards experts
   - Separation of concerns (auth vs resource serving)
   - Proven pattern used by millions of APIs

3. **✅ Easier to Test**
   - Token validation is straightforward
   - No complex OAuth flows to test server-side
   - Mock tokens for unit tests

4. **✅ Better Security**
   - Leverage Auth0/Azure AD security expertise
   - Regular security updates from identity provider
   - No password/credential storage

5. **✅ Flexibility**
   - Clients can use any OAuth-compatible auth server
   - Support multiple identity providers
   - Easy to switch providers

6. **✅ Enterprise Integration**
   - Works with existing corporate identity systems
   - SSO, MFA, conditional access
   - Audit logs from identity provider

7. **✅ Scalability**
   - Stateless token validation
   - No session storage needed
   - Horizontal scaling easy

### What Option B Does NOT Require

- ❌ OAuth Authorization Server implementation
- ❌ User database in MCP server
- ❌ Session management
- ❌ Token issuance logic
- ❌ Consent screens
- ❌ User authentication flows

### What Option B DOES Require

- ✅ JWT Bearer token validation
- ✅ Scope-based authorization
- ✅ WWW-Authenticate header for 401 responses
- ✅ Configuration for trusted issuers
- ✅ JWKS (JSON Web Key Set) endpoint discovery

**Implementation Effort:** ~2-3 weeks vs ~3-6 months for full OAuth AS

### Comparison Table

| Aspect | Option A (Full OAuth AS) | Option B (Resource Server) | Winner |
|--------|-------------------------|---------------------------|--------|
| **Complexity** | High - build OAuth server | Low - validate tokens | ✅ B |
| **Time to Implement** | 3-6 months | 2-3 weeks | ✅ B |
| **Security** | Complex, self-managed | Delegate to experts | ✅ B |
| **Maintenance** | High - OAuth spec changes | Low - stable APIs | ✅ B |
| **Enterprise Ready** | Custom integration needed | Works with existing IdP | ✅ B |
| **Flexibility** | Locked to our impl | Any OAuth provider | ✅ B |
| **Testing** | Complex integration tests | Simple unit tests | ✅ B |
| **Industry Standard** | Over-engineering | Recommended pattern | ✅ B |

**Winner: Option B (Resource Server Only)** - 8/8 categories

### Decision: Implement Option B for Milestone #23

**Scope for Milestone #23:**
1. JWT Bearer token validation
2. Scope-based authorization (mcp:read, mcp:execute)
3. Support for Auth0 as primary identity provider
4. WWW-Authenticate header responses
5. Configuration for trusted issuers
6. Documentation for client OAuth flow
7. Testing with real Auth0 tokens

**Future Enhancements (Not Milestone #23):**
- Support multiple identity providers (Azure AD, AWS Cognito)
- Fine-grained permissions beyond scopes
- Token introspection endpoints
- Admin UI for authorization management

---

## Next Research Tasks

- [ ] Study how to implement OAuth Authorization Server in ASP.NET Core
- [ ] Research session management strategies (in-memory, Redis, database)
- [ ] Learn about token exchange flows (RFC 8693)
- [ ] Understand Auth0 tenant configuration for third-party delegation
- [ ] Review Microsoft.AspNetCore.Authentication.OAuth package
- [ ] Study Duende IdentityServer as reference implementation

---

## Resources

- **MCP + Auth0:** https://auth0.com/blog/an-introduction-to-mcp-and-authorization/
- **OAuth 2.1:** https://datatracker.ietf.org/doc/html/draft-ietf-oauth-v2-1
- **Token Exchange (RFC 8693):** https://datatracker.ietf.org/doc/html/rfc8693
- **PKCE (RFC 7636):** https://datatracker.ietf.org/doc/html/rfc7636

---

**Last Updated:** 2025-10-22
**Status:** Initial research - understanding authorization flow options

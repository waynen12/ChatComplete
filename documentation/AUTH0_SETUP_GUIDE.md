# Auth0 Setup Guide for Knowledge Manager MCP Server

**Date:** 2025-10-22
**Tenant:** `contextbridge.eu.auth0.com`
**Purpose:** OAuth 2.1 authentication for MCP server (Milestone #23)

---

## Overview

This guide walks through setting up Auth0 as the identity provider for the Knowledge Manager MCP Server. The MCP server will act as a **Resource Server** that validates tokens issued by Auth0.

**Architecture:**
```
MCP Client → Auth0 (get token) → MCP Server (validate token)
```

---

## Step 1: Create API in Auth0 ✅

Auth0 APIs represent your backend services (Resource Servers). We need to create an API for the Knowledge Manager MCP Server.

### Navigate to APIs
1. Log in to Auth0 Dashboard: https://manage.auth0.com
2. Go to **Applications** → **APIs**
3. Click **Create API**

### API Configuration

**Name:** `Knowledge Manager MCP API`

**Identifier (Audience):**
```
https://knowledge-manager-mcp
```
> **Important:** This is the "audience" claim in tokens. Use a URL-like identifier (doesn't need to be real URL).
> This value will go in your `appsettings.json` later.

**Signing Algorithm:** `RS256`
> This is the default and what we'll use for JWT validation.

Click **Create**

---

## Step 2: Define Custom Scopes

Scopes define what permissions a token grants. For MCP, we need:

### Navigate to Permissions
1. In your newly created API, go to the **Permissions** tab
2. Add the following scopes:

### MCP Scopes

| Scope | Description |
|-------|-------------|
| `mcp:read` | Read-only access to MCP resources and health checks |
| `mcp:execute` | Execute MCP tools (search, analytics, recommendations) |
| `mcp:admin` | Administrative operations (future: manage knowledge bases) |

**Add Each Scope:**
1. Click **Add Permission**
2. Enter scope value (e.g., `mcp:read`)
3. Enter description
4. Click **Add**

---

## Step 3: Create Test Application (Machine-to-Machine)

For testing, we'll create a Machine-to-Machine (M2M) application that can get tokens.

### Navigate to Applications
1. Go to **Applications** → **Applications**
2. Click **Create Application**

### Application Configuration

**Name:** `MCP Test Client`

**Application Type:** `Machine to Machine Applications`

**Authorize:** Select `Knowledge Manager MCP API`

**Permissions:** Grant all scopes:
- ✅ `mcp:read`
- ✅ `mcp:execute`
- ✅ `mcp:admin`

Click **Authorize**

### Get Credentials

After creation, go to the **Settings** tab and note:

**Domain:**
```
contextbridge.eu.auth0.com
```

**Client ID:**
```
[Your M2M Client ID - will be shown in dashboard]
```

**Client Secret:**
```
[Your M2M Client Secret - will be shown in dashboard]
```

> **Security Note:** Keep the Client Secret secure! It's like a password.

---

## Step 4: Test Token Generation

Let's verify Auth0 is configured correctly by getting a test token.

### Using cURL

```bash
curl --request POST \
  --url https://contextbridge.eu.auth0.com/oauth/token \
  --header 'content-type: application/json' \
  --data '{
    "client_id": "YOUR_CLIENT_ID",
    "client_secret": "YOUR_CLIENT_SECRET",
    "audience": "https://knowledge-manager-mcp",
    "grant_type": "client_credentials"
  }'
```

**Expected Response:**
```json
{
  "access_token": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6IjEyMyJ9...",
  "token_type": "Bearer",
  "expires_in": 86400
}
```

### Decode Token

Copy the `access_token` and paste it into https://jwt.io

**Expected Claims:**
```json
{
  "iss": "https://contextbridge.eu.auth0.com/",
  "sub": "YOUR_CLIENT_ID@clients",
  "aud": "https://knowledge-manager-mcp",
  "iat": 1234567890,
  "exp": 1234654290,
  "scope": "mcp:read mcp:execute mcp:admin",
  "gty": "client-credentials"
}
```

**Verify:**
- ✅ `iss` matches your Auth0 domain
- ✅ `aud` matches your API identifier
- ✅ `scope` contains your MCP scopes
- ✅ `exp` is in the future

---

## Step 5: Get JWKS Endpoint

The JWKS (JSON Web Key Set) endpoint contains public keys for verifying token signatures.

**Your JWKS Endpoint:**
```
https://contextbridge.eu.auth0.com/.well-known/jwks.json
```

**Test it:**
```bash
curl https://contextbridge.eu.auth0.com/.well-known/jwks.json
```

**Expected Response:**
```json
{
  "keys": [
    {
      "kty": "RSA",
      "use": "sig",
      "kid": "...",
      "n": "...",
      "e": "AQAB"
    }
  ]
}
```

---

## Step 6: Configure Knowledge.Mcp

Now we'll update the MCP server configuration to use Auth0.

### Update appsettings.json

File: `Knowledge.Mcp/appsettings.json`

```json
{
  "McpServerSettings": {
    "HttpTransport": {
      "OAuth": {
        "Enabled": true,
        "AuthorizationServerUrl": "https://contextbridge.eu.auth0.com",
        "Audience": "https://knowledge-manager-mcp",
        "RequirePkce": true,
        "TokenValidation": {
          "ValidateAudience": true,
          "ValidateIssuer": true,
          "ValidateLifetime": true,
          "ClockSkewSeconds": 300
        },
        "RequiredScopes": ["mcp:read", "mcp:execute"]
      }
    }
  }
}
```

### Configuration Explanation

| Setting | Value | Purpose |
|---------|-------|---------|
| `Enabled` | `true` | Turn on OAuth validation |
| `AuthorizationServerUrl` | `https://contextbridge.eu.auth0.com` | Your Auth0 tenant |
| `Audience` | `https://knowledge-manager-mcp` | Must match API identifier |
| `RequirePkce` | `true` | Required by MCP spec (enforced client-side) |
| `ValidateAudience` | `true` | Reject tokens for wrong API |
| `ValidateIssuer` | `true` | Reject tokens from wrong tenant |
| `ValidateLifetime` | `true` | Reject expired tokens |
| `ClockSkewSeconds` | `300` | Allow 5 min clock difference |
| `RequiredScopes` | `["mcp:read", "mcp:execute"]` | Minimum scopes needed |

---

## Step 7: Implementation Checklist

**Auth0 Configuration:** ✅
- [x] Tenant created: `contextbridge.eu.auth0.com`
- [x] API created: `Knowledge Manager MCP API`
- [x] API Identifier set: `https://knowledge-manager-mcp`
- [x] Scopes defined: `mcp:read`, `mcp:execute`, `mcp:admin`
- [x] Test M2M application created
- [x] Token generation tested
- [x] JWKS endpoint verified

**MCP Server Configuration:** ⏳ TODO
- [ ] Add `Microsoft.AspNetCore.Authentication.JwtBearer` package
- [ ] Configure JWT Bearer authentication in `Program.cs`
- [ ] Set Auth0 settings in `appsettings.json`
- [ ] Add authorization policies for scopes
- [ ] Apply `[Authorize]` attributes to endpoints
- [ ] Test token validation

---

## Security Best Practices

### Production Considerations

1. **Separate Tenants**
   - Development: `contextbridge-dev.eu.auth0.com`
   - Production: `contextbridge.eu.auth0.com`

2. **Environment Variables**
   - Store Client ID/Secret in environment variables
   - Never commit secrets to git
   - Use different M2M apps for dev/prod

3. **Token Lifetimes**
   - Access tokens: 1 hour (3600 seconds)
   - Refresh tokens: 7 days (for user flows)
   - Adjust in Auth0 API settings → Token Settings

4. **Scope Granularity**
   - Start with `mcp:read` and `mcp:execute`
   - Add `mcp:admin` only when needed
   - Consider per-tool scopes for fine-grained control

5. **Monitoring**
   - Enable Auth0 logs
   - Monitor failed auth attempts
   - Set up alerts for anomalies

---

## Testing Strategy

### Unit Tests
- Mock JWT Bearer authentication
- Test scope-based authorization
- Verify 401 responses for invalid tokens

### Integration Tests
- Get real token from Auth0
- Call MCP endpoints with token
- Verify scope enforcement
- Test expired tokens

### Manual Testing
```bash
# 1. Get token from Auth0
TOKEN=$(curl --request POST \
  --url https://contextbridge.eu.auth0.com/oauth/token \
  --header 'content-type: application/json' \
  --data '{
    "client_id": "YOUR_CLIENT_ID",
    "client_secret": "YOUR_CLIENT_SECRET",
    "audience": "https://knowledge-manager-mcp",
    "grant_type": "client_credentials"
  }' | jq -r '.access_token')

# 2. Call MCP server with token
curl -X POST http://localhost:5001 \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -H "Accept: application/json, text/event-stream" \
  -d '{"jsonrpc":"2.0","id":1,"method":"tools/list"}'
```

---

## Troubleshooting

### Common Issues

**Issue: "Invalid token"**
- Check token not expired (decode at jwt.io)
- Verify `aud` matches API identifier
- Verify `iss` matches Auth0 domain

**Issue: "Insufficient scope"**
- Check token contains required scopes
- Verify scopes granted in Auth0 M2M app
- Check authorization policy in MCP server

**Issue: "Signature verification failed"**
- Verify JWKS endpoint accessible
- Check firewall/network rules
- Verify signing algorithm is RS256

**Issue: "Token missing"**
- Ensure `Authorization: Bearer <token>` header
- Check token not empty/malformed
- Verify client sending header correctly

---

## Next Steps

1. ✅ Auth0 configured (completed in this guide)
2. ⏳ Implement JWT Bearer authentication in MCP server (Week 1)
3. ⏳ Add scope-based authorization (Week 2)
4. ⏳ Test with real tokens (Week 3)

---

## References

- **Auth0 Documentation:** https://auth0.com/docs
- **Auth0 APIs Quickstart:** https://auth0.com/docs/quickstart/backend
- **JWT.io Debugger:** https://jwt.io
- **MCP Authorization Spec:** https://spec.modelcontextprotocol.io/authorization
- **OAuth 2.1 Draft:** https://datatracker.ietf.org/doc/html/draft-ietf-oauth-v2-1

---

**Created:** 2025-10-22
**Last Updated:** 2025-10-22
**Status:** Auth0 configuration complete, ready for MCP server implementation

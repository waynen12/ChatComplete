# Dynamic Client Registration (DCR) Implementation Specification

**Project:** AI Knowledge Manager - MCP Server
**Milestone:** #23 Extension - OAuth 2.1 Dynamic Client Registration
**RFC:** RFC 7591 (OAuth 2.0 Dynamic Client Registration Protocol)
**RFC:** RFC 7592 (OAuth 2.0 Dynamic Client Registration Management Protocol)
**Status:** Not Started
**Estimated Effort:** 30-40 hours

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Requirements](#requirements)
3. [Architecture Overview](#architecture-overview)
4. [Database Schema](#database-schema)
5. [API Endpoints](#api-endpoints)
6. [Implementation Phases](#implementation-phases)
7. [Security Considerations](#security-considerations)
8. [Testing Strategy](#testing-strategy)
9. [Deployment Considerations](#deployment-considerations)
10. [Future Enhancements](#future-enhancements)

---

## Executive Summary

### What is Dynamic Client Registration?

Dynamic Client Registration (DCR) allows OAuth 2.1 clients to register themselves with the authorization server programmatically, without manual administrator intervention. Instead of pre-configuring clients in Auth0 dashboard, clients can self-register by sending a registration request to the MCP server.

### Current State vs Target State

**Current State (Manual Registration):**
```
Developer → Auth0 Dashboard → Create M2M App → Configure Scopes → Copy Credentials → Use MCP Server
Time: 15-30 minutes per client
```

**Target State (Dynamic Registration):**
```
Developer → POST /register → Receive Credentials → Use MCP Server
Time: < 1 minute per client
```

### Why Implement DCR?

**Benefits:**
- ✅ Automated client onboarding (seconds vs minutes)
- ✅ Self-service developer experience
- ✅ Scales to hundreds of clients
- ✅ Standardized registration flow (RFC 7591)
- ✅ No Auth0 dashboard dependency
- ✅ Programmatic client management (RFC 7592)

**Trade-offs:**
- ⚠️ Increased complexity (~1,500 LOC)
- ⚠️ Additional database tables and management
- ⚠️ Security surface area increases
- ⚠️ Need client approval workflow for production

### Success Criteria

- [ ] Clients can register via `/register` endpoint
- [ ] Client metadata stored in SQLite database
- [ ] Client credentials generated securely
- [ ] Client authentication works for token requests
- [ ] Client management endpoints functional (GET/PUT/DELETE)
- [ ] 100% RFC 7591/7592 compliance
- [ ] Integration tests with Auth0
- [ ] Documentation complete

---

## Requirements

### Functional Requirements

**FR-1: Client Registration (RFC 7591)**
- Client sends POST request to `/register` endpoint
- Server validates registration request
- Server generates unique `client_id` (UUID)
- Server generates `client_secret` for confidential clients
- Server stores client metadata in database
- Server returns client credentials to requester

**FR-2: Client Authentication**
- Registered clients can authenticate using `client_id` + `client_secret`
- Client Credentials grant type supported
- Authorization Code grant type supported (PKCE for public clients)

**FR-3: Client Management (RFC 7592)**
- Retrieve client metadata: `GET /register/:client_id`
- Update client metadata: `PUT /register/:client_id`
- Delete client: `DELETE /register/:client_id`
- Require `registration_access_token` for management operations

**FR-4: Security & Validation**
- Validate redirect URIs (HTTPS required, localhost allowed for development)
- Validate grant types (only `authorization_code` and `client_credentials`)
- Validate scopes (only `mcp:read`, `mcp:execute`, `mcp:admin`)
- Rate limiting on registration endpoint (max 10 requests/hour per IP)
- Client secret hashing (never store plaintext)

**FR-5: Admin Controls**
- Optional client approval workflow
- Admin API to list all registered clients
- Admin API to revoke client access
- Audit log of client registrations

### Non-Functional Requirements

**NFR-1: Performance**
- Registration request completes in < 500ms
- Client lookup completes in < 50ms
- Database indexed on `client_id` for fast lookups

**NFR-2: Security**
- Client secrets hashed using PBKDF2 (100,000 iterations)
- Registration access tokens are JWT (signed, expiring)
- Rate limiting enforced via middleware
- Input validation prevents injection attacks

**NFR-3: Reliability**
- Database transactions for atomic operations
- Retry logic for transient failures
- Graceful degradation if database unavailable

**NFR-4: Maintainability**
- Clean separation of concerns (repository pattern)
- Comprehensive unit tests (>80% coverage)
- Integration tests with real Auth0
- Documentation for future developers

---

## Architecture Overview

### Component Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                        MCP Server (HTTP Mode)                    │
└─────────────────────────────────────────────────────────────────┘
                                │
                ┌───────────────┴───────────────┐
                │                               │
        ┌───────▼────────┐             ┌───────▼────────┐
        │  Auth Endpoints │             │  MCP Endpoints │
        │  (New)          │             │  (Existing)    │
        └───────┬────────┘             └────────────────┘
                │
    ┌───────────┼───────────┬─────────────┐
    │           │           │             │
┌───▼───┐  ┌───▼───┐  ┌────▼────┐  ┌────▼────┐
│Register│  │ Get   │  │ Update  │  │ Delete  │
│Endpoint│  │Endpoint│  │Endpoint │  │Endpoint │
│(POST)  │  │(GET)  │  │(PUT)    │  │(DELETE) │
└───┬───┘  └───┬───┘  └────┬────┘  └────┬────┘
    │          │           │            │
    └──────────┴───────────┴────────────┘
                    │
            ┌───────▼────────┐
            │ Client Service │
            │ (Business Logic)│
            └───────┬────────┘
                    │
            ┌───────▼────────┐
            │Client Repository│
            │   (Data Access) │
            └───────┬────────┘
                    │
            ┌───────▼────────┐
            │  SQLite DB     │
            │ OAuthClients   │
            │ ClientScopes   │
            └────────────────┘
```

### Data Flow: Client Registration

```
┌─────────┐                                  ┌─────────────┐
│  Client │                                  │ MCP Server  │
└────┬────┘                                  └──────┬──────┘
     │                                              │
     │  1. POST /register                           │
     │  { client_name, redirect_uris, ... }         │
     ├─────────────────────────────────────────────>│
     │                                              │
     │                          2. Validate Request │
     │                          ┌──────────────────>│
     │                          │                   │
     │                          3. Generate client_id (UUID)
     │                          │                   │
     │                          4. Generate client_secret (confidential)
     │                          │                   │
     │                          5. Hash client_secret
     │                          │                   │
     │                          6. Generate registration_access_token
     │                          │                   │
     │                          7. Store in database
     │                          └──────────────────>│
     │                                              │
     │  8. Return credentials                       │
     │  { client_id, client_secret, ... }           │
     │<─────────────────────────────────────────────┤
     │                                              │
```

### Data Flow: Client Authentication

```
┌─────────┐                                  ┌─────────────┐
│  Client │                                  │ MCP Server  │
└────┬────┘                                  └──────┬──────┘
     │                                              │
     │  1. POST /oauth/token                        │
     │  { client_id, client_secret, grant_type }    │
     ├─────────────────────────────────────────────>│
     │                                              │
     │                          2. Lookup client_id │
     │                          ┌──────────────────>│
     │                          │                   │
     │                          3. Verify client_secret hash
     │                          │                   │
     │                          4. Check grant_type allowed
     │                          │                   │
     │                          5. Check scopes     │
     │                          └──────────────────>│
     │                                              │
     │  6. Return access token (JWT)                │
     │  { access_token, token_type, expires_in }    │
     │<─────────────────────────────────────────────┤
     │                                              │
```

---

## Database Schema

### OAuthClients Table

**Purpose:** Store registered OAuth client metadata

```sql
CREATE TABLE OAuthClients (
    -- Primary Key
    ClientId TEXT PRIMARY KEY NOT NULL,  -- UUID v4 (e.g., "7a42b3f1-e9c5-4d8a-b2f6-3e1c9a8d7b4f")

    -- Client Credentials
    ClientSecretHash TEXT,               -- PBKDF2 hash (confidential clients only, NULL for public)
    ClientSecretSalt TEXT,               -- Salt for PBKDF2 (confidential clients only, NULL for public)
    RegistrationAccessToken TEXT NOT NULL UNIQUE, -- JWT for client management operations

    -- Client Metadata (RFC 7591)
    ClientName TEXT NOT NULL,            -- Human-readable name (e.g., "My MCP Client")
    ClientType TEXT NOT NULL CHECK(ClientType IN ('confidential', 'public')), -- Client type
    RedirectUris TEXT NOT NULL,          -- JSON array of redirect URIs (e.g., '["https://client.example.com/callback"]')
    GrantTypes TEXT NOT NULL,            -- JSON array of grant types (e.g., '["authorization_code"]')
    ResponseTypes TEXT NOT NULL,         -- JSON array of response types (e.g., '["code"]')
    TokenEndpointAuthMethod TEXT NOT NULL CHECK(TokenEndpointAuthMethod IN ('client_secret_basic', 'client_secret_post', 'none')),

    -- Optional Metadata
    LogoUri TEXT,                        -- Client logo URL
    ClientUri TEXT,                      -- Client homepage URL
    PolicyUri TEXT,                      -- Privacy policy URL
    TosUri TEXT,                         -- Terms of service URL
    Contacts TEXT,                       -- JSON array of contact emails

    -- Status & Lifecycle
    Status TEXT NOT NULL DEFAULT 'pending' CHECK(Status IN ('pending', 'approved', 'rejected', 'revoked')),
    ApprovedBy TEXT,                     -- Admin user who approved (NULL if auto-approved)
    ApprovedAt INTEGER,                  -- Unix timestamp of approval
    RevokedAt INTEGER,                   -- Unix timestamp of revocation
    RevokedReason TEXT,                  -- Reason for revocation

    -- Timestamps
    CreatedAt INTEGER NOT NULL,          -- Unix timestamp of registration
    UpdatedAt INTEGER NOT NULL,          -- Unix timestamp of last update

    -- Audit
    CreatedByIp TEXT,                    -- IP address of registration request
    LastUsedAt INTEGER                   -- Unix timestamp of last token request
);

-- Indexes for performance
CREATE INDEX idx_oauth_clients_status ON OAuthClients(Status);
CREATE INDEX idx_oauth_clients_created_at ON OAuthClients(CreatedAt);
CREATE UNIQUE INDEX idx_oauth_clients_registration_token ON OAuthClients(RegistrationAccessToken);
```

### ClientScopes Table

**Purpose:** Store allowed scopes per client (many-to-many relationship)

```sql
CREATE TABLE ClientScopes (
    ClientId TEXT NOT NULL,
    Scope TEXT NOT NULL,
    GrantedAt INTEGER NOT NULL,          -- Unix timestamp when scope was granted

    PRIMARY KEY (ClientId, Scope),
    FOREIGN KEY (ClientId) REFERENCES OAuthClients(ClientId) ON DELETE CASCADE
);

-- Index for reverse lookup (find clients with specific scope)
CREATE INDEX idx_client_scopes_scope ON ClientScopes(Scope);
```

### ClientAuditLog Table

**Purpose:** Audit trail for client lifecycle events

```sql
CREATE TABLE ClientAuditLog (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ClientId TEXT NOT NULL,
    EventType TEXT NOT NULL CHECK(EventType IN ('registered', 'approved', 'updated', 'deleted', 'revoked', 'token_issued')),
    EventData TEXT,                      -- JSON with event-specific details
    PerformedBy TEXT,                    -- User/IP who performed action
    Timestamp INTEGER NOT NULL,          -- Unix timestamp

    FOREIGN KEY (ClientId) REFERENCES OAuthClients(ClientId) ON DELETE CASCADE
);

-- Index for querying client history
CREATE INDEX idx_client_audit_client_id ON ClientAuditLog(ClientId, Timestamp);
CREATE INDEX idx_client_audit_event_type ON ClientAuditLog(EventType, Timestamp);
```

### Sample Data

```sql
-- Example: Public client (PKCE flow)
INSERT INTO OAuthClients (
    ClientId, ClientSecretHash, ClientSecretSalt, RegistrationAccessToken,
    ClientName, ClientType, RedirectUris, GrantTypes, ResponseTypes, TokenEndpointAuthMethod,
    Status, CreatedAt, UpdatedAt, CreatedByIp
) VALUES (
    '7a42b3f1-e9c5-4d8a-b2f6-3e1c9a8d7b4f',
    NULL,  -- No secret for public clients
    NULL,
    'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...',  -- JWT
    'My MCP Client App',
    'public',
    '["https://myclient.example.com/callback"]',
    '["authorization_code"]',
    '["code"]',
    'none',  -- Public client uses PKCE instead of client secret
    'approved',
    1700000000,
    1700000000,
    '192.168.1.100'
);

INSERT INTO ClientScopes (ClientId, Scope, GrantedAt) VALUES
    ('7a42b3f1-e9c5-4d8a-b2f6-3e1c9a8d7b4f', 'mcp:read', 1700000000),
    ('7a42b3f1-e9c5-4d8a-b2f6-3e1c9a8d7b4f', 'mcp:execute', 1700000000);

-- Example: Confidential client (M2M flow)
INSERT INTO OAuthClients (
    ClientId, ClientSecretHash, ClientSecretSalt, RegistrationAccessToken,
    ClientName, ClientType, RedirectUris, GrantTypes, ResponseTypes, TokenEndpointAuthMethod,
    Status, CreatedAt, UpdatedAt, CreatedByIp
) VALUES (
    '9f8e7d6c-5b4a-3210-fedc-ba9876543210',
    '$pbkdf2-sha256$100000$...',  -- Hashed secret
    'random-salt-value',
    'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...',
    'Backend Service Client',
    'confidential',
    '[]',  -- No redirect URIs for M2M
    '["client_credentials"]',
    '[]',  -- No response types for M2M
    'client_secret_post',
    'approved',
    1700001000,
    1700001000,
    '10.0.0.50'
);

INSERT INTO ClientScopes (ClientId, Scope, GrantedAt) VALUES
    ('9f8e7d6c-5b4a-3210-fedc-ba9876543210', 'mcp:read', 1700001000),
    ('9f8e7d6c-5b4a-3210-fedc-ba9876543210', 'mcp:execute', 1700001000),
    ('9f8e7d6c-5b4a-3210-fedc-ba9876543210', 'mcp:admin', 1700001000);
```

---

## API Endpoints

### 1. Client Registration Endpoint (RFC 7591)

**Endpoint:** `POST /register`
**Purpose:** Register a new OAuth client
**Authentication:** None (open registration) OR Bearer token (admin only)
**Rate Limit:** 10 requests/hour per IP

**Request:**
```http
POST /register HTTP/1.1
Host: mcp-server.example.com
Content-Type: application/json

{
  "client_name": "My MCP Client",
  "client_type": "public",  // or "confidential"
  "redirect_uris": [
    "https://myclient.example.com/callback",
    "http://localhost:3000/callback"  // Allowed for development
  ],
  "grant_types": ["authorization_code"],
  "response_types": ["code"],
  "token_endpoint_auth_method": "none",  // For public clients (PKCE)
  "scope": "mcp:read mcp:execute",

  // Optional metadata
  "logo_uri": "https://myclient.example.com/logo.png",
  "client_uri": "https://myclient.example.com",
  "policy_uri": "https://myclient.example.com/privacy",
  "tos_uri": "https://myclient.example.com/terms",
  "contacts": ["admin@myclient.example.com"]
}
```

**Response (201 Created):**
```json
{
  "client_id": "7a42b3f1-e9c5-4d8a-b2f6-3e1c9a8d7b4f",
  "client_secret": "K7gNU3sdo+OL0wNhqoVWhr3g6s1xYv72ol/pe/Unols=",  // Only for confidential clients
  "client_id_issued_at": 1700000000,
  "client_secret_expires_at": 0,  // 0 = never expires
  "registration_access_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "registration_client_uri": "https://mcp-server.example.com/register/7a42b3f1-e9c5-4d8a-b2f6-3e1c9a8d7b4f",

  // Echo back metadata
  "client_name": "My MCP Client",
  "client_type": "public",
  "redirect_uris": [
    "https://myclient.example.com/callback",
    "http://localhost:3000/callback"
  ],
  "grant_types": ["authorization_code"],
  "response_types": ["code"],
  "token_endpoint_auth_method": "none",
  "scope": "mcp:read mcp:execute",
  "status": "approved"  // or "pending" if approval workflow enabled
}
```

**Error Responses:**

```json
// 400 Bad Request - Invalid redirect URI
{
  "error": "invalid_redirect_uri",
  "error_description": "redirect_uris must use HTTPS (except localhost)"
}

// 400 Bad Request - Invalid grant type
{
  "error": "invalid_client_metadata",
  "error_description": "grant_types must be one of: authorization_code, client_credentials"
}

// 400 Bad Request - Invalid scope
{
  "error": "invalid_client_metadata",
  "error_description": "scope must be subset of: mcp:read, mcp:execute, mcp:admin"
}

// 429 Too Many Requests
{
  "error": "rate_limit_exceeded",
  "error_description": "Maximum 10 registration requests per hour",
  "retry_after": 3600
}
```

**Validation Rules:**
1. `client_name` - Required, 1-100 characters, no special characters
2. `client_type` - Required, must be "public" or "confidential"
3. `redirect_uris` - Required for authorization_code grant, must be HTTPS (except localhost)
4. `grant_types` - Required, must be subset of `["authorization_code", "client_credentials"]`
5. `response_types` - Required for authorization_code, must be `["code"]`
6. `token_endpoint_auth_method` - Required, must be "none" (public) or "client_secret_post" (confidential)
7. `scope` - Optional, must be subset of `["mcp:read", "mcp:execute", "mcp:admin"]`, default: `"mcp:read"`

---

### 2. Get Client Metadata (RFC 7592)

**Endpoint:** `GET /register/:client_id`
**Purpose:** Retrieve client metadata
**Authentication:** Bearer `registration_access_token` (required)

**Request:**
```http
GET /register/7a42b3f1-e9c5-4d8a-b2f6-3e1c9a8d7b4f HTTP/1.1
Host: mcp-server.example.com
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response (200 OK):**
```json
{
  "client_id": "7a42b3f1-e9c5-4d8a-b2f6-3e1c9a8d7b4f",
  "client_id_issued_at": 1700000000,
  "registration_access_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "registration_client_uri": "https://mcp-server.example.com/register/7a42b3f1-e9c5-4d8a-b2f6-3e1c9a8d7b4f",

  "client_name": "My MCP Client",
  "client_type": "public",
  "redirect_uris": ["https://myclient.example.com/callback"],
  "grant_types": ["authorization_code"],
  "response_types": ["code"],
  "token_endpoint_auth_method": "none",
  "scope": "mcp:read mcp:execute",
  "status": "approved"
}
```

**Error Responses:**
```json
// 401 Unauthorized - Missing or invalid token
{
  "error": "invalid_token",
  "error_description": "The registration access token is missing or invalid"
}

// 404 Not Found
{
  "error": "invalid_client_id",
  "error_description": "Client not found"
}
```

---

### 3. Update Client Metadata (RFC 7592)

**Endpoint:** `PUT /register/:client_id`
**Purpose:** Update client metadata
**Authentication:** Bearer `registration_access_token` (required)

**Request:**
```http
PUT /register/7a42b3f1-e9c5-4d8a-b2f6-3e1c9a8d7b4f HTTP/1.1
Host: mcp-server.example.com
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
  "client_name": "My Updated MCP Client",
  "redirect_uris": [
    "https://myclient.example.com/callback",
    "https://myclient.example.com/callback2"
  ],
  "scope": "mcp:read mcp:execute mcp:admin"  // Request additional scope
}
```

**Response (200 OK):**
```json
{
  "client_id": "7a42b3f1-e9c5-4d8a-b2f6-3e1c9a8d7b4f",
  "client_name": "My Updated MCP Client",
  "redirect_uris": [
    "https://myclient.example.com/callback",
    "https://myclient.example.com/callback2"
  ],
  "scope": "mcp:read mcp:execute mcp:admin",
  "updated_at": 1700001000
}
```

**Error Responses:**
```json
// 403 Forbidden - Attempted to change immutable field
{
  "error": "invalid_client_metadata",
  "error_description": "client_type cannot be changed after registration"
}
```

**Immutable Fields:**
- `client_id`
- `client_type`
- `grant_types`
- `token_endpoint_auth_method`

---

### 4. Delete Client (RFC 7592)

**Endpoint:** `DELETE /register/:client_id`
**Purpose:** Delete/revoke client registration
**Authentication:** Bearer `registration_access_token` (required)

**Request:**
```http
DELETE /register/7a42b3f1-e9c5-4d8a-b2f6-3e1c9a8d7b4f HTTP/1.1
Host: mcp-server.example.com
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response (204 No Content):**
```
(empty body)
```

**Error Responses:**
```json
// 401 Unauthorized
{
  "error": "invalid_token"
}

// 404 Not Found
{
  "error": "invalid_client_id"
}
```

---

### 5. Admin: List All Clients (Extension)

**Endpoint:** `GET /admin/clients`
**Purpose:** List all registered clients (admin only)
**Authentication:** Bearer token with `mcp:admin` scope (required)

**Request:**
```http
GET /admin/clients?status=approved&limit=50&offset=0 HTTP/1.1
Host: mcp-server.example.com
Authorization: Bearer <admin-token>
```

**Query Parameters:**
- `status` - Filter by status (pending, approved, rejected, revoked)
- `limit` - Page size (default: 50, max: 100)
- `offset` - Page offset (default: 0)
- `sort` - Sort field (created_at, last_used_at, client_name)
- `order` - Sort order (asc, desc)

**Response (200 OK):**
```json
{
  "clients": [
    {
      "client_id": "7a42b3f1-e9c5-4d8a-b2f6-3e1c9a8d7b4f",
      "client_name": "My MCP Client",
      "client_type": "public",
      "status": "approved",
      "scopes": ["mcp:read", "mcp:execute"],
      "created_at": 1700000000,
      "last_used_at": 1700005000
    },
    {
      "client_id": "9f8e7d6c-5b4a-3210-fedc-ba9876543210",
      "client_name": "Backend Service",
      "client_type": "confidential",
      "status": "approved",
      "scopes": ["mcp:read", "mcp:execute", "mcp:admin"],
      "created_at": 1700001000,
      "last_used_at": 1700010000
    }
  ],
  "total": 2,
  "limit": 50,
  "offset": 0
}
```

---

### 6. Admin: Approve/Reject Client (Extension)

**Endpoint:** `POST /admin/clients/:client_id/approve` or `/admin/clients/:client_id/reject`
**Purpose:** Approve or reject pending client
**Authentication:** Bearer token with `mcp:admin` scope (required)

**Request (Approve):**
```http
POST /admin/clients/7a42b3f1-e9c5-4d8a-b2f6-3e1c9a8d7b4f/approve HTTP/1.1
Host: mcp-server.example.com
Authorization: Bearer <admin-token>
Content-Type: application/json

{
  "approved_scopes": ["mcp:read", "mcp:execute"]  // Can limit requested scopes
}
```

**Request (Reject):**
```http
POST /admin/clients/7a42b3f1-e9c5-4d8a-b2f6-3e1c9a8d7b4f/reject HTTP/1.1
Host: mcp-server.example.com
Authorization: Bearer <admin-token>
Content-Type: application/json

{
  "reason": "Suspicious activity detected"
}
```

**Response (200 OK):**
```json
{
  "client_id": "7a42b3f1-e9c5-4d8a-b2f6-3e1c9a8d7b4f",
  "status": "approved",  // or "rejected"
  "approved_by": "admin@example.com",
  "approved_at": 1700005000
}
```

---

### 7. Admin: Revoke Client (Extension)

**Endpoint:** `POST /admin/clients/:client_id/revoke`
**Purpose:** Revoke active client (emergency kill switch)
**Authentication:** Bearer token with `mcp:admin` scope (required)

**Request:**
```http
POST /admin/clients/7a42b3f1-e9c5-4d8a-b2f6-3e1c9a8d7b4f/revoke HTTP/1.1
Host: mcp-server.example.com
Authorization: Bearer <admin-token>
Content-Type: application/json

{
  "reason": "Security breach - compromised credentials"
}
```

**Response (200 OK):**
```json
{
  "client_id": "7a42b3f1-e9c5-4d8a-b2f6-3e1c9a8d7b4f",
  "status": "revoked",
  "revoked_at": 1700010000,
  "revoked_reason": "Security breach - compromised credentials"
}
```

**Effect:**
- Client immediately cannot obtain new access tokens
- Existing access tokens remain valid until expiration (implement token revocation separately)

---

### 8. Update OAuth Metadata Endpoint

**Endpoint:** `GET /.well-known/oauth-authorization-server`
**Changes:** Add `registration_endpoint` to metadata

**Current Response:**
```json
{
  "issuer": "https://auth0-domain.auth0.com/",
  "authorization_endpoint": "https://auth0-domain.auth0.com/authorize?audience=...",
  "token_endpoint": "https://auth0-domain.auth0.com/oauth/token",
  "jwks_uri": "https://auth0-domain.auth0.com/.well-known/jwks.json",
  "response_types_supported": ["code"],
  "grant_types_supported": ["authorization_code"],
  "code_challenge_methods_supported": ["S256"],
  "token_endpoint_auth_methods_supported": ["none"],
  "scopes_supported": ["mcp:read", "mcp:execute", "mcp:admin"]
}
```

**New Response (with DCR):**
```json
{
  "issuer": "https://auth0-domain.auth0.com/",
  "authorization_endpoint": "https://auth0-domain.auth0.com/authorize?audience=...",
  "token_endpoint": "https://auth0-domain.auth0.com/oauth/token",
  "jwks_uri": "https://auth0-domain.auth0.com/.well-known/jwks.json",
  "registration_endpoint": "https://mcp-server.example.com/register",  // NEW
  "response_types_supported": ["code"],
  "grant_types_supported": ["authorization_code", "client_credentials"],  // Added client_credentials
  "code_challenge_methods_supported": ["S256"],
  "token_endpoint_auth_methods_supported": ["none", "client_secret_post"],  // Added client_secret_post
  "scopes_supported": ["mcp:read", "mcp:execute", "mcp:admin"]
}
```

---

## Implementation Phases

### Phase 1: Database & Repository Layer (8 hours)

**Objective:** Set up database schema and data access layer

**Tasks:**
- [ ] **Task 1.1:** Create database migration for `OAuthClients` table (1 hour)
  - File: `Knowledge.Data/Migrations/AddOAuthClientsTables.sql`
  - Create table with all columns
  - Add indexes for performance
  - Add constraints and foreign keys

- [ ] **Task 1.2:** Create database migration for `ClientScopes` table (0.5 hour)
  - File: `Knowledge.Data/Migrations/AddOAuthClientsTables.sql`
  - Create junction table
  - Add indexes

- [ ] **Task 1.3:** Create database migration for `ClientAuditLog` table (0.5 hour)
  - File: `Knowledge.Data/Migrations/AddOAuthClientsTables.sql`
  - Create audit log table
  - Add indexes

- [ ] **Task 1.4:** Create `OAuthClient` entity model (1 hour)
  - File: `Knowledge.Data/Entities/OAuthClient.cs`
  - Map to database columns
  - Add data annotations
  - Add JSON serialization for arrays (RedirectUris, GrantTypes, etc.)

- [ ] **Task 1.5:** Create `ClientScope` entity model (0.5 hour)
  - File: `Knowledge.Data/Entities/ClientScope.cs`

- [ ] **Task 1.6:** Create `ClientAuditLogEntry` entity model (0.5 hour)
  - File: `Knowledge.Data/Entities/ClientAuditLogEntry.cs`

- [ ] **Task 1.7:** Create `IOAuthClientRepository` interface (1 hour)
  - File: `Knowledge.Data/Repositories/IOAuthClientRepository.cs`
  - Methods:
    - `Task<OAuthClient> CreateAsync(OAuthClient client)`
    - `Task<OAuthClient?> GetByIdAsync(string clientId)`
    - `Task<OAuthClient?> GetByRegistrationTokenAsync(string token)`
    - `Task<bool> UpdateAsync(OAuthClient client)`
    - `Task<bool> DeleteAsync(string clientId)`
    - `Task<IEnumerable<OAuthClient>> GetAllAsync(string? status, int limit, int offset)`
    - `Task<IEnumerable<string>> GetScopesAsync(string clientId)`
    - `Task AddAuditLogAsync(ClientAuditLogEntry entry)`

- [ ] **Task 1.8:** Implement `SqliteOAuthClientRepository` (3 hours)
  - File: `Knowledge.Data/Repositories/SqliteOAuthClientRepository.cs`
  - Implement all interface methods
  - Use Dapper for data access
  - Handle JSON serialization for array columns
  - Use transactions for atomic operations
  - Add error handling and logging

- [ ] **Task 1.9:** Register repository in DI container (0.5 hour)
  - File: `Knowledge.Data/Extensions/ServiceCollectionExtensions.cs`
  - Add `services.AddScoped<IOAuthClientRepository, SqliteOAuthClientRepository>()`

**Deliverables:**
- ✅ Database schema created and indexed
- ✅ Entity models defined
- ✅ Repository interface and implementation
- ✅ DI registration complete

---

### Phase 2: Business Logic Layer (10 hours)

**Objective:** Implement client registration and management logic

**Tasks:**
- [ ] **Task 2.1:** Create `ClientRegistrationRequest` DTO (1 hour)
  - File: `Knowledge.Mcp/Models/ClientRegistrationRequest.cs`
  - Properties match RFC 7591 specification
  - Data annotations for validation

- [ ] **Task 2.2:** Create `ClientRegistrationResponse` DTO (1 hour)
  - File: `Knowledge.Mcp/Models/ClientRegistrationResponse.cs`
  - Include all required RFC 7591 fields

- [ ] **Task 2.3:** Create `IOAuthClientService` interface (1 hour)
  - File: `Knowledge.Mcp/Services/IOAuthClientService.cs`
  - Methods:
    - `Task<ClientRegistrationResponse> RegisterClientAsync(ClientRegistrationRequest request, string? createdByIp)`
    - `Task<ClientRegistrationResponse> GetClientAsync(string clientId, string registrationToken)`
    - `Task<ClientRegistrationResponse> UpdateClientAsync(string clientId, string registrationToken, ClientRegistrationRequest request)`
    - `Task DeleteClientAsync(string clientId, string registrationToken)`
    - `Task<bool> ValidateClientCredentialsAsync(string clientId, string clientSecret)`
    - `Task<IEnumerable<string>> GetClientScopesAsync(string clientId)`

- [ ] **Task 2.4:** Implement `OAuthClientService` (5 hours)
  - File: `Knowledge.Mcp/Services/OAuthClientService.cs`
  - **RegisterClientAsync:**
    - Validate request (redirect URIs, grant types, scopes)
    - Generate `client_id` (UUID v4)
    - Generate `client_secret` for confidential clients (32-byte secure random)
    - Hash client secret with PBKDF2 (100,000 iterations, SHA-256)
    - Generate `registration_access_token` (JWT signed with server key)
    - Store in database via repository
    - Return registration response
  - **GetClientAsync:**
    - Validate registration access token
    - Retrieve client from repository
    - Return client metadata
  - **UpdateClientAsync:**
    - Validate registration access token
    - Validate update request
    - Prevent updates to immutable fields
    - Update in database
    - Return updated metadata
  - **DeleteClientAsync:**
    - Validate registration access token
    - Soft delete (mark as revoked) or hard delete
    - Audit log entry
  - **ValidateClientCredentialsAsync:**
    - Lookup client by ID
    - Verify hashed secret matches
    - Check client status (approved, not revoked)
  - **GetClientScopesAsync:**
    - Retrieve allowed scopes from database

- [ ] **Task 2.5:** Create client secret hashing utility (1 hour)
  - File: `Knowledge.Mcp/Security/ClientSecretHasher.cs`
  - Methods:
    - `string HashSecret(string secret, out string salt)`
    - `bool VerifySecret(string secret, string hash, string salt)`
  - Use PBKDF2 with 100,000 iterations

- [ ] **Task 2.6:** Create registration access token generator (1 hour)
  - File: `Knowledge.Mcp/Security/RegistrationTokenGenerator.cs`
  - Methods:
    - `string GenerateToken(string clientId, DateTime expiresAt)`
    - `ClaimsPrincipal ValidateToken(string token)`
  - Use HS256 JWT signing
  - Include `client_id` claim
  - Set expiration (30 days default)

**Deliverables:**
- ✅ DTOs for request/response
- ✅ Service interface defined
- ✅ Business logic implemented
- ✅ Security utilities (hashing, JWT)

---

### Phase 3: API Endpoints (6 hours)

**Objective:** Expose DCR endpoints via HTTP

**Tasks:**
- [ ] **Task 3.1:** Create `ClientRegistrationEndpoints.cs` (3 hours)
  - File: `Knowledge.Mcp/Endpoints/ClientRegistrationEndpoints.cs`
  - Endpoint: `POST /register`
    - Parse request body
    - Validate model state
    - Call `IOAuthClientService.RegisterClientAsync`
    - Return 201 Created with client credentials
    - Handle errors (400, 429, 500)
  - Endpoint: `GET /register/:client_id`
    - Extract registration token from Authorization header
    - Call `IOAuthClientService.GetClientAsync`
    - Return 200 OK with client metadata
    - Handle errors (401, 404)
  - Endpoint: `PUT /register/:client_id`
    - Parse request body and registration token
    - Call `IOAuthClientService.UpdateClientAsync`
    - Return 200 OK with updated metadata
    - Handle errors (400, 401, 403, 404)
  - Endpoint: `DELETE /register/:client_id`
    - Extract registration token
    - Call `IOAuthClientService.DeleteClientAsync`
    - Return 204 No Content
    - Handle errors (401, 404)

- [ ] **Task 3.2:** Create `ClientAdminEndpoints.cs` (2 hours)
  - File: `Knowledge.Mcp/Endpoints/ClientAdminEndpoints.cs`
  - Endpoint: `GET /admin/clients`
    - Require `mcp:admin` scope
    - Parse query parameters (status, limit, offset, sort)
    - Call repository `GetAllAsync`
    - Return paginated list
  - Endpoint: `POST /admin/clients/:client_id/approve`
    - Require `mcp:admin` scope
    - Update client status to "approved"
    - Audit log entry
    - Return updated client
  - Endpoint: `POST /admin/clients/:client_id/reject`
    - Require `mcp:admin` scope
    - Update client status to "rejected"
    - Audit log entry
  - Endpoint: `POST /admin/clients/:client_id/revoke`
    - Require `mcp:admin` scope
    - Update client status to "revoked"
    - Audit log entry

- [ ] **Task 3.3:** Register endpoints in Program.cs (0.5 hour)
  - File: `Knowledge.Mcp/Program.cs`
  - Add `app.MapClientRegistrationEndpoints()`
  - Add `app.MapClientAdminEndpoints()`
  - Configure before/after existing MCP endpoints

- [ ] **Task 3.4:** Update OAuth metadata endpoint (0.5 hour)
  - File: `Knowledge.Mcp/Endpoints/WellKnownEndpoints.cs`
  - Add `registration_endpoint` to metadata response
  - Add `client_credentials` to `grant_types_supported`
  - Add `client_secret_post` to `token_endpoint_auth_methods_supported`

**Deliverables:**
- ✅ Registration endpoints implemented
- ✅ Admin endpoints implemented
- ✅ Endpoints registered in app pipeline
- ✅ OAuth metadata updated

---

### Phase 4: Rate Limiting & Validation (4 hours)

**Objective:** Add security controls to prevent abuse

**Tasks:**
- [ ] **Task 4.1:** Create rate limiting middleware (2 hours)
  - File: `Knowledge.Mcp/Middleware/RegistrationRateLimitMiddleware.cs`
  - Use in-memory cache (IMemoryCache)
  - Track requests per IP address
  - Limit: 10 requests/hour per IP for `/register`
  - Return 429 Too Many Requests with `Retry-After` header
  - Log rate limit violations

- [ ] **Task 4.2:** Create request validation service (1.5 hours)
  - File: `Knowledge.Mcp/Validation/ClientRegistrationValidator.cs`
  - Validate redirect URIs:
    - Must be HTTPS (except localhost for development)
    - Must be valid absolute URIs
    - Maximum 10 redirect URIs per client
  - Validate grant types:
    - Must be subset of `["authorization_code", "client_credentials"]`
    - At least one grant type required
  - Validate scopes:
    - Must be subset of `["mcp:read", "mcp:execute", "mcp:admin"]`
    - Default to `"mcp:read"` if not specified
  - Validate client name:
    - 1-100 characters
    - No special characters (alphanumeric, spaces, hyphens, underscores only)

- [ ] **Task 4.3:** Add validation to service layer (0.5 hour)
  - File: `Knowledge.Mcp/Services/OAuthClientService.cs`
  - Call validator before persisting
  - Throw validation exceptions with descriptive messages

**Deliverables:**
- ✅ Rate limiting middleware
- ✅ Request validation service
- ✅ Validation integrated into service layer

---

### Phase 5: Client Authentication Integration (6 hours)

**Objective:** Integrate registered clients with Auth0 token endpoint

**Tasks:**
- [ ] **Task 5.1:** Create Auth0 client creation service (3 hours)
  - File: `Knowledge.Mcp/Services/Auth0ClientProvisioningService.cs`
  - Use Auth0 Management API
  - When client registers via `/register`:
    - Create corresponding M2M application in Auth0
    - Set client ID to same UUID
    - Set client secret (if confidential)
    - Grant requested scopes
    - Store Auth0 client ID in OAuthClients table
  - When client is deleted:
    - Delete Auth0 application
  - Requires Auth0 Management API token

- [ ] **Task 5.2:** Configure Auth0 Management API access (1 hour)
  - Create M2M application in Auth0 for MCP server
  - Grant `create:clients`, `read:clients`, `update:clients`, `delete:clients` scopes
  - Store Auth0 domain and Management API token in appsettings.json
  - Add configuration class `Auth0ManagementSettings`

- [ ] **Task 5.3:** Update OAuthClientService to call provisioning (1 hour)
  - File: `Knowledge.Mcp/Services/OAuthClientService.cs`
  - In `RegisterClientAsync`:
    - After creating local database record
    - Call `Auth0ClientProvisioningService.CreateClientAsync`
    - If Auth0 call fails, rollback local creation
  - In `DeleteClientAsync`:
    - After marking local client as deleted
    - Call `Auth0ClientProvisioningService.DeleteClientAsync`

- [ ] **Task 5.4:** Add configuration to appsettings.json (0.5 hour)
  - File: `Knowledge.Mcp/appsettings.json`
  - Add section:
    ```json
    "Auth0Management": {
      "Domain": "your-tenant.auth0.com",
      "ClientId": "management-api-client-id",
      "ClientSecret": "management-api-client-secret",
      "Audience": "https://your-tenant.auth0.com/api/v2/"
    }
    ```

- [ ] **Task 5.5:** Handle Auth0 sync failures gracefully (0.5 hour)
  - Add retry logic (3 attempts with exponential backoff)
  - On permanent failure, mark client as "pending_sync"
  - Add background job to retry sync (optional)

**Deliverables:**
- ✅ Auth0 client provisioning service
- ✅ Management API configured
- ✅ Registration flow creates Auth0 clients
- ✅ Deletion flow removes Auth0 clients
- ✅ Error handling and retry logic

**Alternative (Simpler):**
If Auth0 integration is too complex:
- Store client credentials locally only
- Implement custom token endpoint: `POST /oauth/token`
- Issue JWT access tokens directly from MCP server
- Skip Auth0 provisioning entirely
- **Trade-off:** More implementation work, but complete control

---

### Phase 6: Testing (6 hours)

**Objective:** Comprehensive test coverage

**Tasks:**
- [ ] **Task 6.1:** Unit tests for repository (2 hours)
  - File: `Knowledge.Mcp.Tests/Repositories/OAuthClientRepositoryTests.cs`
  - Test `CreateAsync` - creates client with all fields
  - Test `GetByIdAsync` - retrieves existing client
  - Test `GetByIdAsync` - returns null for non-existent
  - Test `UpdateAsync` - updates client metadata
  - Test `DeleteAsync` - soft deletes client
  - Test `GetScopesAsync` - returns correct scopes
  - Test concurrent access (thread safety)
  - Use in-memory SQLite database for tests

- [ ] **Task 6.2:** Unit tests for service layer (2 hours)
  - File: `Knowledge.Mcp.Tests/Services/OAuthClientServiceTests.cs`
  - Test `RegisterClientAsync` - public client (no secret)
  - Test `RegisterClientAsync` - confidential client (with secret)
  - Test `RegisterClientAsync` - validation failures
  - Test `GetClientAsync` - valid token
  - Test `GetClientAsync` - invalid token (401)
  - Test `UpdateClientAsync` - allowed fields
  - Test `UpdateClientAsync` - immutable fields (403)
  - Test `DeleteClientAsync` - marks as revoked
  - Test `ValidateClientCredentialsAsync` - correct secret
  - Test `ValidateClientCredentialsAsync` - wrong secret
  - Use mocked repository

- [ ] **Task 6.3:** Integration tests with Auth0 (1.5 hours)
  - File: `Knowledge.Mcp.Tests/Integration/ClientRegistrationIntegrationTests.cs`
  - Test full flow:
    1. Register client via `/register`
    2. Verify client created in Auth0
    3. Get access token from Auth0 using client credentials
    4. Use access token with MCP server
    5. Delete client via `/register/:client_id`
    6. Verify client deleted in Auth0
  - Requires Auth0 test tenant

- [ ] **Task 6.4:** API endpoint tests (0.5 hour)
  - File: `Knowledge.Mcp.Tests/Endpoints/ClientRegistrationEndpointsTests.cs`
  - Test `POST /register` - 201 Created
  - Test `POST /register` - 400 Bad Request (validation)
  - Test `POST /register` - 429 Too Many Requests (rate limit)
  - Test `GET /register/:client_id` - 200 OK
  - Test `GET /register/:client_id` - 401 Unauthorized
  - Test `PUT /register/:client_id` - 200 OK
  - Test `DELETE /register/:client_id` - 204 No Content
  - Use WebApplicationFactory for integration tests

**Deliverables:**
- ✅ Unit tests for repository (>80% coverage)
- ✅ Unit tests for service layer (>80% coverage)
- ✅ Integration tests with Auth0
- ✅ API endpoint tests
- ✅ All tests passing in CI/CD

---

### Phase 7: Documentation (2 hours)

**Objective:** Complete developer and user documentation

**Tasks:**
- [ ] **Task 7.1:** Update CLAUDE.md (0.5 hour)
  - File: `CLAUDE.md`
  - Update Milestone #23 status to "COMPLETED"
  - Add DCR subsection with implementation details
  - Document endpoints and flows

- [ ] **Task 7.2:** Create DCR developer guide (1 hour)
  - File: `documentation/DCR_DEVELOPER_GUIDE.md`
  - How to register a client
  - How to manage client metadata
  - How to authenticate with registered client
  - Code examples (curl, C#, JavaScript)
  - Troubleshooting common issues

- [ ] **Task 7.3:** Update MCP documentation (0.5 hour)
  - File: `documentation/MCP_CLIENT_IMPLEMENTATION_PLAN.md`
  - Add DCR section
  - Explain how MCP clients can use DCR
  - Update client configuration examples

**Deliverables:**
- ✅ CLAUDE.md updated
- ✅ Developer guide created
- ✅ MCP documentation updated

---

## Security Considerations

### Threat Model

**1. Malicious Client Registration**
- **Threat:** Attacker registers thousands of clients to exhaust resources
- **Mitigation:**
  - Rate limiting (10 requests/hour per IP)
  - CAPTCHA on registration form (optional)
  - Admin approval workflow for production
  - Monitor and alert on unusual registration patterns

**2. Client Secret Theft**
- **Threat:** Attacker steals client secret from client application
- **Mitigation:**
  - Client secrets hashed in database (PBKDF2, 100,000 iterations)
  - Secrets only transmitted once during registration (never retrievable)
  - Recommend public clients with PKCE (no secret)
  - Short-lived access tokens (5 min)
  - Implement token revocation

**3. Registration Token Theft**
- **Threat:** Attacker steals registration access token to modify client
- **Mitigation:**
  - Registration tokens are JWT with expiration (30 days)
  - Tokens signed with server secret key
  - Tokens revoked when client is deleted
  - HTTPS required for all registration operations

**4. Redirect URI Manipulation**
- **Threat:** Attacker modifies redirect URI to steal authorization codes
- **Mitigation:**
  - Strict redirect URI validation (exact match, HTTPS required)
  - Localhost only allowed for development
  - Maximum 10 redirect URIs per client
  - Immutable after registration (unless updated via registration token)

**5. Scope Elevation**
- **Threat:** Client requests more scopes than needed
- **Mitigation:**
  - Scopes validated against whitelist (`mcp:read`, `mcp:execute`, `mcp:admin`)
  - Admin approval required for sensitive scopes (optional)
  - Scope changes audited in ClientAuditLog
  - Principle of least privilege enforced

**6. Database Injection**
- **Threat:** SQL injection via client metadata
- **Mitigation:**
  - Parameterized queries (Dapper)
  - Input validation on all fields
  - Maximum length limits enforced
  - Special character escaping

**7. Denial of Service**
- **Threat:** Attacker floods registration endpoint
- **Mitigation:**
  - Rate limiting (10 requests/hour per IP)
  - Request size limits (max 10KB)
  - Database connection pooling
  - Graceful degradation under load

### Security Best Practices

**Client Secret Handling:**
```csharp
// ✅ GOOD: Hash secret before storing
var secret = GenerateSecureSecret(32); // 32 bytes = 256 bits
var (hash, salt) = ClientSecretHasher.HashSecret(secret);
client.ClientSecretHash = hash;
client.ClientSecretSalt = salt;

// ❌ BAD: Store plaintext secret
client.ClientSecret = secret; // NEVER DO THIS
```

**Registration Token Generation:**
```csharp
// ✅ GOOD: JWT with expiration and claims
var token = new JwtSecurityToken(
    issuer: "mcp-server",
    claims: new[] {
        new Claim("client_id", clientId),
        new Claim("purpose", "registration_management")
    },
    expires: DateTime.UtcNow.AddDays(30),
    signingCredentials: serverCredentials
);

// ❌ BAD: Random string with no expiration
var token = Guid.NewGuid().ToString(); // No expiration, no verification
```

**Redirect URI Validation:**
```csharp
// ✅ GOOD: Strict validation
public bool IsValidRedirectUri(string uri)
{
    if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsedUri))
        return false;

    // Must be HTTPS (except localhost)
    if (parsedUri.Scheme != "https" && !IsLocalhost(parsedUri))
        return false;

    // No wildcards or path traversal
    if (uri.Contains("*") || uri.Contains(".."))
        return false;

    return true;
}

// ❌ BAD: No validation
public bool IsValidRedirectUri(string uri)
{
    return !string.IsNullOrEmpty(uri); // Accepts anything
}
```

---

## Testing Strategy

### Test Pyramid

```
           ┌─────────────┐
           │  E2E Tests  │  10% - Full OAuth flow with Auth0
           └─────────────┘
          ┌───────────────┐
          │ Integration   │ 30% - API endpoints, DB access
          └───────────────┘
        ┌───────────────────┐
        │   Unit Tests       │ 60% - Business logic, validation
        └───────────────────┘
```

### Test Coverage Goals

| Component | Target Coverage | Priority |
|-----------|----------------|----------|
| Repository Layer | 90% | High |
| Service Layer | 85% | High |
| Validation | 95% | Critical |
| Endpoints | 80% | Medium |
| Security Utilities | 95% | Critical |

### Key Test Scenarios

**Unit Tests (60%):**
- Client secret hashing and verification
- Registration token generation and validation
- Redirect URI validation
- Scope validation
- Client metadata updates (immutable fields)
- Repository CRUD operations
- JSON serialization/deserialization

**Integration Tests (30%):**
- Full registration flow (POST /register → client created)
- Client retrieval (GET /register/:client_id)
- Client update (PUT /register/:client_id)
- Client deletion (DELETE /register/:client_id)
- Admin client listing (GET /admin/clients)
- Admin approval/rejection
- Database transactions (rollback on error)
- Rate limiting enforcement

**End-to-End Tests (10%):**
- Register client → Get Auth0 token → Use MCP server
- Register public client → PKCE flow → Use MCP server
- Update client → Verify Auth0 synced
- Delete client → Verify Auth0 deleted
- Revoke client → Verify tokens no longer work

### Test Data

**Test Clients:**
```csharp
// Public client (PKCE)
var publicClient = new ClientRegistrationRequest
{
    ClientName = "Test Public Client",
    ClientType = "public",
    RedirectUris = new[] { "http://localhost:3000/callback" },
    GrantTypes = new[] { "authorization_code" },
    ResponseTypes = new[] { "code" },
    TokenEndpointAuthMethod = "none",
    Scope = "mcp:read mcp:execute"
};

// Confidential client (M2M)
var confidentialClient = new ClientRegistrationRequest
{
    ClientName = "Test Backend Service",
    ClientType = "confidential",
    RedirectUris = Array.Empty<string>(),
    GrantTypes = new[] { "client_credentials" },
    ResponseTypes = Array.Empty<string>(),
    TokenEndpointAuthMethod = "client_secret_post",
    Scope = "mcp:read mcp:execute mcp:admin"
};
```

**Mock Auth0 Responses:**
```json
// Token endpoint success
{
  "access_token": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
  "token_type": "Bearer",
  "expires_in": 86400,
  "scope": "mcp:read mcp:execute"
}

// Management API client creation success
{
  "client_id": "7a42b3f1-e9c5-4d8a-b2f6-3e1c9a8d7b4f",
  "client_secret": "K7gNU3sdo+OL0wNhqoVWhr3g6s1xYv72ol/pe/Unols=",
  "name": "Test Client"
}
```

---

## Deployment Considerations

### Configuration

**appsettings.json additions:**
```json
{
  "DynamicClientRegistration": {
    "Enabled": true,
    "RequireApproval": false,  // true for production
    "AutoApproveScopes": ["mcp:read"],  // Auto-approve read-only
    "MaxClientsPerIp": 10,  // Max 10 clients per IP address
    "RateLimit": {
      "RequestsPerHour": 10,
      "Enabled": true
    }
  },

  "Auth0Management": {
    "Domain": "your-tenant.auth0.com",
    "ClientId": "management-api-client-id",
    "ClientSecret": "ENCRYPTED_SECRET",
    "Audience": "https://your-tenant.auth0.com/api/v2/",
    "SyncToAuth0": true  // false to skip Auth0 integration
  },

  "RegistrationTokens": {
    "SigningKey": "GENERATED_SECRET_KEY",  // Generate with: openssl rand -base64 32
    "Issuer": "https://mcp-server.example.com",
    "ExpirationDays": 30
  }
}
```

### Database Migration

**Migration script:**
```bash
#!/bin/bash
# Apply DCR database migrations

DB_PATH="/opt/knowledge-api/data/knowledge.db"

# Backup database
cp "$DB_PATH" "$DB_PATH.backup.$(date +%s)"

# Apply migrations
sqlite3 "$DB_PATH" < Knowledge.Data/Migrations/AddOAuthClientsTables.sql

# Verify
sqlite3 "$DB_PATH" "SELECT name FROM sqlite_master WHERE type='table' AND name LIKE '%OAuth%';"

echo "Migration complete. Verify tables: OAuthClients, ClientScopes, ClientAuditLog"
```

### GitHub Actions CI/CD

**Update `.github/workflows/deploy-self.yml`:**
```yaml
# After existing deployment steps...

- name: Run DCR Database Migrations
  run: |
    echo "Running DCR database migrations..."
    cp /opt/knowledge-api/data/knowledge.db /opt/knowledge-api/data/knowledge.db.backup.$(date +%s)
    sqlite3 /opt/knowledge-api/data/knowledge.db < Knowledge.Data/Migrations/AddOAuthClientsTables.sql

    # Verify migration
    TABLE_COUNT=$(sqlite3 /opt/knowledge-api/data/knowledge.db "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name IN ('OAuthClients', 'ClientScopes', 'ClientAuditLog');")

    if [ "$TABLE_COUNT" -ne "3" ]; then
      echo "❌ Migration failed - expected 3 tables, found $TABLE_COUNT"
      exit 1
    fi

    echo "✅ DCR database migration successful"
```

### Rollback Plan

**If DCR causes issues:**
```bash
# 1. Disable DCR in appsettings.json
"DynamicClientRegistration": {
  "Enabled": false
}

# 2. Restart MCP server
sudo systemctl restart knowledge-mcp.service

# 3. (Optional) Rollback database
DB_BACKUP=$(ls -t /opt/knowledge-api/data/knowledge.db.backup.* | head -1)
cp "$DB_BACKUP" /opt/knowledge-api/data/knowledge.db

# 4. (Optional) Drop DCR tables
sqlite3 /opt/knowledge-api/data/knowledge.db <<EOF
DROP TABLE IF EXISTS ClientAuditLog;
DROP TABLE IF EXISTS ClientScopes;
DROP TABLE IF EXISTS OAuthClients;
EOF
```

### Monitoring

**Metrics to track:**
- Client registrations per day/hour
- Registration errors (validation, rate limit)
- Active clients count
- Token requests per client
- Failed authentication attempts
- Auth0 sync failures
- Database size growth

**Alerts:**
- Registration rate spike (> 100/hour)
- High validation failure rate (> 50%)
- Auth0 sync failures (> 5 consecutive)
- Database errors
- Disk space low (> 90% used)

---

## Future Enhancements

### Phase 8: Advanced Features (Not in Initial Scope)

**1. Client Logo and Branding**
- Upload client logo during registration
- Store in file system or S3
- Display in admin dashboard

**2. Client Statistics Dashboard**
- Tokens issued per client
- API usage per client
- Most active clients
- Error rates per client

**3. Automated Client Rotation**
- Auto-rotate client secrets every 90 days
- Email notification before expiration
- Grace period for old secret

**4. Client Webhooks**
- Notify client when status changes
- Notify client when registration token expires
- Configurable webhook URLs

**5. Client Categories/Tags**
- Group clients by purpose (development, production, testing)
- Filter admin dashboard by tags
- Apply different rate limits per category

**6. Multi-Factor Approval**
- Require multiple admin approvals for sensitive scopes
- Approval workflow with email notifications
- Approval history and audit trail

**7. Client Health Monitoring**
- Track last used timestamp
- Auto-revoke inactive clients (> 90 days)
- Email notification before auto-revocation

**8. OAuth Token Introspection (RFC 7662)**
- Endpoint: `POST /oauth/introspect`
- Check if token is valid and active
- Return token metadata (scopes, expiration, client)

**9. OAuth Token Revocation (RFC 7009)**
- Endpoint: `POST /oauth/revoke`
- Revoke specific access token
- Maintain revocation list (Redis)

**10. Client Certificate Authentication**
- mTLS client authentication
- Upload client certificate during registration
- Validate certificate on token requests

---

## Effort Summary

| Phase | Description | Hours | Difficulty |
|-------|-------------|-------|------------|
| Phase 1 | Database & Repository | 8 | Medium |
| Phase 2 | Business Logic | 10 | High |
| Phase 3 | API Endpoints | 6 | Medium |
| Phase 4 | Rate Limiting & Validation | 4 | Medium |
| Phase 5 | Auth0 Integration | 6 | High |
| Phase 6 | Testing | 6 | Medium |
| Phase 7 | Documentation | 2 | Low |
| **Total** | | **42 hours** | |

**Assumptions:**
- Developer familiar with .NET, ASP.NET Core, SQLite, Auth0
- Existing OAuth 2.1 infrastructure in place (Milestone #23)
- Auth0 test tenant available
- No major blockers or scope creep

**Breakdown by Skill:**
- Backend Development: 30 hours (71%)
- Testing: 6 hours (14%)
- DevOps/Deployment: 4 hours (10%)
- Documentation: 2 hours (5%)

---

## Success Criteria

### Functional Success

- [ ] Clients can register via `POST /register` without errors
- [ ] Registered clients receive valid credentials (`client_id`, `client_secret`)
- [ ] Clients can authenticate with Auth0 using registered credentials
- [ ] Clients can access MCP server with valid access token
- [ ] Clients can retrieve metadata via `GET /register/:client_id`
- [ ] Clients can update metadata via `PUT /register/:client_id`
- [ ] Clients can delete registration via `DELETE /register/:client_id`
- [ ] Admin can list all clients via `GET /admin/clients`
- [ ] Admin can approve/reject pending clients
- [ ] Admin can revoke active clients

### Non-Functional Success

- [ ] Registration completes in < 500ms (p95)
- [ ] Client lookup completes in < 50ms (p95)
- [ ] Rate limiting prevents abuse (10 requests/hour enforced)
- [ ] Client secrets never stored in plaintext (PBKDF2 hashed)
- [ ] Database transactions ensure atomicity
- [ ] All endpoints return proper HTTP status codes
- [ ] All errors logged with context
- [ ] Test coverage > 80% across all components

### Compliance Success

- [ ] RFC 7591 (Dynamic Client Registration) compliance verified
- [ ] RFC 7592 (Client Management) compliance verified
- [ ] OAuth 2.1 best practices followed
- [ ] Security audit passed (no critical vulnerabilities)
- [ ] OWASP Top 10 mitigations in place

---

## Conclusion

Dynamic Client Registration (DCR) is a powerful feature that enables self-service client onboarding for your MCP server. However, it adds significant complexity (~42 hours of implementation).

**Recommendation:**
- **Start with Phases 1-4** (28 hours) to get basic DCR working
- **Skip Auth0 integration (Phase 5)** initially - implement local token issuance instead
- **Add Auth0 sync later** if needed

**Alternative Quick Win:**
- Implement a simple admin API to create clients programmatically
- Skip full RFC 7591/7592 compliance
- Reduce implementation to 10-15 hours

The choice depends on your scalability needs and whether you expect many third-party integrations.

---

**Last Updated:** 2025-11-17
**Author:** Claude (AI Assistant)
**Review Status:** Pending human review

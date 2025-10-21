# MCP HTTP Transport Implementation - Progress Report

**Date:** 2025-10-19
**Status:** ⚠️ PARTIAL - Infrastructure ready, MapMcp() endpoint registration issue

---

## Summary

Successfully converted Knowledge.Mcp from console-only (STDIO) to a dual-mode MCP server supporting both STDIO and HTTP SSE transports. The infrastructure is in place and builds successfully, but the `MapMcp()` method from `ModelContextProtocol.AspNetCore` v0.4.0-preview.2 is not registering endpoints as expected.

---

## Completed Tasks ✅

### 1. Project Conversion
**File:** `Knowledge.Mcp/Knowledge.Mcp.csproj`
Changed from `Microsoft.NET.Sdk` (console) to `Microsoft.NET.Sdk.Web` (web application)

```xml
<!-- Before: -->
<Project Sdk="Microsoft.NET.Sdk">

<!-- After: -->
<Project Sdk="Microsoft.NET.Sdk.Web">
```

### 2. Package Installation
Added `ModelContextProtocol.AspNetCore` package:
```xml
<PackageReference Include="ModelContextProtocol.AspNetCore" Version="0.4.0-preview.2" />
```

Fixed `appsettings.json` duplicate content item:
```xml
<!-- Before: (caused build error with Web SDK) -->
<None Remove="appsettings.json" />
<Content Include="appsettings.json">

<!-- After: (works with Web SDK auto-inclusion) -->
<Content Update="appsettings.json">
```

### 3. Dual Transport Support
**File:** `Knowledge.Mcp/Program.cs` (Lines 39-65)

Added command-line flag to switch between STDIO and HTTP modes:

```csharp
// Check for HTTP transport mode
bool useHttp = args.Contains("--http") || args.Contains("--http-transport");

if (useHttp)
{
    // Run as HTTP SSE server (for web clients)
    Console.WriteLine("Starting Knowledge MCP Server in HTTP SSE mode...");
    await RunHttpServer(args);
}
else
{
    // Run as STDIO server (for Claude Desktop, default)
    Console.WriteLine("Starting Knowledge MCP Server in STDIO mode...");
    var host = CreateHostBuilder(args).Build();
    await host.RunAsync();
}
```

### 4. HTTP Server Implementation
**File:** `Knowledge.Mcp/Program.cs` (Lines 291-464)

Created `RunHttpServer()` method using WebApplication builder:

```csharp
static async Task RunHttpServer(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);

    // Configure all services (same as STDIO mode)
    // - ChatCompleteSettings
    // - Database services
    // - Qdrant vector store
    // - System health checkers
    // - Knowledge management services
    // - Embedding services (Ollama)
    // - Agent plugins
    // - MCP resources

    // ⭐ Configure MCP server with HTTP transport
    builder.Services
        .AddMcpServer()
        .WithHttpTransport()
        .WithToolsFromAssembly()
        .WithResources<Knowledge.Mcp.Resources.KnowledgeResourceMethods>();

    var app = builder.Build();

    // ⭐ Map MCP endpoints (should create /sse and /messages)
    app.MapMcp();

    await app.RunAsync();
}
```

### 5. Build Status
✅ **Build:** SUCCESS (1 warning - async method lacks await)
```bash
dotnet build Knowledge.Mcp/Knowledge.Mcp.csproj
# Knowledge.Mcp -> /home/wayne/repos/ChatComplete/Knowledge.Mcp/bin/Debug/net8.0/Knowledge.Mcp.dll
# Build succeeded.
```

### 6. Server Startup
✅ **HTTP Mode Starts Successfully:**
```bash
dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj -- --http

# Output:
# Starting Knowledge MCP Server in HTTP SSE mode...
# MCP Server configuration base path: .../Knowledge.Mcp/bin/Debug/net8.0
# MCP Server using database path: .../data/knowledge.db
# Knowledge MCP Server HTTP SSE endpoints:
#   GET  /sse       - Server-Sent Events stream
#   POST /messages  - JSON-RPC requests
#
# Server listening on: http://localhost:5000
# Now listening on: http://localhost:5000
# Application started. Press Ctrl+C to shut down.
```

---

## Current Issue ⚠️

### MapMcp() Endpoint Registration Not Working

**Problem:**
Despite calling `app.MapMcp()`, the `/messages` and `/sse` endpoints return 404:

```bash
curl -i -X POST http://localhost:5000/messages
# HTTP/1.1 404 Not Found

curl -i http://localhost:5000/sse
# HTTP/1.1 404 Not Found
```

**Test Command:**
```bash
curl -X POST http://localhost:5000/messages \
  -H 'Content-Type: application/json' \
  -H 'Accept: application/json, text/event-stream' \
  -d '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test-client","version":"1.0.0"}}}'
```

**Result:** Empty response (404)

**Server Logs:** No indication that requests are being routed to MCP handlers

---

## Architecture Changes

### Before (STDIO Only)
```
Knowledge.Mcp (Console App)
├── Uses Host.CreateDefaultBuilder()
├── .WithStdioServerTransport()
└── Communicates via stdin/stdout with Claude Desktop
```

### After (Dual Mode)
```
Knowledge.Mcp (Web + Console App)
├── Default: STDIO mode (--no flags)
│   ├── Host.CreateDefaultBuilder()
│   └── .WithStdioServerTransport()
│
└── HTTP mode (--http flag)
    ├── WebApplication.CreateBuilder()
    ├── .WithHttpTransport()
    └── app.MapMcp() → Should create /messages and /sse
```

---

## Investigation Needed

### Possible Causes of MapMcp() Failure

1. **SDK Version Issue**
   - Using preview package `0.4.0-preview.2`
   - API surface may have changed
   - MapMcp() may have bugs or require specific configuration

2. **Missing Middleware/Configuration**
   - May need additional middleware before MapMcp()
   - CORS, routing, or other ASP.NET Core configuration

3. **Endpoint Path Issue**
   - MapMcp() might not create `/messages` and `/sse` at root
   - May need to specify base path differently
   - Documentation may be outdated

4. **Protocol Version Mismatch**
   - SDK may implement different MCP protocol version
   - May not be compatible with 2024-11-05 or 2025-06-18 spec

5. **Stateful vs Stateless**
   - HTTP transport may require explicit session management
   - May need to configure `HttpServerTransportOptions`

---

## Next Steps

### Option A: Debug MapMcp() (Recommended First)
1. ✅ Check SDK source code or examples for correct usage
2. ✅ Try different MapMcp() overloads (with/without base path)
3. ✅ Add middleware (UseRouting, UseEndpoints) explicitly
4. ✅ Check if HTTP POST/GET handlers are being registered
5. ✅ Enable detailed ASP.NET Core logging

### Option B: Manual Implementation (If MapMcp() Can't Be Fixed)
Implement endpoints manually per MCP Streamable HTTP specification:

```csharp
// POST /messages - Handle JSON-RPC requests
app.MapPost("/messages", async (HttpContext context) =>
{
    // Read JSON-RPC request from body
    // Route to MCP server handlers
    // Return JSON or initiate SSE stream based on Accept header
});

// GET /sse - Server-Sent Events stream
app.MapGet("/sse", async (HttpContext context) =>
{
    context.Response.Headers.ContentType = "text/event-stream";
    // Stream server-to-client messages
});

// DELETE /messages - Session termination (optional)
app.MapDelete("/messages", async (HttpContext context) =>
{
    // Handle session termination if session ID provided
});
```

### Option C: Use Different SDK Version
1. Check if newer/older SDK versions work better
2. Try `ModelContextProtocol` main package directly with custom HTTP handling
3. Wait for stable SDK release

---

## Usage

### Run in STDIO Mode (Default - for Claude Desktop)
```bash
dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj
# or
dotnet Knowledge.Mcp/bin/Debug/net8.0/Knowledge.Mcp.dll
```

### Run in HTTP Mode (for Web Clients)
```bash
dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj -- --http
# or
dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj -- --http --urls "http://localhost:8080"
```

### Test (Once Working)
```bash
# Initialize MCP session
curl -X POST http://localhost:5000/messages \
  -H 'Content-Type: application/json' \
  -H 'Accept: application/json, text/event-stream' \
  -d '{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "initialize",
    "params": {
      "protocolVersion": "2024-11-05",
      "capabilities": {},
      "clientInfo": {
        "name": "test-client",
        "version": "1.0.0"
      }
    }
  }'

# List available tools
curl -X POST http://localhost:5000/messages \
  -H 'Content-Type: application/json' \
  -H 'Accept: application/json' \
  -d '{
    "jsonrpc": "2.0",
    "id": 2,
    "method": "tools/list"
  }'

# Open SSE stream for server messages
curl -N http://localhost:5000/sse \
  -H 'Accept: text/event-stream'
```

---

## Files Modified

### Modified Files
1. **Knowledge.Mcp/Knowledge.Mcp.csproj**
   - Changed SDK from `Microsoft.NET.Sdk` to `Microsoft.NET.Sdk.Web`
   - Added `ModelContextProtocol.AspNetCore` package
   - Fixed appsettings.json content item (Remove → Update)

2. **Knowledge.Mcp/Program.cs**
   - Added transport mode detection (--http flag)
   - Kept existing STDIO implementation (CreateHostBuilder)
   - Added new HTTP implementation (RunHttpServer)
   - Configured MCP server with HTTP transport
   - Called app.MapMcp() for endpoint mapping

### New Files
- **Knowledge.Mcp/MCP_HTTP_IMPLEMENTATION_PROGRESS.md** (this document)

---

## Key Learnings

1. **Dual transport is possible** - Same MCP server can support both STDIO and HTTP
2. **Web SDK changes defaults** - Automatic content item inclusion requires `Update` instead of `Include`
3. **MapMcp() may have issues** - Preview package might not work as documented
4. **MCP SDK is transport-agnostic** - Tools, resources, and core logic work the same regardless of transport
5. **HTTP requires explicit routing** - ASP.NET Core needs proper endpoint configuration

---

## Comparison: STDIO vs HTTP

| Feature | STDIO Transport | HTTP SSE Transport |
|---------|----------------|-------------------|
| **Use Case** | Claude Desktop, CLI tools | Web apps, remote clients |
| **Connection** | stdin/stdout pipes | HTTP POST + Server-Sent Events |
| **Sessions** | Single persistent session | Multiple concurrent sessions |
| **Firewall** | Local only | Network accessible |
| **Load Balancing** | Not applicable | Possible with stateless mode |
| **Setup** | Simple (pipe communication) | Complex (HTTP server, routing) |
| **Current Status** | ✅ Working | ⚠️ Infrastructure ready, endpoints 404 |

---

## Decision Point

**Do we:**
1. ✅ Continue debugging MapMcp() - try to fix SDK endpoint registration
2. ✅ Implement manual HTTP endpoints per MCP Streamable HTTP spec
3. ✅ Document current state and move forward with STDIO only for now

**Current blockers:**
- `ModelContextProtocol.AspNetCore` preview package not creating endpoints
- No clear documentation on additional configuration required
- May need to wait for stable SDK release or implement manually

---

**Last Updated:** 2025-10-19 19:30 UTC
**Next:** Debug MapMcp() or implement manual endpoints per MCP specification

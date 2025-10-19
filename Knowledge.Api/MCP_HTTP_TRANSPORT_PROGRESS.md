# MCP HTTP Transport Implementation - Progress Report

**Date:** 2025-10-19
**Status:** ⚠️ IN PROGRESS - Endpoint registration issue

---

## Summary

Implemented initial MCP HTTP SSE transport in Knowledge.Api using the official `ModelContextProtocol.AspNetCore` package. Build succeeds, but endpoint registration requires further investigation.

---

## Completed Tasks ✅

### 1. Package Installation
**File:** `Knowledge.Api/Knowledge.Api.csproj`
```xml
<PackageReference Include="ModelContextProtocol.AspNetCore" Version="0.4.0-preview.2" />
```

### 2. MCP Server Configuration
**File:** `Knowledge.Api/Program.cs` (Lines 225-237)
```csharp
// ── MCP Server Configuration (HTTP SSE Transport) ────────────────────────────
// Register MCP server with HTTP transport and tools
// Note: This provides HTTP SSE access to the same MCP tools available via STDIO
// Resources will be added once Knowledge.Mcp library is extracted from executable
builder.Services
    .AddMcpServer()
    .WithHttpTransport(options =>
    {
        // Configure session timeout and limits for HTTP SSE transport
        options.IdleTimeout = TimeSpan.FromHours(1);
        options.MaxIdleSessionCount = 100;
    })
    .WithToolsFromAssembly(); // Discovers tools with [McpServerTool] attribute
```

### 3. Endpoint Mapping
**File:** `Knowledge.Api/Program.cs` (Lines 253-255)
```csharp
// ── MCP HTTP Endpoints (must come before middleware) ─────────────────────────
// Map MCP endpoints (adds /api/mcp/messages and /api/mcp/sse)
app.MapMcp("/api/mcp");
```

### 4. Build Status
✅ **Build:** SUCCESS (0 errors, 64 warnings - XML docs only)
```
Knowledge.Api -> /home/wayne/repos/ChatComplete/Knowledge.Api/bin/Debug/net8.0/linux-x64/Knowledge.Api.dll
Build succeeded.
Time Elapsed 00:00:04.01
```

### 5. Cleanup
✅ Deleted unnecessary `/home/wayne/repos/ChatComplete/Knowledge.Mcp/Transport/` folder
(MCP SDK handles transport internally - custom transport classes not needed)

---

## Current Issue ⚠️

### Endpoint Registration Not Working

**Problem:**
POST requests to `/api/mcp/messages` return 404 with logs showing:
```
[19:29:23 INF] Request starting HTTP/1.1 POST http://localhost:7040/api/mcp/messages - application/json 243
[19:29:23 DBG] No candidates found for the request path '/api/mcp/messages'
[19:29:23 DBG] POST requests are not supported
[19:29:23 INF] HTTP POST /api/mcp/messages responded 404 in 13.9821 ms
```

**Expected Endpoints:**
- `POST /api/mcp/messages` - JSON-RPC requests
- `GET /api/mcp/sse` - Server-Sent Events stream

**Test Command:**
```bash
curl -X POST http://localhost:7040/api/mcp/messages \
  -H "Content-Type: application/json" \
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
```

**Result:** 404 Not Found

---

## Investigation Needed

### Possible Causes

1. **Endpoint Path Mismatch**
   - `MapMcp("/api/mcp")` may not create `/api/mcp/messages`
   - SDK might expect different path format
   - Try `MapMcp()` without parameters (defaults to root)

2. **Middleware Order**
   - `MapMcp()` called before middleware pipeline
   - May need different placement in startup

3. **CORS Configuration**
   - CORS policy might be blocking MCP endpoints
   - Need to verify CORS allows MCP paths

4. **Missing Service Registration**
   - `WithToolsFromAssembly()` might not find any tools
   - Tools are in Knowledge.Mcp (executable project, not library)
   - May need to copy tools to Knowledge.Api

5. **SDK Version Issue**
   - Using preview version `0.4.0-preview.2`
   - API surface might have changed
   - Check latest SDK documentation

---

## Architecture Challenges

### Problem: Knowledge.Mcp is an Executable

**Current Structure:**
```
Knowledge.Mcp/         (executable - can't be referenced)
├── Tools/             (11 MCP tools)
│   ├── CrossKnowledgeSearchMcpTool.cs
│   ├── KnowledgeAnalyticsMcpTool.cs
│   ├── ModelRecommendationMcpTool.cs
│   └── SystemHealthMcpTool.cs
└── Resources/         (3 static + 3 parameterized resources)
    ├── KnowledgeResourceMethods.cs
    ├── KnowledgeResourceProvider.cs
    └── ResourceUriParser.cs
```

**Issue:**
Knowledge.Api (self-contained executable) cannot reference Knowledge.Mcp (non-self-contained executable)

**Error When Attempted:**
```
error NETSDK1150: The referenced project '../Knowledge.Mcp/Knowledge.Mcp.csproj' is a non self-contained
executable. A non self-contained executable cannot be referenced by a self-contained executable.
```

**Solutions:**
1. **Option A:** Extract MCP tools/resources to shared library project (e.g., `Knowledge.Mcp.Core`)
2. **Option B:** Copy tools directly into Knowledge.Api project
3. **Option C:** Use reflection to load tools dynamically
4. **Option D:** Change Knowledge.Api to non-self-contained (not recommended - breaks Docker deployment)

---

## Next Steps

### Immediate (Debug Endpoint Registration)
1. ✅ Try `app.MapMcp()` without path parameter
2. ✅ Check endpoint routing with minimal example
3. ✅ Verify SDK documentation for correct usage pattern
4. ✅ Add debug logging to trace endpoint registration
5. ✅ Check if CORS is interfering

### Short-term (Get Basic HTTP Working)
1. Get `initialize` request working
2. Test `tools/list` request
3. Verify SSE endpoint (`GET /api/mcp/sse`)
4. Test with MCP Inspector tool

### Medium-term (Add Tools & Resources)
1. Extract Knowledge.Mcp tools to shared library
2. Register tools with Knowledge.Api MCP server
3. Test cross-knowledge search via HTTP
4. Add MCP resources support
5. Implement resource templates discovery

### Long-term (Client Integration)
1. Update MCP Client to support HTTP transport
2. Test STDIO vs HTTP performance
3. Document HTTP setup for users
4. Add configuration for transport selection

---

## References

### MCP SDK Documentation
- **StreamableHttpServerTransport:** https://modelcontextprotocol.github.io/csharp-sdk/api/ModelContextProtocol.Server.StreamableHttpServerTransport.html
- **MapMcp Extension:** Part of ModelContextProtocol.AspNetCore package
- **Expected Endpoints:**
  - `/sse` - Server-Sent Events for streaming
  - `/messages` - HTTP POST for JSON-RPC requests

### Implementation Examples
- ASP.NET Core integration uses `MapMcp()` extension method
- Supports stateless mode (no sticky sessions required)
- Unsolicited server-to-client messages via SSE

---

## Configuration

### appsettings.json (No Changes Needed)
Current MCP configuration in Knowledge.Mcp still works via STDIO:
```json
{
  "ChatCompleteSettings": {
    "DatabasePath": "./data/knowledge.db",
    "VectorStore": {
      "Provider": "Qdrant",
      "Qdrant": {
        "Host": "localhost",
        "Port": 6334,
        "UseHttps": false
      }
    }
  }
}
```

### Knowledge.Api Launch Settings
HTTP server runs on port 7040:
```json
{
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "applicationUrl": "http://localhost:7040",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

---

## Testing Plan

### Once Endpoints Work:

#### Test 1: Initialize
```bash
curl -X POST http://localhost:7040/api/mcp/messages \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc": "2.0", "id": 1, "method": "initialize", "params": {...}}'
```

**Expected:** JSON-RPC response with server capabilities

#### Test 2: Tools List
```bash
curl -X POST http://localhost:7040/api/mcp/messages \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc": "2.0", "id": 2, "method": "tools/list"}'
```

**Expected:** List of available MCP tools (once tools are registered)

#### Test 3: SSE Stream
```bash
curl -N http://localhost:7040/api/mcp/sse
```

**Expected:** Server-Sent Events stream with `Content-Type: text/event-stream`

---

## Files Modified

### New Files
- `Knowledge.Api/MCP_HTTP_TRANSPORT_PROGRESS.md` (this document)

### Modified Files
1. **Knowledge.Api/Knowledge.Api.csproj**
   - Added `ModelContextProtocol.AspNetCore` package reference

2. **Knowledge.Api/Program.cs**
   - Added MCP server configuration (lines 225-237)
   - Added MCP endpoint mapping (lines 253-255)

### Deleted Files
- `/home/wayne/repos/ChatComplete/Knowledge.Mcp/Transport/` (entire folder)

---

## Key Learnings

1. **MCP SDK provides built-in HTTP SSE support** - no need for custom transport classes
2. **ModelContextProtocol.AspNetCore package required** for ASP.NET Core integration
3. **Executable projects cannot reference other executables** - need shared library for cross-project code sharing
4. **WithToolsFromAssembly() discovers tools via attributes** - tools must be in same assembly or referenced library
5. **MapMcp() should add endpoints automatically** - but something is preventing registration

---

## Decision Point

**Do we need to:**
1. ✅ Debug why `MapMcp()` isn't registering endpoints
2. ✅ Extract Knowledge.Mcp tools to shared library
3. ✅ Test HTTP transport works before updating client
4. ✅ Keep STDIO transport in Knowledge.Mcp working

**Current blockers:**
- Endpoint registration not working despite correct SDK usage
- Tools not accessible from Knowledge.Api (different assembly)

---

**Last Updated:** 2025-10-19 19:30 UTC
**Next:** Debug MapMcp() endpoint registration issue

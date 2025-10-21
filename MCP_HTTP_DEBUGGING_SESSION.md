# MCP HTTP Transport Debugging Session

**Date:** 2025-10-19
**Status:** 🔧 DEBUGGING REQUIRED - MCP Inspector connects but tools/resources have errors

---

## Current Situation

### What's Working ✅
1. **Knowledge.Mcp STDIO mode** - Works perfectly with Claude Desktop
2. **Knowledge.Mcp HTTP mode** - Server starts and MCP Inspector can connect
3. **MCP Inspector connection** - Can see tools and resources listed
4. **Project builds** - No compilation errors

### What's Broken ❌
1. **Tool execution** - Errors when trying to run tools via MCP Inspector
2. **Resource access** - Errors when trying to access resources via MCP Inspector
3. **SDK endpoint registration** - `MapMcp()` returns 404 for `/messages` and `/sse` endpoints

---

## Project Structure

```
ChatComplete/
├── Knowledge.Api/          # Main REST API (port 7040)
│   └── Program.cs          # Web application for UI
│
├── Knowledge.Mcp/          # MCP Server (port 5000)
│   ├── Program.cs          # Main entry point with dual transport
│   ├── MinimalHttpProgram.cs  # Minimal test server
│   ├── Tools/              # 11 MCP tools
│   │   ├── CrossKnowledgeSearchMcpTool.cs
│   │   ├── KnowledgeAnalyticsMcpTool.cs
│   │   ├── ModelRecommendationMcpTool.cs
│   │   └── SystemHealthMcpTool.cs
│   └── Resources/          # 6 MCP resources (3 static + 3 parameterized)
│       ├── KnowledgeResourceMethods.cs
│       ├── KnowledgeResourceProvider.cs
│       └── ResourceUriParser.cs
│
└── McpClient/              # Separate repo at /home/wayne/repos/McpClient
    └── Program.cs          # C# MCP client (currently uses STDIO)
```

---

## How to Start the MCP Server

### Option 1: STDIO Mode (for Claude Desktop) ✅ WORKING
```bash
cd /home/wayne/repos/ChatComplete
dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj

# Or from binary:
dotnet Knowledge.Mcp/bin/Debug/net8.0/Knowledge.Mcp.dll
```

### Option 2: HTTP SSE Mode (for web clients) ⚠️ PARTIALLY WORKING
```bash
cd /home/wayne/repos/ChatComplete
dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj -- --http

# Server will start on: http://localhost:5000
# Expected endpoints:
#   GET  /sse       - Server-Sent Events stream
#   POST /messages  - JSON-RPC requests
```

### Option 3: Minimal HTTP Mode (for testing) ⚠️ SAME ISSUE
```bash
cd /home/wayne/repos/ChatComplete
dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj -- --minimal-http

# Minimal server with just 2 test tools
# Server on: http://localhost:5000
```

---

## MCP Inspector Connection

### Connection Details
**Endpoint:** `http://localhost:5000/sse`

**What MCP Inspector Shows:**
- ✅ Successfully connects to server
- ✅ Lists all 11 tools (cross-knowledge search, analytics, health, model recommendations)
- ✅ Lists all 6 resources (system health, knowledge bases, provider analytics, etc.)
- ❌ **Errors when executing tools** (need details from user)
- ❌ **Errors when accessing resources** (need details from user)

---

## Known Issues

### Issue 1: MapMcp() Endpoint Registration Fails
**Symptom:**
```bash
curl -X POST http://localhost:5000/messages
# Returns: HTTP/1.1 404 Not Found
```

**Expected:** Should handle JSON-RPC requests

**Code:**
```csharp
// Knowledge.Mcp/Program.cs or MinimalHttpProgram.cs
builder.Services.AddMcpServer().WithHttpTransport().WithToolsFromAssembly();
var app = builder.Build();
app.MapMcp();  // ← This should create /messages and /sse endpoints
app.Run();
```

**Package Used:** `ModelContextProtocol.AspNetCore` v0.4.0-preview.2

**Investigation Attempts:**
1. ✅ Tried with/without base path: `app.MapMcp()` and `app.MapMcp("/api/mcp")`
2. ✅ Added routing middleware: `app.UseRouting()`
3. ✅ Created minimal test server (same issue)
4. ✅ Verified package is installed and version is correct

**Hypothesis:** Preview SDK package may have bugs or require additional configuration not documented

### Issue 2: Tool Execution Errors
**Symptom:** MCP Inspector shows tools but errors when executing them

**Details Needed:**
- What error message appears?
- Which tool was being tested?
- Does it fail for all tools or specific ones?
- Stack trace or error details from MCP Inspector

### Issue 3: Resource Access Errors
**Symptom:** MCP Inspector shows resources but errors when accessing them

**Details Needed:**
- What error message appears?
- Which resource was being tested (static or parameterized)?
- Error details from MCP Inspector

---

## Available Tools (11 Total)

### Cross-Knowledge Search
- `search_knowledge` - Search specific knowledge base
- `search_all_knowledge` - Search across all knowledge bases
- `compare_knowledge_bases` - Compare content between knowledge bases

### Knowledge Analytics
- `get_knowledge_base_summary` - Get summary of all knowledge bases
- `get_knowledge_base_health` - Health check for knowledge bases
- `get_storage_optimization` - Storage optimization recommendations

### Model Recommendations
- `get_popular_models` - Most popular AI models based on usage
- `compare_models` - Compare multiple models side-by-side
- `get_model_performance` - Performance analysis for specific model

### System Health
- `get_system_health` - Overall system health status
- `check_component_health` - Health of specific component

---

## Available Resources (6 Total)

### Static Resources (3)
1. `resource://system/health` - System health status
2. `resource://knowledge/bases` - List of all knowledge bases
3. `resource://analytics/providers` - Provider connection status

### Parameterized Resources (3)
1. `resource://knowledge/{knowledgeId}` - Specific knowledge base details
2. `resource://analytics/provider/{providerName}` - Provider-specific analytics
3. `resource://analytics/model/{modelName}` - Model-specific performance

---

## Configuration Files

### Knowledge.Mcp/appsettings.json
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
    },
    "OllamaBaseUrl": "http://localhost:11434",
    "EmbeddingProviders": {
      "ActiveProvider": "Ollama",
      "Ollama": {
        "ModelName": "nomic-embed-text"
      }
    }
  }
}
```

### Knowledge.Mcp/Knowledge.Mcp.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="ModelContextProtocol" Version="0.4.0-preview.2" />
    <PackageReference Include="ModelContextProtocol.AspNetCore" Version="0.4.0-preview.2" />
  </ItemGroup>
</Project>
```

---

## Code Snippets for Debugging

### Minimal HTTP Server (MinimalHttpProgram.cs)
```csharp
public static async Task<int> RunMinimalHttp(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);

    // Register MCP server and discover tools from the current assembly
    builder.Services.AddMcpServer().WithHttpTransport().WithToolsFromAssembly();

    var app = builder.Build();

    // Add MCP middleware
    app.MapMcp();

    Console.WriteLine("Minimal MCP Server started!");
    Console.WriteLine("Test SSE endpoint: curl http://localhost:5000/sse");

    await app.RunAsync();
    return 0;
}
```

### Example Tool Definition (TestTools class in MinimalHttpProgram.cs)
```csharp
[McpServerToolType]
public sealed class TestTools
{
    [McpServerTool, Description("Says Hello to a user")]
    public static string SayHello(string username)
    {
        return $"Hello, {username}!";
    }
}
```

### Example Resource Definition (KnowledgeResourceMethods.cs)
```csharp
[McpServerResource(
    Uri = "resource://system/health",
    Name = "System Health",
    Description = "Current health status of all system components",
    MimeType = "application/json"
)]
public async Task<string> GetSystemHealthAsync()
{
    var health = await _systemHealthService.GetSystemHealthAsync();
    return JsonSerializer.Serialize(health, _jsonOptions);
}
```

---

## Error Investigation Checklist

When tool/resource execution fails, check:

### 1. Server-Side Errors
```bash
# Check if server is running and logging errors
dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj -- --http

# Look for exceptions in console output
# Enable detailed logging if needed
```

### 2. Dependency Issues
- ✅ Qdrant running on port 6334? `curl http://localhost:6333/health`
- ✅ Database accessible? Check path in appsettings.json
- ✅ Ollama running? `curl http://localhost:11434/api/tags`

### 3. Tool-Specific Errors
```bash
# Test specific tool via MCP Inspector
# Check if error is:
# - Parameter validation (missing/invalid params)
# - Service dependency (Qdrant, database, etc.)
# - Authentication/authorization
# - Serialization/deserialization
```

### 4. Resource-Specific Errors
```bash
# Test specific resource via MCP Inspector
# Check if error is:
# - URI parsing (parameterized resources)
# - Permission issues (file access, database)
# - Data availability (empty knowledge bases)
# - Serialization issues (JSON conversion)
```

---

## Debugging Steps

### Step 1: Get Error Details from MCP Inspector
**Need from user:**
1. Exact error message when executing a tool
2. Which tool was tested (e.g., `get_system_health`)
3. What parameters were provided (if any)
4. Stack trace or error details shown in MCP Inspector

### Step 2: Check Server Logs
```bash
# Run server with verbose logging
dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj -- --http

# Watch console for:
# - Unhandled exceptions
# - Dependency injection errors
# - Database connection failures
# - JSON serialization errors
```

### Step 3: Test Individual Components
```bash
# Test if tools work in STDIO mode
dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj
# Then connect with Claude Desktop or MCP client

# If STDIO works but HTTP doesn't, issue is transport-specific
# If both fail, issue is in tool/resource implementation
```

### Step 4: Simplify and Isolate
```bash
# Test minimal server with simple tools
dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj -- --minimal-http

# If TestTools.SayHello() works, issue is in complex tools
# If TestTools.SayHello() fails, issue is in HTTP transport layer
```

---

## Quick Reference Commands

### Build and Test
```bash
cd /home/wayne/repos/ChatComplete

# Build
dotnet build Knowledge.Mcp/Knowledge.Mcp.csproj

# Run STDIO mode
dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj

# Run HTTP mode
dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj -- --http

# Run minimal HTTP
dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj -- --minimal-http
```

### Test Endpoints (Currently Returns 404)
```bash
# Test SSE
curl -i http://localhost:5000/sse -H 'Accept: text/event-stream'

# Test JSON-RPC initialize
curl -X POST http://localhost:5000/messages \
  -H 'Content-Type: application/json' \
  -H 'Accept: application/json, text/event-stream' \
  -d '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}'
```

### Check Dependencies
```bash
# Qdrant
curl http://localhost:6333/health

# Ollama
curl http://localhost:11434/api/tags

# Database
ls -lh /home/wayne/repos/ChatComplete/Knowledge.Api/bin/Debug/net8.0/linux-x64/data/knowledge.db
```

---

## Next Steps for Debugging

### Priority 1: Get Error Details
1. Run `dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj -- --http`
2. Connect with MCP Inspector to `http://localhost:5000/sse`
3. Try executing a simple tool (e.g., `get_system_health`)
4. **Copy the exact error message** and stack trace
5. Try accessing a simple resource (e.g., `resource://system/health`)
6. **Copy the exact error message** and stack trace

### Priority 2: Compare STDIO vs HTTP
1. Test same tool in STDIO mode (with Claude Desktop)
2. If STDIO works but HTTP fails → transport layer issue
3. If both fail → tool implementation issue

### Priority 3: Test Dependencies
1. Verify all required services are running (Qdrant, Ollama, database)
2. Check if error is dependency-related or code-related

### Priority 4: Manual Endpoint Implementation
If `MapMcp()` continues to fail, implement endpoints manually per MCP spec:
```csharp
app.MapPost("/messages", async (HttpContext ctx) => { /* handle JSON-RPC */ });
app.MapGet("/sse", async (HttpContext ctx) => { /* handle SSE stream */ });
```

---

## Related Documentation

### Files to Review
- `/home/wayne/repos/ChatComplete/Knowledge.Mcp/MCP_HTTP_IMPLEMENTATION_PROGRESS.md` - Implementation history
- `/home/wayne/repos/ChatComplete/Knowledge.Api/MCP_HTTP_TRANSPORT_PROGRESS.md` - Earlier attempt (wrong project)
- `/home/wayne/repos/ChatComplete/documentation/MCP_PHASE_2C_COMPLETION.md` - Resources implementation
- `/home/wayne/repos/ChatComplete/documentation/MCP_CLIENT_TESTING_GUIDE.md` - Testing with different clients

### Key Findings
1. **MapMcp() doesn't create endpoints** - Returns 404 despite following examples
2. **MCP Inspector connects** - SSE connection works, but tool/resource execution fails
3. **STDIO mode works** - Same tools/resources work fine in Claude Desktop
4. **Preview SDK may have issues** - Using v0.4.0-preview.2, may need manual implementation

---

## Questions to Answer

1. **What is the exact error when executing tools via MCP Inspector?**
   - Error message?
   - Stack trace?
   - Which tool?

2. **What is the exact error when accessing resources via MCP Inspector?**
   - Error message?
   - Stack trace?
   - Which resource?

3. **Do the tools work in STDIO mode?**
   - Connect with Claude Desktop and test
   - If yes → HTTP transport issue
   - If no → Tool implementation issue

4. **Is there server-side logging showing errors?**
   - Check console output when MCP Inspector tries to execute tool
   - Any unhandled exceptions?
   - Dependency injection errors?

5. **Should we implement manual HTTP endpoints?**
   - If `MapMcp()` can't be fixed
   - Follow MCP Streamable HTTP specification directly
   - Would give us full control over request/response handling

---

**Last Updated:** 2025-10-19 19:35 UTC
**Status:** Awaiting error details from MCP Inspector to continue debugging

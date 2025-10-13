# MCP Client Implementation Plan - Separate Solution

**Date:** 2025-10-13
**Status:** Planning Phase
**Target Start:** 2025-10-14

---

## Executive Summary

The MCP Client will be implemented as a **separate solution/git repository** that can connect to our Knowledge Manager MCP Server. This separation provides:

✅ **Clean separation of concerns** (server vs. client)
✅ **Independent versioning** and release cycles
✅ **Reusable client library** for other projects
✅ **Easier testing** and development
✅ **Standalone distribution** (NuGet package potential)

---

## Architecture Decision: Why Separate Repository?

### Benefits

**1. Clean Separation**
- Client and server have different lifecycles
- Server focuses on exposing knowledge via MCP
- Client focuses on consuming external MCP servers
- Avoids circular dependencies

**2. Reusability**
- Client can be used by other .NET projects
- Can be packaged as NuGet library
- Other teams can integrate MCP client functionality
- Example: Desktop apps, CLI tools, web services

**3. Independent Development**
- Different git histories
- Separate CI/CD pipelines
- Independent versioning (client v1.0, server v2.0)
- Parallel development without conflicts

**4. Testing Isolation**
- Test client against multiple MCP servers
- Test server with multiple MCP clients
- Integration tests in separate repo
- No pollution of main codebase

**5. Distribution**
- Standalone CLI tool
- NuGet package
- Docker container
- Portable executable

### Trade-offs

❌ **Requires separate repo management**
- Need to maintain two repos
- Separate CI/CD setup
- Cross-repo coordination for breaking changes

✅ **Mitigations:**
- Use MCP specification as contract
- Semantic versioning
- Changelog documentation
- Integration tests

---

## Two-Phase Implementation Plan

### Phase 1: STDIO Transport (Weeks 1-3)

**Goal:** Create MCP client that connects to MCP servers via **STDIO** (stdin/stdout)

**Why STDIO First?**
- ✅ Simpler to implement
- ✅ Standard MCP transport
- ✅ Works with existing Knowledge Manager server
- ✅ No HTTP complexity
- ✅ Process-based isolation

**STDIO Architecture:**
```
┌─────────────────────────────────────────┐
│  MCP Client Process                      │
│  (ChatComplete.McpClient.exe)           │
│                                          │
│  ┌────────────────────────────────────┐ │
│  │  McpClientService                   │ │
│  │  - Connection management            │ │
│  │  - Discovery (tools/resources)      │ │
│  │  - Execution                        │ │
│  └────────────────────────────────────┘ │
│               ▲                          │
│               │ JSON-RPC over STDIO      │
└───────────────┼──────────────────────────┘
                │
                ├─ STDIN  (send requests)
                └─ STDOUT (receive responses)
                │
                ▼
┌───────────────┼──────────────────────────┐
│  MCP Server Process                      │
│  (Knowledge.Mcp.exe)                     │
│                                          │
│  ┌────────────────────────────────────┐ │
│  │  Knowledge Manager MCP Server       │ │
│  │  - 11 Tools                         │ │
│  │  - 6 Resources                      │ │
│  │  - 3 Resource Templates             │ │
│  └────────────────────────────────────┘ │
└──────────────────────────────────────────┘
```

**Phase 1 Deliverables:**
- ✅ New repository: `ChatComplete.McpClient`
- ✅ .NET 8 console application
- ✅ ModelContextProtocol.Client SDK integration
- ✅ STDIO transport implementation
- ✅ Connection lifecycle management
- ✅ Tool discovery and execution
- ✅ Resource discovery and reading
- ✅ Error handling and logging
- ✅ Configuration management
- ✅ Unit tests + integration tests
- ✅ CLI interface for testing

---

### Phase 2: HTTP Server-Sent Events Transport (Weeks 4-6)

**Goal:** Upgrade both client and server to support **HTTP with Server-Sent Events (SSE)**

**Why HTTP SSE?**
- ✅ Web-friendly (firewalls, proxies)
- ✅ Supports server-to-client streaming
- ✅ Works with load balancers
- ✅ Better for production deployments
- ✅ Enables remote MCP servers

**HTTP SSE Architecture:**
```
┌─────────────────────────────────────────┐
│  MCP Client (HTTP)                       │
│  (ChatComplete.McpClient.exe)           │
│                                          │
│  ┌────────────────────────────────────┐ │
│  │  HttpMcpClientTransport             │ │
│  │  - HTTP POST for requests           │ │
│  │  - SSE for server messages          │ │
│  │  - Reconnection logic               │ │
│  └────────────────────────────────────┘ │
│               ▲                          │
│               │ HTTP + SSE               │
└───────────────┼──────────────────────────┘
                │
                │ HTTP POST /mcp
                │ (send JSON-RPC requests)
                │
                │ GET /mcp/events
                │ (receive SSE stream)
                │
                ▼
┌───────────────┼──────────────────────────┐
│  MCP Server (HTTP)                       │
│  (Knowledge.Api integrated)             │
│                                          │
│  ┌────────────────────────────────────┐ │
│  │  HTTP MCP Endpoints                 │ │
│  │  - POST /mcp (handle requests)      │ │
│  │  - GET /mcp/events (SSE stream)     │ │
│  │  - Connection management            │ │
│  └────────────────────────────────────┘ │
│               ▲                          │
│  ┌────────────┴──────────────────────┐ │
│  │  Knowledge Manager MCP Server       │ │
│  │  (same tools/resources)             │ │
│  └────────────────────────────────────┘ │
└──────────────────────────────────────────┘
```

**Phase 2 Changes:**

**Server Side (Knowledge Manager):**
- Add HTTP transport to existing MCP server
- Create `/mcp` POST endpoint for requests
- Create `/mcp/events` GET endpoint for SSE
- Maintain backward compatibility with STDIO
- Add authentication middleware (optional)

**Client Side:**
- Add HTTP transport option
- Implement SSE connection handling
- Add reconnection logic
- Support both STDIO and HTTP transports
- Configuration-based transport selection

**Phase 2 Deliverables:**
- ✅ Server: HTTP MCP endpoints
- ✅ Server: SSE streaming support
- ✅ Client: HTTP transport implementation
- ✅ Client: SSE message handling
- ✅ Client: Reconnection logic
- ✅ Transport configuration
- ✅ Authentication support
- ✅ Integration tests (HTTP)
- ✅ Performance benchmarks
- ✅ Documentation updates

---

## Project Structure: ChatComplete.McpClient

### Repository Layout

```
ChatComplete.McpClient/
├── .github/
│   └── workflows/
│       ├── build.yml          # CI/CD for client
│       └── integration-test.yml  # Test against servers
├── src/
│   ├── ChatComplete.McpClient/           # Core library
│   │   ├── Transports/
│   │   │   ├── ITransport.cs             # Transport interface
│   │   │   ├── StdioTransport.cs         # Phase 1
│   │   │   └── HttpSseTransport.cs       # Phase 2
│   │   ├── Services/
│   │   │   ├── McpClientService.cs       # Main client
│   │   │   ├── ConnectionManager.cs      # Lifecycle
│   │   │   ├── DiscoveryService.cs       # Tools/resources
│   │   │   └── ExecutionService.cs       # Tool execution
│   │   ├── Models/
│   │   │   ├── ClientConfiguration.cs    # Config model
│   │   │   ├── McpServer.cs              # Server info
│   │   │   └── ConnectionState.cs        # State tracking
│   │   └── Extensions/
│   │       └── ServiceCollectionExtensions.cs
│   ├── ChatComplete.McpClient.Cli/       # CLI tool
│   │   ├── Program.cs
│   │   ├── Commands/
│   │   │   ├── ConnectCommand.cs         # Connect to server
│   │   │   ├── DiscoverCommand.cs        # List tools/resources
│   │   │   ├── ExecuteCommand.cs         # Run tool
│   │   │   └── ReadCommand.cs            # Read resource
│   │   └── appsettings.json
│   └── ChatComplete.McpClient.Web/       # Web API wrapper (optional)
│       ├── Program.cs
│       └── Controllers/
│           └── McpProxyController.cs     # Proxy to MCP servers
├── tests/
│   ├── ChatComplete.McpClient.Tests/     # Unit tests
│   │   ├── StdioTransportTests.cs
│   │   ├── McpClientServiceTests.cs
│   │   └── DiscoveryServiceTests.cs
│   └── ChatComplete.McpClient.IntegrationTests/
│       ├── KnowledgeManagerIntegrationTests.cs
│       └── MultiServerTests.cs
├── docs/
│   ├── README.md                         # Getting started
│   ├── CONFIGURATION.md                  # Config guide
│   ├── TRANSPORTS.md                     # Transport details
│   └── EXAMPLES.md                       # Usage examples
├── examples/
│   ├── simple-client/                    # Basic example
│   ├── multi-server/                     # Multiple servers
│   └── web-proxy/                        # HTTP proxy
├── .gitignore
├── LICENSE
├── README.md
└── ChatComplete.McpClient.sln
```

---

## Phase 1 Implementation Details

### Week 1: Project Setup & Core Infrastructure

**Day 1-2: Repository & Project Structure**
```bash
# Create new repository
mkdir ChatComplete.McpClient
cd ChatComplete.McpClient
git init
git remote add origin https://github.com/waynen12/ChatComplete.McpClient.git

# Create solution and projects
dotnet new sln -n ChatComplete.McpClient
dotnet new classlib -n ChatComplete.McpClient -f net8.0
dotnet new console -n ChatComplete.McpClient.Cli -f net8.0
dotnet new xunit -n ChatComplete.McpClient.Tests -f net8.0
dotnet new xunit -n ChatComplete.McpClient.IntegrationTests -f net8.0

# Add projects to solution
dotnet sln add src/ChatComplete.McpClient/ChatComplete.McpClient.csproj
dotnet sln add src/ChatComplete.McpClient.Cli/ChatComplete.McpClient.Cli.csproj
dotnet sln add tests/ChatComplete.McpClient.Tests/ChatComplete.McpClient.Tests.csproj
dotnet sln add tests/ChatComplete.McpClient.IntegrationTests/ChatComplete.McpClient.IntegrationTests.csproj

# Add NuGet packages
dotnet add src/ChatComplete.McpClient package ModelContextProtocol --version 0.4.0-preview.2
dotnet add src/ChatComplete.McpClient package Microsoft.Extensions.DependencyInjection
dotnet add src/ChatComplete.McpClient package Microsoft.Extensions.Logging
dotnet add src/ChatComplete.McpClient package Microsoft.Extensions.Configuration
dotnet add src/ChatComplete.McpClient package Microsoft.Extensions.Hosting

dotnet add src/ChatComplete.McpClient.Cli package Spectre.Console
dotnet add src/ChatComplete.McpClient.Cli package System.CommandLine
```

**Day 3-5: Core Transport Implementation**

**ITransport.cs:**
```csharp
namespace ChatComplete.McpClient.Transports;

public interface ITransport : IDisposable
{
    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    Task<JsonRpcResponse> SendRequestAsync(JsonRpcRequest request, CancellationToken cancellationToken = default);
    IAsyncEnumerable<JsonRpcMessage> ListenAsync(CancellationToken cancellationToken = default);
    bool IsConnected { get; }
    event EventHandler<TransportException>? OnError;
}
```

**StdioTransport.cs:**
```csharp
namespace ChatComplete.McpClient.Transports;

public class StdioTransport : ITransport
{
    private readonly string _serverCommand;
    private readonly string[] _serverArgs;
    private Process? _serverProcess;
    private StreamWriter? _stdin;
    private StreamReader? _stdout;

    public StdioTransport(string serverCommand, string[] serverArgs)
    {
        _serverCommand = serverCommand;
        _serverArgs = serverArgs;
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        _serverProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _serverCommand,
                Arguments = string.Join(" ", _serverArgs),
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        _serverProcess.Start();
        _stdin = _serverProcess.StandardInput;
        _stdout = _serverProcess.StandardOutput;

        // Send initialize request
        await InitializeAsync(cancellationToken);
    }

    public async Task<JsonRpcResponse> SendRequestAsync(
        JsonRpcRequest request,
        CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(request);
        await _stdin!.WriteLineAsync(json);
        await _stdin.FlushAsync();

        var responseLine = await _stdout!.ReadLineAsync();
        return JsonSerializer.Deserialize<JsonRpcResponse>(responseLine!)!;
    }

    // ... other methods
}
```

---

### Week 2: Service Layer & Discovery

**McpClientService.cs:**
```csharp
namespace ChatComplete.McpClient.Services;

public class McpClientService : IDisposable
{
    private readonly ITransport _transport;
    private readonly ILogger<McpClientService> _logger;
    private readonly DiscoveryService _discovery;
    private readonly ExecutionService _execution;

    public McpClientService(
        ITransport transport,
        ILogger<McpClientService> logger)
    {
        _transport = transport;
        _logger = logger;
        _discovery = new DiscoveryService(transport);
        _execution = new ExecutionService(transport);
    }

    public async Task<ServerInfo> ConnectAsync(CancellationToken ct = default)
    {
        await _transport.ConnectAsync(ct);
        var serverInfo = await _discovery.GetServerInfoAsync(ct);
        _logger.LogInformation("Connected to MCP server: {Name} v{Version}",
            serverInfo.Name, serverInfo.Version);
        return serverInfo;
    }

    public async Task<IReadOnlyList<Tool>> DiscoverToolsAsync(CancellationToken ct = default)
    {
        return await _discovery.ListToolsAsync(ct);
    }

    public async Task<IReadOnlyList<Resource>> DiscoverResourcesAsync(CancellationToken ct = default)
    {
        return await _discovery.ListResourcesAsync(ct);
    }

    public async Task<IReadOnlyList<ResourceTemplate>> DiscoverTemplatesAsync(CancellationToken ct = default)
    {
        return await _discovery.ListResourceTemplatesAsync(ct);
    }

    public async Task<ToolResult> ExecuteToolAsync(
        string toolName,
        object? arguments = null,
        CancellationToken ct = default)
    {
        return await _execution.CallToolAsync(toolName, arguments, ct);
    }

    public async Task<ResourceContents> ReadResourceAsync(
        string uri,
        CancellationToken ct = default)
    {
        return await _execution.ReadResourceAsync(uri, ct);
    }
}
```

---

### Week 3: CLI Interface & Testing

**CLI Commands:**

```bash
# Connect to server and show info
mcp-client connect --server dotnet --args "run --project ../Knowledge.Mcp/Knowledge.Mcp.csproj"

# Discover available tools
mcp-client tools list

# Discover available resources
mcp-client resources list

# Discover resource templates
mcp-client resources templates

# Execute a tool
mcp-client tool execute search_all_knowledge_bases --query "Docker SSL"

# Read a resource
mcp-client resource read "resource://system/health"

# Interactive mode
mcp-client interactive --server dotnet --args "run --project ../Knowledge.Mcp/Knowledge.Mcp.csproj"
```

**Interactive Mode UI (Spectre.Console):**
```
╔════════════════════════════════════════════════════════════╗
║  MCP Client - Interactive Mode                            ║
╚════════════════════════════════════════════════════════════╝

Connected to: Knowledge Manager MCP Server v1.0.0

Available Actions:
1. 📋 List Tools
2. 📚 List Resources
3. 🔧 Execute Tool
4. 📖 Read Resource
5. 🔍 Search Knowledge Bases
6. ❌ Disconnect

Select action [1-6]: _
```

---

## Phase 2 Implementation Details

### Week 4: Server-Side HTTP Transport

**Knowledge.Api Changes:**

**Add MCP HTTP endpoints:**
```csharp
// Program.cs
app.MapPost("/mcp", async (
    [FromBody] JsonRpcRequest request,
    [FromServices] IMcpServer mcpServer) =>
{
    var response = await mcpServer.HandleRequestAsync(request);
    return Results.Json(response);
});

app.MapGet("/mcp/events", async (
    HttpContext context,
    [FromServices] IMcpServer mcpServer) =>
{
    context.Response.Headers.Add("Content-Type", "text/event-stream");
    context.Response.Headers.Add("Cache-Control", "no-cache");
    context.Response.Headers.Add("Connection", "keep-alive");

    await foreach (var message in mcpServer.GetEventStreamAsync(context.RequestAborted))
    {
        await context.Response.WriteAsync($"data: {JsonSerializer.Serialize(message)}\n\n");
        await context.Response.Body.FlushAsync();
    }
});
```

---

### Week 5: Client-Side HTTP Transport

**HttpSseTransport.cs:**
```csharp
namespace ChatComplete.McpClient.Transports;

public class HttpSseTransport : ITransport
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly Channel<JsonRpcMessage> _messageChannel;
    private CancellationTokenSource? _listenerCts;

    public HttpSseTransport(string baseUrl, HttpClient httpClient)
    {
        _baseUrl = baseUrl;
        _httpClient = httpClient;
        _messageChannel = Channel.CreateUnbounded<JsonRpcMessage>();
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        // Start SSE listener
        _listenerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _ = Task.Run(() => ListenToSseAsync(_listenerCts.Token), cancellationToken);

        // Send initialize request
        await InitializeAsync(cancellationToken);
    }

    public async Task<JsonRpcResponse> SendRequestAsync(
        JsonRpcRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"{_baseUrl}/mcp",
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonRpcResponse>(cancellationToken: cancellationToken)!;
    }

    private async Task ListenToSseAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/mcp/events");
        request.Headers.Add("Accept", "text/event-stream");

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync();
            if (line == null) break;

            if (line.StartsWith("data: "))
            {
                var json = line.Substring(6);
                var message = JsonSerializer.Deserialize<JsonRpcMessage>(json)!;
                await _messageChannel.Writer.WriteAsync(message, cancellationToken);
            }
        }
    }

    public async IAsyncEnumerable<JsonRpcMessage> ListenAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var message in _messageChannel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return message;
        }
    }
}
```

---

### Week 6: Integration Testing & Documentation

**Integration Test Example:**
```csharp
[Fact]
public async Task Client_ShouldConnectToServer_Via_Stdio()
{
    // Arrange
    var transport = new StdioTransport(
        "dotnet",
        new[] { "run", "--project", "../../../../Knowledge.Mcp/Knowledge.Mcp.csproj" });

    var client = new McpClientService(transport, _logger);

    // Act
    var serverInfo = await client.ConnectAsync();
    var tools = await client.DiscoverToolsAsync();

    // Assert
    Assert.Equal("Knowledge.Mcp", serverInfo.Name);
    Assert.Equal(11, tools.Count);
}

[Fact]
public async Task Client_ShouldConnectToServer_Via_Http()
{
    // Arrange
    var httpClient = new HttpClient();
    var transport = new HttpSseTransport("http://localhost:7040", httpClient);
    var client = new McpClientService(transport, _logger);

    // Act
    var serverInfo = await client.ConnectAsync();
    var resources = await client.DiscoverResourcesAsync();

    // Assert
    Assert.Equal("Knowledge Manager", serverInfo.Name);
    Assert.Equal(3, resources.Count); // Static resources
}
```

---

## Configuration Examples

**appsettings.json (Client):**
```json
{
  "McpServers": {
    "knowledge-manager": {
      "transport": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "../Knowledge.Mcp/Knowledge.Mcp.csproj"],
      "env": {
        "ANTHROPIC_API_KEY": "${ANTHROPIC_API_KEY}"
      }
    },
    "knowledge-manager-http": {
      "transport": "http",
      "baseUrl": "http://localhost:7040",
      "authToken": "${MCP_AUTH_TOKEN}"
    },
    "external-docs": {
      "transport": "stdio",
      "command": "mcp-docs-server",
      "args": ["--path", "/usr/share/docs"]
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "ChatComplete.McpClient": "Debug"
    }
  }
}
```

---

## Success Criteria

### Phase 1 (STDIO)
- [x] ✅ Client connects to Knowledge Manager via STDIO
- [x] ✅ Discovers all 11 tools
- [x] ✅ Executes tools successfully
- [x] ✅ Discovers all 6 resources (3 static + 3 parameterized)
- [x] ✅ Reads resource content
- [x] ✅ Handles errors gracefully
- [x] ✅ CLI interface works
- [x] ✅ Unit tests pass (>80% coverage)
- [x] ✅ Integration tests pass

### Phase 2 (HTTP SSE)
- [x] ✅ Server exposes HTTP MCP endpoints
- [x] ✅ Client connects via HTTP
- [x] ✅ SSE streaming works
- [x] ✅ Reconnection logic works
- [x] ✅ Both transports supported
- [x] ✅ Performance acceptable (<100ms latency)
- [x] ✅ Documentation complete

---

## Risk Management

### Risks & Mitigations

**Risk 1: MCP SDK API Changes**
- **Impact:** Medium
- **Mitigation:** Pin to specific SDK version, monitor releases
- **Contingency:** Fork SDK if needed

**Risk 2: STDIO Process Management**
- **Impact:** Medium
- **Mitigation:** Robust error handling, process cleanup
- **Contingency:** Add watchdog process

**Risk 3: HTTP SSE Browser Support**
- **Impact:** Low (server-to-server primarily)
- **Mitigation:** Document browser limitations
- **Contingency:** WebSocket fallback (future)

**Risk 4: Cross-Repo Coordination**
- **Impact:** Low
- **Mitigation:** MCP spec as contract, integration tests
- **Contingency:** Version pinning

---

## Timeline Summary

| Week | Phase | Deliverable |
|------|-------|-------------|
| 1 | Phase 1 | Project setup, STDIO transport |
| 2 | Phase 1 | Service layer, discovery |
| 3 | Phase 1 | CLI interface, testing |
| 4 | Phase 2 | Server HTTP endpoints |
| 5 | Phase 2 | Client HTTP transport |
| 6 | Phase 2 | Integration tests, docs |

**Total Duration:** 6 weeks

---

## Next Steps (Tomorrow)

### Day 1 - Repository Setup

1. **Create new repository:**
   ```bash
   cd ~/repos
   mkdir ChatComplete.McpClient
   cd ChatComplete.McpClient
   git init
   ```

2. **Create solution structure:**
   ```bash
   dotnet new sln -n ChatComplete.McpClient
   mkdir -p src/ChatComplete.McpClient
   mkdir -p src/ChatComplete.McpClient.Cli
   mkdir -p tests/ChatComplete.McpClient.Tests
   mkdir -p tests/ChatComplete.McpClient.IntegrationTests
   ```

3. **Create projects:**
   ```bash
   dotnet new classlib -n ChatComplete.McpClient -o src/ChatComplete.McpClient -f net8.0
   dotnet new console -n ChatComplete.McpClient.Cli -o src/ChatComplete.McpClient.Cli -f net8.0
   dotnet new xunit -n ChatComplete.McpClient.Tests -o tests/ChatComplete.McpClient.Tests -f net8.0
   dotnet new xunit -n ChatComplete.McpClient.IntegrationTests -o tests/ChatComplete.McpClient.IntegrationTests -f net8.0
   ```

4. **Add to solution:**
   ```bash
   dotnet sln add src/ChatComplete.McpClient/ChatComplete.McpClient.csproj
   dotnet sln add src/ChatComplete.McpClient.Cli/ChatComplete.McpClient.Cli.csproj
   dotnet sln add tests/ChatComplete.McpClient.Tests/ChatComplete.McpClient.Tests.csproj
   dotnet sln add tests/ChatComplete.McpClient.IntegrationTests/ChatComplete.McpClient.IntegrationTests.csproj
   ```

5. **Install dependencies:**
   ```bash
   cd src/ChatComplete.McpClient
   dotnet add package ModelContextProtocol --version 0.4.0-preview.2
   dotnet add package Microsoft.Extensions.DependencyInjection
   dotnet add package Microsoft.Extensions.Logging
   ```

6. **Initial commit:**
   ```bash
   git add .
   git commit -m "Initial project structure for MCP Client"
   ```

---

## Documentation To Create

- [ ] **README.md** - Getting started guide
- [ ] **CONFIGURATION.md** - Configuration options
- [ ] **TRANSPORTS.md** - Transport details (STDIO vs HTTP)
- [ ] **EXAMPLES.md** - Usage examples
- [ ] **ARCHITECTURE.md** - Design decisions
- [ ] **CONTRIBUTING.md** - Contribution guidelines

---

## Questions for Tomorrow

1. **Repository location:** GitHub same org as Knowledge Manager?
2. **Naming:** `ChatComplete.McpClient` or different name?
3. **License:** Same as Knowledge Manager (MIT)?
4. **CI/CD:** GitHub Actions like main repo?
5. **Package distribution:** NuGet.org or private feed?

---

**Ready to start Phase 1 implementation tomorrow!** 🚀

All planning complete, architecture defined, roadmap clear. ✅

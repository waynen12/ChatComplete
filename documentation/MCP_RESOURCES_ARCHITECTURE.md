# MCP Resources: How Servers Expose Data to Clients

## Architecture Overview

MCP Resources work like a **virtual file system** that AI assistants can browse and read. Think of it as making your knowledge base "mountable" by any MCP client.

```
┌─────────────────────────────────────────────────────────────────┐
│                        MCP CLIENT                                │
│  (VS Code, Claude Desktop, Continue.dev, Custom Apps)           │
│                                                                   │
│  User: "Show me the Docker SSL setup documentation"             │
└───────────────────────┬─────────────────────────────────────────┘
                        │
                        │ 1. resources/list (list available resources)
                        ▼
┌─────────────────────────────────────────────────────────────────┐
│                    MCP SERVER (STDIO)                            │
│              Knowledge.Mcp.dll (our server)                      │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  KnowledgeResourceProvider                               │   │
│  │  ├─ ListResourcesAsync()                                 │   │
│  │  │  └─ Returns URIs of all available resources          │   │
│  │  │                                                        │   │
│  │  ├─ ReadResourceAsync(uri)                               │   │
│  │  │  └─ Returns content of specific resource             │   │
│  │  │                                                        │   │
│  │  └─ SubscribeResourceAsync(uri)                          │   │
│  │     └─ Notifies clients when resource changes           │   │
│  └─────────────────────────────────────────────────────────┘   │
│                            │                                     │
│                            │ queries                             │
│                            ▼                                     │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  IKnowledgeRepository (SQLite)                           │   │
│  │  - GetAllAsync()                                         │   │
│  │  - GetByIdAsync(id)                                      │   │
│  │  - GetDocumentsByCollectionAsync(collectionId)          │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘
                        │
                        │ 2. Response: List of resource URIs
                        ▼
┌─────────────────────────────────────────────────────────────────┐
│                        MCP CLIENT                                │
│                                                                   │
│  Received resources:                                             │
│  - resource://knowledge/docker-guides/documents                  │
│  - resource://knowledge/docker-guides/document/ssl-setup         │
│  - resource://knowledge/react-docs/documents                     │
│  - resource://system/health                                      │
│                                                                   │
│  Client picks: resource://knowledge/docker-guides/document/ssl-setup
└───────────────────────┬─────────────────────────────────────────┘
                        │
                        │ 3. resources/read (uri: resource://knowledge/...)
                        ▼
┌─────────────────────────────────────────────────────────────────┐
│                    MCP SERVER (STDIO)                            │
│                                                                   │
│  ReadResourceAsync("resource://knowledge/docker-guides/document/ssl-setup")
│   │                                                               │
│   ├─ Parse URI → collection: "docker-guides", doc: "ssl-setup"  │
│   ├─ Query SQLite for document content                          │
│   └─ Return full document text + metadata                       │
│                                                                   │
└───────────────────────┬─────────────────────────────────────────┘
                        │
                        │ 4. Response: Full document content
                        ▼
┌─────────────────────────────────────────────────────────────────┐
│                        MCP CLIENT                                │
│                                                                   │
│  Received document content:                                      │
│  {                                                               │
│    "uri": "resource://knowledge/docker-guides/document/ssl-setup"│
│    "mimeType": "text/markdown",                                  │
│    "text": "# Docker SSL Setup\n\n## Prerequisites..."          │
│  }                                                               │
│                                                                   │
│  → LLM reads full document and provides detailed answer         │
└─────────────────────────────────────────────────────────────────┘
```

---

## Protocol Flow: Message Sequence

### Step 1: Client Lists Available Resources

**Client Request (JSON-RPC 2.0):**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "resources/list",
  "params": {}
}
```

**Server Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "resources": [
      {
        "uri": "resource://knowledge/collections",
        "name": "Knowledge Base Collections",
        "description": "List of all knowledge base collections",
        "mimeType": "application/json"
      },
      {
        "uri": "resource://knowledge/docker-guides/documents",
        "name": "Docker Guides - All Documents",
        "description": "List of all documents in docker-guides collection",
        "mimeType": "application/json"
      },
      {
        "uri": "resource://knowledge/docker-guides/document/ssl-setup",
        "name": "Docker SSL Setup Guide",
        "description": "Complete documentation for setting up SSL with Docker",
        "mimeType": "text/markdown"
      },
      {
        "uri": "resource://knowledge/react-docs/documents",
        "name": "React Documentation - All Documents",
        "mimeType": "application/json"
      },
      {
        "uri": "resource://system/health",
        "name": "System Health Status",
        "description": "Current system health and component status",
        "mimeType": "application/json"
      }
    ]
  }
}
```

### Step 2: Client Reads a Specific Resource

**Client Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "resources/read",
  "params": {
    "uri": "resource://knowledge/docker-guides/document/ssl-setup"
  }
}
```

**Server Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "result": {
    "contents": [
      {
        "uri": "resource://knowledge/docker-guides/document/ssl-setup",
        "mimeType": "text/markdown",
        "text": "# Docker SSL Setup\n\n## Prerequisites\n- Docker installed\n- SSL certificates ready\n\n## Step 1: Generate Certificates\n...[full document content]..."
      }
    ]
  }
}
```

### Step 3: Client Subscribes to Resource Updates (Optional)

**Client Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "method": "resources/subscribe",
  "params": {
    "uri": "resource://knowledge/docker-guides/document/ssl-setup"
  }
}
```

**Server Notification (when document changes):**
```json
{
  "jsonrpc": "2.0",
  "method": "notifications/resources/updated",
  "params": {
    "uri": "resource://knowledge/docker-guides/document/ssl-setup"
  }
}
```

---

## Implementation: Server-Side Code

### 1. Resource Provider Class

```csharp
// Knowledge.Mcp/Resources/KnowledgeResourceProvider.cs

using ModelContextProtocol.Server;
using System.Text.Json;

namespace Knowledge.Mcp.Resources;

/// <summary>
/// Provides MCP resource access to knowledge base documents and collections.
/// Resources are read-only data endpoints that clients can browse and read.
/// </summary>
public class KnowledgeResourceProvider
{
    private readonly IKnowledgeRepository _knowledgeRepository;
    private readonly ILogger<KnowledgeResourceProvider> _logger;
    private readonly ResourceUriParser _uriParser;

    public KnowledgeResourceProvider(
        IKnowledgeRepository knowledgeRepository,
        ILogger<KnowledgeResourceProvider> logger)
    {
        _knowledgeRepository = knowledgeRepository;
        _logger = logger;
        _uriParser = new ResourceUriParser();
    }

    /// <summary>
    /// Lists all available resources (called when client sends resources/list).
    /// This is like "ls" - showing what's available to read.
    /// </summary>
    public async Task<ResourceListResult> ListResourcesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MCP: Listing all available resources");

        var resources = new List<Resource>();

        // 1. Add root collection list resource
        resources.Add(new Resource
        {
            Uri = "resource://knowledge/collections",
            Name = "Knowledge Base Collections",
            Description = "List of all knowledge base collections with metadata",
            MimeType = "application/json"
        });

        // 2. Add system health resource
        resources.Add(new Resource
        {
            Uri = "resource://system/health",
            Name = "System Health Status",
            Description = "Current system health and component status",
            MimeType = "application/json"
        });

        // 3. Add resources for each knowledge base collection
        var collections = await _knowledgeRepository.GetAllAsync(cancellationToken);

        foreach (var collection in collections)
        {
            // Collection document list
            resources.Add(new Resource
            {
                Uri = $"resource://knowledge/{collection.Id}/documents",
                Name = $"{collection.Name} - All Documents",
                Description = $"List of all documents in {collection.Name} collection",
                MimeType = "application/json"
            });

            // Collection stats
            resources.Add(new Resource
            {
                Uri = $"resource://knowledge/{collection.Id}/stats",
                Name = $"{collection.Name} - Statistics",
                Description = $"Document count, chunk count, and usage stats for {collection.Name}",
                MimeType = "application/json"
            });

            // Individual document resources (if we have document metadata)
            var documents = await _knowledgeRepository.GetDocumentsByCollectionAsync(
                collection.Id,
                cancellationToken
            );

            foreach (var doc in documents)
            {
                resources.Add(new Resource
                {
                    Uri = $"resource://knowledge/{collection.Id}/document/{doc.Id}",
                    Name = doc.Name ?? doc.Id,
                    Description = $"Full content of {doc.Name ?? doc.Id} document",
                    MimeType = "text/markdown" // or detect from doc metadata
                });
            }
        }

        _logger.LogInformation("MCP: Returning {Count} resources", resources.Count);

        return new ResourceListResult { Resources = resources };
    }

    /// <summary>
    /// Reads the content of a specific resource (called when client sends resources/read).
    /// This is like "cat" - reading the actual content.
    /// </summary>
    public async Task<ResourceReadResult> ReadResourceAsync(
        string uri,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MCP: Reading resource {Uri}", uri);

        var parsedUri = _uriParser.Parse(uri);

        return parsedUri.Type switch
        {
            ResourceType.CollectionList => await ReadCollectionListAsync(cancellationToken),
            ResourceType.DocumentList => await ReadDocumentListAsync(parsedUri.CollectionId!, cancellationToken),
            ResourceType.Document => await ReadDocumentAsync(parsedUri.CollectionId!, parsedUri.DocumentId!, cancellationToken),
            ResourceType.CollectionStats => await ReadCollectionStatsAsync(parsedUri.CollectionId!, cancellationToken),
            ResourceType.SystemHealth => await ReadSystemHealthAsync(cancellationToken),
            _ => throw new ArgumentException($"Unknown resource URI: {uri}")
        };
    }

    /// <summary>
    /// Subscribes to resource updates (called when client sends resources/subscribe).
    /// Client will be notified when this resource changes.
    /// </summary>
    public Task SubscribeToResourceAsync(string uri, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MCP: Client subscribed to resource {Uri}", uri);

        // In a real implementation, you'd register the subscription and send
        // notifications when the resource changes (e.g., document updated)

        // For now, just acknowledge the subscription
        return Task.CompletedTask;
    }

    // Private helper methods for reading different resource types

    private async Task<ResourceReadResult> ReadCollectionListAsync(CancellationToken cancellationToken)
    {
        var collections = await _knowledgeRepository.GetAllAsync(cancellationToken);

        var collectionList = collections.Select(c => new
        {
            Id = c.Id,
            Name = c.Name,
            DocumentCount = c.DocumentCount,
            CreatedAt = c.CreatedAt,
            LastUpdated = c.LastUpdated
        }).ToList();

        var json = JsonSerializer.Serialize(collectionList, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        return new ResourceReadResult
        {
            Contents = new[]
            {
                new ResourceContent
                {
                    Uri = "resource://knowledge/collections",
                    MimeType = "application/json",
                    Text = json
                }
            }
        };
    }

    private async Task<ResourceReadResult> ReadDocumentAsync(
        string collectionId,
        string documentId,
        CancellationToken cancellationToken)
    {
        var document = await _knowledgeRepository.GetDocumentAsync(
            collectionId,
            documentId,
            cancellationToken
        );

        if (document == null)
        {
            throw new ArgumentException($"Document not found: {documentId} in collection {collectionId}");
        }

        return new ResourceReadResult
        {
            Contents = new[]
            {
                new ResourceContent
                {
                    Uri = $"resource://knowledge/{collectionId}/document/{documentId}",
                    MimeType = document.MimeType ?? "text/plain",
                    Text = document.Content // Full document content
                }
            }
        };
    }

    private async Task<ResourceReadResult> ReadSystemHealthAsync(CancellationToken cancellationToken)
    {
        // Delegate to existing health check logic
        var healthService = /* get from DI */;
        var health = await healthService.GetSystemHealthAsync(cancellationToken);

        var json = JsonSerializer.Serialize(health, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        return new ResourceReadResult
        {
            Contents = new[]
            {
                new ResourceContent
                {
                    Uri = "resource://system/health",
                    MimeType = "application/json",
                    Text = json
                }
            }
        };
    }
}
```

### 2. URI Parser Helper

```csharp
// Knowledge.Mcp/Resources/ResourceUriParser.cs

namespace Knowledge.Mcp.Resources;

public enum ResourceType
{
    CollectionList,      // resource://knowledge/collections
    DocumentList,        // resource://knowledge/{collectionId}/documents
    Document,            // resource://knowledge/{collectionId}/document/{docId}
    CollectionStats,     // resource://knowledge/{collectionId}/stats
    SystemHealth         // resource://system/health
}

public class ParsedResourceUri
{
    public ResourceType Type { get; set; }
    public string? CollectionId { get; set; }
    public string? DocumentId { get; set; }
}

public class ResourceUriParser
{
    public ParsedResourceUri Parse(string uri)
    {
        if (!uri.StartsWith("resource://"))
        {
            throw new ArgumentException($"Invalid resource URI format: {uri}");
        }

        var path = uri.Substring("resource://".Length);
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
        {
            throw new ArgumentException($"Empty resource path: {uri}");
        }

        // resource://knowledge/collections
        if (parts.Length == 2 && parts[0] == "knowledge" && parts[1] == "collections")
        {
            return new ParsedResourceUri { Type = ResourceType.CollectionList };
        }

        // resource://knowledge/{collectionId}/documents
        if (parts.Length == 3 && parts[0] == "knowledge" && parts[2] == "documents")
        {
            return new ParsedResourceUri
            {
                Type = ResourceType.DocumentList,
                CollectionId = parts[1]
            };
        }

        // resource://knowledge/{collectionId}/document/{docId}
        if (parts.Length == 4 && parts[0] == "knowledge" && parts[2] == "document")
        {
            return new ParsedResourceUri
            {
                Type = ResourceType.Document,
                CollectionId = parts[1],
                DocumentId = parts[3]
            };
        }

        // resource://knowledge/{collectionId}/stats
        if (parts.Length == 3 && parts[0] == "knowledge" && parts[2] == "stats")
        {
            return new ParsedResourceUri
            {
                Type = ResourceType.CollectionStats,
                CollectionId = parts[1]
            };
        }

        // resource://system/health
        if (parts.Length == 2 && parts[0] == "system" && parts[1] == "health")
        {
            return new ParsedResourceUri { Type = ResourceType.SystemHealth };
        }

        throw new ArgumentException($"Unknown resource URI pattern: {uri}");
    }
}
```

### 3. Registration in Program.cs

```csharp
// Knowledge.Mcp/Program.cs

// Register resource provider
services.AddScoped<KnowledgeResourceProvider>();

// Configure MCP server to expose resources
builder.Services.AddMcpServer(options =>
{
    // Resources are automatically discovered from registered providers
    // The MCP SDK will call:
    // - ListResourcesAsync() for resources/list requests
    // - ReadResourceAsync(uri) for resources/read requests
    // - SubscribeToResourceAsync(uri) for resources/subscribe requests

    options.ServerInfo = new ServerInfo
    {
        Name = "knowledge-mcp",
        Version = "1.0.0"
    };
});
```

---

## Client-Side: How VS Code Uses Resources

### VS Code MCP Extension Behavior

When a user interacts with MCP resources in VS Code:

```typescript
// VS Code MCP Client (conceptual)

// 1. User asks question
const userQuestion = "Show me the Docker SSL setup documentation";

// 2. Client lists available resources
const resourcesResponse = await mcpClient.request({
  method: "resources/list"
});

// 3. Client finds relevant resource (via semantic search or exact match)
const dockerSslResource = resourcesResponse.resources.find(r =>
  r.uri.includes("docker") && r.uri.includes("ssl")
);
// Found: resource://knowledge/docker-guides/document/ssl-setup

// 4. Client reads the resource
const content = await mcpClient.request({
  method: "resources/read",
  params: { uri: dockerSslResource.uri }
});

// 5. LLM receives FULL document content (not just search snippets)
const llmContext = content.contents[0].text;
// Contains complete markdown: "# Docker SSL Setup\n\n## Prerequisites..."

// 6. LLM generates answer using full context
const answer = await llm.complete({
  messages: [
    { role: "system", content: "You have access to technical documentation." },
    { role: "user", content: userQuestion },
    { role: "assistant", content: `Based on the documentation:\n\n${llmContext}\n\nHere's how...` }
  ]
});
```

---

## Key Differences: Tools vs Resources

| Aspect | Tools (Phase 1 ✅) | Resources (Phase 2B) |
|--------|-------------------|---------------------|
| **What it does** | Executes search, returns snippets | Returns full document content |
| **LLM interaction** | "Search for X" → snippets | "Read document Y" → complete text |
| **Side effects** | Can modify state (future) | Read-only, no changes |
| **Discovery** | `tools/list` | `resources/list` |
| **Invocation** | `tools/call` + parameters | `resources/read` + URI |
| **Use case** | "Find Docker info" | "Show me ssl-setup.md" |
| **Context size** | Limited (search results) | Full document |

---

## Benefits for Your System

### 1. **Better Answers**
- LLMs get complete documents, not search snippets
- More accurate, detailed responses
- Can reference exact sections

### 2. **Browsable Knowledge Base**
- Clients can list all collections
- Discover available documentation
- Navigate like a file system

### 3. **Direct Access**
- Skip search when you know what you want
- Faster for specific document requests
- Lower latency than search

### 4. **Real-Time Updates**
- Clients subscribe to resources
- Get notified when docs change
- Always see latest content

### 5. **MCP Spec Compliance**
- Reach 50% MCP spec coverage (2/4 primitives)
- Standard way to expose data
- Works with all MCP clients

---

## Summary

**MCP Resources** turn your knowledge base into a **virtual file system** that AI assistants can:
1. **Browse** - List all available documents and collections
2. **Read** - Access full content of any document
3. **Subscribe** - Get notified when documents change

**Protocol**: Simple JSON-RPC 2.0 over STDIO
- `resources/list` → Get all available URIs
- `resources/read` → Get content of specific URI
- `resources/subscribe` → Watch for changes

**Implementation**: 2-3 files, ~500 lines of code
- KnowledgeResourceProvider (main logic)
- ResourceUriParser (URI handling)
- Program.cs updates (registration)

**Next Step**: Ready to implement when you want to start Phase 2B!

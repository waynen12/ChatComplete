# MCP Resources Testing Guide

## Phase 2B: Resources Implementation Testing

### SDK Upgrade
- ✅ Upgraded from ModelContextProtocol 0.3.0-preview.4 → 0.4.0-preview.2
- ✅ Added `.WithResourcesFromAssembly()` to Program.cs
- ✅ Created `KnowledgeResourceMethods` with `[McpServerResourceType]` attribute

### Resource Methods Implemented

All methods are in `Knowledge.Mcp/Resources/KnowledgeResourceMethods.cs`:

1. **GetCollections()** - `knowledge://collections`
   - Name: "Knowledge Collections"
   - Returns: List of all knowledge collections with document counts
   - Test: Should see all uploaded knowledge bases

2. **GetCollectionDocuments(collectionId)** - `knowledge://{collectionId}/documents`
   - Name: "Collection Documents"
   - Returns: List of documents in a specific collection
   - Test: Should see all documents with metadata (fileName, chunkCount, fileSize, etc.)

3. **GetDocument(collectionId, documentId)** - `knowledge://{collectionId}/document/{documentId}`
   - Name: "Document Content"
   - Returns: **Full document content** with MIME detection
   - Test: Should return complete reconstructed text from chunks
   - **This is the key test for Copilot's feedback**

4. **GetCollectionStats(collectionId)** - `knowledge://{collectionId}/stats`
   - Name: "Collection Statistics"
   - Returns: Analytics and usage statistics
   - Test: Should show query counts, conversation counts, etc.

5. **GetSystemHealth()** - `system://health`
   - Name: "System Health"
   - Returns: System health status (Qdrant, SQLite, Ollama, etc.)
   - Test: Should show all component health statuses

6. **GetModels()** - `system://models`
   - Name: "AI Models Inventory"
   - Returns: AI model inventory with usage statistics
   - Test: Should list all available models with performance metrics

### Testing with Copilot

**Original Issue from User:**
> Copilot asked to "Read the full Heliograph_Test_Document content with MIME detection"
> Copilot responded: "available MCP endpoints here appear limited to summary and search that return snippets; they don't expose a direct 'get document' or 'export KB' call"

**Expected Behavior After Fix:**
1. Copilot should be able to **list available resources** using MCP resources/list protocol
2. Copilot should see `resource://knowledge/{collectionId}/document/{documentId}` template
3. Copilot should be able to **read full document content** using MCP resources/read protocol
4. Response should include complete document text with MIME type information

**Test Commands for Copilot:**

```
1. "List all available knowledge base resources"
   Expected: Should see 6 resource types listed

2. "Show me all collections in the knowledge base"
   Expected: JSON with collections list

3. "Read the full Heliograph_Test_Document content with MIME detection"
   Expected: Complete document content, not snippets

4. "Export the complete content of document X from collection Y"
   Expected: Full document JSON with content field containing all text
```

### Manual Testing (if needed)

Run the MCP server:
```bash
dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj
```

The server will expose resources via STDIO transport. MCP clients (like Copilot) can now:
- Send `resources/list` request → receive all 6 resource types
- Send `resources/read` request with URI → receive resource content

### Build Status
✅ Build succeeded (0 errors, 1 warning - unrelated)
✅ 179/180 tests passing (1 failing test is pre-existing, unrelated to resources)

### Implementation Status - Day 4 (2025-10-09)

**✅ Completed:**
1. ✅ Upgraded MCP SDK: 0.3.0-preview.4 → 0.4.0-preview.2
2. ✅ Created KnowledgeResourceMethods with 6 resource methods
3. ✅ Added `[McpServerResourceType]` class attribute
4. ✅ **CRITICAL FIX**: Changed to instance class (not static) - `public class KnowledgeResourceMethods`
5. ✅ **CRITICAL FIX**: Added explicit `UriTemplate` to all `[McpServerResource]` attributes
6. ✅ **CRITICAL FIX**: Changed registration from `.WithResourcesFromAssembly()` to `.WithResources<KnowledgeResourceMethods>()`
7. ✅ Build verified successful (0 errors, 1 pre-existing warning)

**🔄 Testing Phase (Day 4 Evening - BLOCKED):**
- ✅ Qdrant started and running (was not running initially)
- ✅ MCP server builds and starts successfully
- ✅ `resources/list` endpoint handler IS registered (confirmed via test)
- ❌ **Copilot does NOT see/use resources** - only uses Tools
- ❌ Copilot ignores requests to use resources, calls tools instead

**Testing Results:**
When explicitly asking Copilot to use resources:
- ✅ Copilot CAN call MCP Tools: `get_knowledge_base_summary`, `search_all_knowledge_bases`
- ❌ Copilot NEVER calls: `resources/list` or `resources/read`
- ❌ Even when explicitly instructed: "use resources/read endpoint"

**Hypothesis - Why Resources Aren't Working:**
1. Server may not be advertising resource capabilities in `initialize` response
2. VS Code MCP extension may not support resources protocol yet
3. Resource method signatures may be incorrect (async Task<string> vs string)
4. .WithResources<> registration may not be working despite no errors

**🔧 Critical Discovery (Day 4 Evening):**
After investigating the actual MCP SDK samples from the official GitHub repository, discovered the **root cause** of why resources weren't being discovered:

1. **WRONG**: `.WithResourcesFromAssembly()` - This method may not work as expected in 0.4.0-preview.2
2. **CORRECT**: `.WithResources<TResourceType>()` - Explicitly register each resource type class

**EverythingServer Sample Pattern** (from official SDK):
```csharp
// Program.cs
services.AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<AddTool>()
    .WithResources<SimpleResourceType>()  // ← Explicit type registration

// SimpleResourceType.cs
[McpServerResourceType]
public class SimpleResourceType  // ← Instance class, NOT static
{
    [McpServerResource(UriTemplate = "test://template/resource/{id}", Name = "Template Resource")]
    [Description("A template resource with a numeric ID")]
    public static ResourceContents TemplateResource(RequestContext<ReadResourceRequestParams> requestContext, int id)
    {
        // Method implementation
    }
}
```

**Key Differences from Our Failed Attempt:**
1. ❌ Used `static class` → ✅ Should be instance `class`
2. ❌ Used `.WithResourcesFromAssembly()` → ✅ Should use `.WithResources<TType>()`
3. ❌ Omitted `UriTemplate` attribute parameter → ✅ Must include explicit `UriTemplate = "..."`
4. ✅ Static methods are OK - the class should be instance, but methods can be static

### Critical Fixes Applied

**Issue #1: SDK Version**
- Problem: SDK 0.3.0 didn't support Resources
- Fix: Upgraded to 0.4.0-preview.2

**Issue #2: Missing MIME Types**
- Problem: Resources without MIME types fail in MCP inspector (GitHub issue #516)
- Fix: Added explicit `MimeType = "application/json"` to all `[McpServerResource]` attributes

**Issue #3: Assembly Discovery**
- Problem: `.WithResourcesFromAssembly()` was missing
- Fix: Added to Program.cs MCP server configuration

### Next Steps
1. 🔄 **Test with Copilot** - verify resources are discoverable via `resources/list`
2. 🔄 **Verify full document access** - Copilot should read complete Heliograph document
3. ⏳ Update unit tests for new resource methods (if needed)
4. ⏳ Remove old routing implementation if new approach works
5. ⏳ Commit working changes

---

## Technical Implementation Notes

**Key Difference from Previous Implementation:**

**Old Approach (didn't work with MCP clients):**
- Single routing method `ReadResourceAsync(uri)` that parsed URIs
- Not discoverable by `.WithResourcesFromAssembly()`
- SDK 0.3.0 didn't support Resources at all

**New Approach (should work with MCP clients):**
- Individual methods with `[McpServerResource]` attributes
- Method parameters (collectionId, documentId) automatically become URI template variables
- SDK 0.4.0 supports Resources via `.WithResourcesFromAssembly()`
- SDK auto-generates URI templates from method signatures

**Example:**
```csharp
[McpServerResource, Description("Full document content")]
public async Task<string> GetDocument(
    string collectionId,
    string documentId,
    CancellationToken cancellationToken = default)
```

SDK automatically creates:
- URI Template: `resource://knowledge/{collectionId}/document/{documentId}`
- Parameter binding from URI to method parameters
- Resource listing in `resources/list` response

# MCP Resource Testing with Claude Desktop & VS Code Copilot

**Updated:** 2025-10-13
**MCP Specification:** 2024-11-05

## Executive Summary

**Can tests be run from Copilot or Claude Desktop?**

| Feature | Claude Desktop | VS Code Copilot | Claude Code |
|---------|---------------|-----------------|-------------|
| **MCP Resources** | ✅ Full Support | ❌ Not Supported | ✅ Full Support |
| **MCP Tools** | ✅ Full Support | ✅ Full Support | ✅ Full Support |
| **Resources Discovery** | ✅ Automatic | ❌ N/A | ✅ Automatic |
| **Template Discovery** | ✅ Yes | ❌ N/A | ✅ Yes |
| **Can Test Resources?** | ✅ **YES** | ❌ **NO** | ✅ **YES** |

**Short Answer:**
- ✅ **Claude Desktop**: Can test ALL MCP resources features
- ✅ **Claude Code**: Can test ALL MCP resources features (you're using it now!)
- ❌ **VS Code Copilot**: Cannot test MCP resources (client limitation)
- ✅ **All Clients**: Can test MCP tools

---

## Part 1: Testing with Claude Desktop

### Prerequisites

1. **Install Claude Desktop**
   - Download from: https://claude.ai/download
   - Version required: Latest (MCP support added ~November 2024)

2. **Configure MCP Server**

   Edit Claude Desktop config file:
   - **macOS**: `~/Library/Application Support/Claude/claude_desktop_config.json`
   - **Windows**: `%APPDATA%\Claude\claude_desktop_config.json`
   - **Linux**: `~/.config/Claude/claude_desktop_config.json`

   Add Knowledge Manager MCP server:
   ```json
   {
     "mcpServers": {
       "knowledge-manager": {
         "command": "dotnet",
         "args": [
           "run",
           "--project",
           "/home/wayne/repos/ChatComplete/Knowledge.Mcp/Knowledge.Mcp.csproj"
         ],
         "env": {
           "ANTHROPIC_API_KEY": "your-key-here"
         }
       }
     }
   }
   ```

3. **Restart Claude Desktop**
   - Quit completely
   - Reopen application
   - MCP server should auto-connect

### Test 1: Verify MCP Server Connection ✅

**In Claude Desktop chat, type:**
```
Can you see any MCP resources available?
```

**Expected Response:**
```
Yes, I can see several MCP resources from the Knowledge Manager:

Static Resources:
- AI Models Inventory (resource://system/models)
- Knowledge Collections (resource://knowledge/collections)
- System Health (resource://system/health)

Resource Templates:
- Collection Documents (resource://knowledge/{collectionId}/documents)
- Document Content (resource://knowledge/{collectionId}/document/{documentId})
- Collection Statistics (resource://knowledge/{collectionId}/stats)
```

**If Claude says "No resources available":**
- Check config file syntax (use `jq` to validate JSON)
- Verify dotnet is in PATH: `which dotnet`
- Check logs: Look for MCP server startup messages

---

### Test 2: Static Resource Discovery ✅

**Test 2.1: System Health Resource**

**Ask Claude Desktop:**
```
Read the system health resource and tell me the overall status
```

**Expected Behavior:**
- Claude automatically calls `resources/read` with URI: `resource://system/health`
- Displays health status of all components
- Shows vector store, database, and AI provider status

**Example Response:**
```
The system health status shows:
- Overall Status: Healthy
- Qdrant Vector Store: Connected (6333)
- SQLite Database: Operational (123.4 KB)
- OpenAI Provider: Available
- Ollama Provider: Available (3 models)
```

---

**Test 2.2: Knowledge Collections Resource**

**Ask Claude Desktop:**
```
What knowledge collections are available?
```

**Expected Behavior:**
- Claude reads `resource://knowledge/collections`
- Lists all collections with document counts
- Shows collection names and IDs

**Example Response:**
```
Available knowledge collections:
1. docker-docs (5 documents)
2. api-reference (12 documents)
3. python-tutorials (8 documents)

Total: 3 collections with 25 documents
```

---

**Test 2.3: AI Models Resource**

**Ask Claude Desktop:**
```
Show me all available AI models and their usage statistics
```

**Expected Behavior:**
- Claude reads `resource://system/models`
- Displays models from all providers
- Shows usage stats (request counts, tokens, success rates)

**Example Response:**
```
AI Models Inventory:

OpenAI Models:
- gpt-4-turbo: 45 conversations, 95.6% success rate
- gpt-3.5-turbo: 123 conversations, 98.2% success rate

Ollama Models:
- llama3.2:latest: 12 conversations, 100% success rate
- nomic-embed-text: Embedding model

Anthropic Models:
- claude-sonnet-4: 67 conversations, 97.8% success rate
```

---

### Test 3: Parameterized Resource Templates ✅

**Test 3.1: Collection Documents**

**Ask Claude Desktop:**
```
Show me all documents in the docker-docs collection
```

**Expected Behavior:**
- Claude automatically constructs URI: `resource://knowledge/docker-docs/documents`
- Lists all documents with metadata
- Shows file names, chunk counts, upload dates

**Example Response:**
```
Documents in docker-docs collection:

1. docker-compose.yml.md
   - Document ID: doc-abc123
   - Chunks: 15
   - File Size: 24.5 KB
   - Type: text/markdown
   - Uploaded: 2025-10-01

2. dockerfile-best-practices.pdf
   - Document ID: doc-def456
   - Chunks: 28
   - File Size: 156.2 KB
   - Type: application/pdf
   - Uploaded: 2025-10-03

Total: 5 documents
```

---

**Test 3.2: Single Document Content**

**Ask Claude Desktop:**
```
Show me the full content of document doc-abc123 from the docker-docs collection
```

**Expected Behavior:**
- Claude constructs URI: `resource://knowledge/docker-docs/document/doc-abc123`
- Retrieves full document content
- Displays formatted text (respects markdown, code blocks, etc.)

**Example Response:**
```
Document: docker-compose.yml.md
Collection: docker-docs

[Full document content displayed here, properly formatted]

Content Statistics:
- 15 chunks reassembled
- 3,245 characters
- File type: text/markdown
```

---

**Test 3.3: Collection Statistics**

**Ask Claude Desktop:**
```
What are the statistics for the docker-docs collection?
```

**Expected Behavior:**
- Claude reads: `resource://knowledge/docker-docs/stats`
- Shows analytics: document count, chunk count, query count
- Displays usage metrics: conversations, last queried time

**Example Response:**
```
Statistics for docker-docs collection:

Documents: 5
Total Chunks: 87
Vector Store: Qdrant
Total File Size: 452.3 KB

Usage Metrics:
- Total Queries: 23
- Conversations: 12
- Last Queried: 2025-10-13 14:32:15
- Created: 2025-09-15 10:20:00
```

---

### Test 4: Resource Template Discovery ✅

**Ask Claude Desktop:**
```
What resource templates are available for parameterized access?
```

**Expected Behavior:**
- Claude lists templates from `resources/templates/list`
- Shows URI patterns with parameter placeholders
- Explains what each template provides

**Example Response:**
```
Available Resource Templates:

1. Collection Documents
   URI: resource://knowledge/{collectionId}/documents
   Description: List of documents in a specific knowledge collection
   Parameters: collectionId

2. Document Content
   URI: resource://knowledge/{collectionId}/document/{documentId}
   Description: Full content of a specific document
   Parameters: collectionId, documentId

3. Collection Statistics
   URI: resource://knowledge/{collectionId}/stats
   Description: Analytics and usage statistics for a collection
   Parameters: collectionId
```

---

### Test 5: End-to-End Workflow ✅

**Complete user story:**

**You:** "I want to find information about Docker SSL setup in my knowledge base"

**Expected Claude Behavior:**
1. Reads `resource://knowledge/collections` to discover collections
2. Identifies `docker-docs` as relevant collection
3. Reads `resource://knowledge/docker-docs/documents` to list documents
4. Finds "ssl-configuration.md" document
5. Reads full document content
6. Provides answer based on document content

**This demonstrates:**
- ✅ Automatic resource discovery
- ✅ Template URI construction
- ✅ Multi-resource workflow
- ✅ Context-aware responses

---

## Part 2: Testing with VS Code Copilot

### Current Status: MCP Resources NOT Supported ❌

**VS Code Copilot Limitations (as of January 2025):**
- ❌ Does **NOT** support MCP Resources protocol
- ❌ Cannot discover resources via `resources/list`
- ❌ Cannot read resource content via `resources/read`
- ❌ Cannot discover templates via `resources/templates/list`
- ✅ **DOES** support MCP Tools (see below)

**Why?**
VS Code Copilot is focused on **MCP Tools** for code assistance. The resources protocol is not part of their current feature set. This is a client-side limitation, not a server issue.

**What CAN you test in VS Code Copilot?**

### MCP Tools Testing (Supported) ✅

If you configure the Knowledge Manager MCP server in VS Code settings, you can test **MCP Tools**:

**Available MCP Tools:**
1. `search_all_knowledge_bases` - Search across all collections
2. `get_system_health` - Check system status
3. `get_knowledge_base_summary` - Get collections summary
4. `get_popular_models` - View most-used AI models
5. `compare_models` - Compare model performance
6. ... (8 more analytics tools)

**Configuration for VS Code:**

Create `.vscode/settings.json`:
```json
{
  "github.copilot.chat.mcp.servers": {
    "knowledge-manager": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/home/wayne/repos/ChatComplete/Knowledge.Mcp/Knowledge.Mcp.csproj"
      ]
    }
  }
}
```

**Test MCP Tools in Copilot Chat:**
```
@workspace /search What is the Docker SSL configuration?
```

**Expected:** Copilot uses `search_all_knowledge_bases` tool to find relevant content.

**Note:** This tests **MCP Tools**, not **MCP Resources**. They are different protocols.

---

## Part 3: Testing with Claude Code (This Client!)

### You're Already Using It! ✅

**Claude Code** (the client you're using right now) has **full MCP support** including:
- ✅ MCP Resources protocol
- ✅ MCP Tools protocol
- ✅ MCP Prompts protocol

**Tests you can run RIGHT NOW:**

### Test 1: Check MCP Server Connection

**Ask me:**
```
Are there any MCP servers connected? What resources do they provide?
```

**I should respond with:**
- List of connected MCP servers
- Available resources from each server
- Available tools from each server

---

### Test 2: Read System Health

**Ask me:**
```
Read the system health resource from the Knowledge Manager MCP server
```

**Expected:** I'll use the MCP resources protocol to fetch and display health status.

---

### Test 3: List Knowledge Collections

**Ask me:**
```
What knowledge collections are available in the Knowledge Manager?
```

**Expected:** I'll read `resource://knowledge/collections` and show you all collections.

---

### Test 4: Parameterized Resource Access

**Ask me:**
```
Show me all documents in the [collection-name] collection
```

**Expected:** I'll construct the parameterized URI and fetch the document list.

---

## Part 4: Comparison Matrix

### Feature Support Across Clients

| Feature | Claude Desktop | VS Code Copilot | Claude Code | Terminal/CLI |
|---------|----------------|-----------------|-------------|--------------|
| **MCP Resources Protocol** |
| `resources/list` | ✅ Auto | ❌ No | ✅ Auto | ✅ Manual |
| `resources/read` | ✅ Auto | ❌ No | ✅ Auto | ✅ Manual |
| `resources/templates/list` | ✅ Auto | ❌ No | ✅ Auto | ✅ Manual |
| Static resources | ✅ Yes | ❌ No | ✅ Yes | ✅ Yes |
| Parameterized resources | ✅ Yes | ❌ No | ✅ Yes | ✅ Yes |
| **MCP Tools Protocol** |
| Tool discovery | ✅ Auto | ✅ Auto | ✅ Auto | ✅ Manual |
| Tool execution | ✅ Auto | ✅ Auto | ✅ Auto | ✅ Manual |
| **Testing Experience** |
| Natural language testing | ✅ Yes | ⚠️ Tools Only | ✅ Yes | ❌ No |
| Automatic URI construction | ✅ Yes | ❌ No | ✅ Yes | ❌ No |
| Resource content display | ✅ Formatted | ❌ No | ✅ Formatted | ⚠️ Raw JSON |
| Error handling visibility | ✅ Clear | ⚠️ Limited | ✅ Clear | ✅ Detailed |

**Legend:**
- ✅ **Full support** - Feature works as expected
- ⚠️ **Partial support** - Limited functionality
- ❌ **No support** - Feature not available
- 🔧 **Manual** - Requires manual commands

---

## Part 5: Practical Test Scenarios

### Scenario 1: New User Onboarding (Claude Desktop)

**Goal:** Verify complete resource discovery workflow

**Steps:**
1. Open Claude Desktop
2. Start new conversation
3. Type: "Show me what resources are available from the Knowledge Manager"
4. Type: "What's the system health status?"
5. Type: "List all knowledge collections"
6. Type: "Show me documents in [any collection]"

**Expected:** All steps complete successfully with formatted responses.

**Time:** ~2 minutes

---

### Scenario 2: Document Search (Claude Desktop)

**Goal:** Test parameterized resource access with real data

**Steps:**
1. Type: "What knowledge collections exist?"
2. Note a collection ID from response
3. Type: "Show me all documents in [collection-id]"
4. Note a document ID from response
5. Type: "Read document [document-id] from [collection-id]"

**Expected:** Full document content displayed with proper formatting.

**Time:** ~3 minutes

---

### Scenario 3: Analytics Review (Claude Desktop)

**Goal:** Verify all static resources work

**Steps:**
1. Type: "What AI models are available?"
2. Type: "Show me system health"
3. Type: "Get statistics for [collection-id]"

**Expected:** All analytics data displayed correctly.

**Time:** ~2 minutes

---

### Scenario 4: Error Handling (Claude Desktop)

**Goal:** Verify graceful error handling

**Steps:**
1. Type: "Show me documents in collection 'non-existent-collection'"
2. Type: "Read document 'invalid-doc-id' from 'docker-docs'"

**Expected:** Clear error messages, no crashes, helpful suggestions.

**Time:** ~1 minute

---

### Scenario 5: Template Discovery (Claude Desktop)

**Goal:** Verify resources/templates/list endpoint

**Steps:**
1. Type: "What resource templates are available?"
2. Verify 3 templates are listed
3. Verify templates have parameter placeholders
4. Try using one of the templates with real parameters

**Expected:** Templates listed, then successfully used.

**Time:** ~2 minutes

---

## Part 6: Troubleshooting

### Issue: Claude Desktop Doesn't See Resources

**Symptoms:**
- Claude says "No resources available"
- Resources not showing in UI

**Solutions:**
1. **Check config file syntax:**
   ```bash
   jq . ~/.config/Claude/claude_desktop_config.json
   # Should output valid JSON, no errors
   ```

2. **Verify dotnet is in PATH:**
   ```bash
   which dotnet
   # Should show: /usr/bin/dotnet or similar
   ```

3. **Test server manually:**
   ```bash
   dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj
   # Should start without errors
   ```

4. **Check Claude Desktop logs:**
   - macOS: `~/Library/Logs/Claude/`
   - Windows: `%APPDATA%\Claude\logs\`
   - Linux: `~/.config/Claude/logs/`

5. **Restart Claude Desktop completely:**
   - Quit (not just close window)
   - Kill any background processes
   - Reopen

---

### Issue: Resources Show But Can't Be Read

**Symptoms:**
- Resources appear in list
- Error when trying to read content

**Solutions:**
1. **Check database exists:**
   ```bash
   ls -lh Knowledge.Api/bin/Debug/net8.0/linux-x64/data/knowledge.db
   ```

2. **Verify Qdrant is running:**
   ```bash
   docker ps | grep qdrant
   ```

3. **Test resource manually:**
   ```bash
   ./test-mcp-resources-clean.sh
   ```

4. **Check server logs for errors**

---

### Issue: VS Code Copilot Can't Access Resources

**This is expected!**

VS Code Copilot does not support the MCP Resources protocol. You can only test MCP Tools in Copilot.

**Alternative:** Use Claude Desktop or Claude Code for resources testing.

---

## Part 7: Test Results Template

Use this template to document your testing:

```markdown
## MCP Resources Test Results

**Date:** YYYY-MM-DD
**Tester:** [Name]
**Client:** Claude Desktop / Claude Code / VS Code Copilot
**Server Version:** [Git commit SHA]

### Static Resources
- [ ] System Health - PASS/FAIL
- [ ] Knowledge Collections - PASS/FAIL
- [ ] AI Models Inventory - PASS/FAIL

### Resource Templates
- [ ] Template Discovery - PASS/FAIL
- [ ] Collection Documents - PASS/FAIL
- [ ] Document Content - PASS/FAIL
- [ ] Collection Statistics - PASS/FAIL

### Error Handling
- [ ] Invalid Collection ID - PASS/FAIL
- [ ] Invalid Document ID - PASS/FAIL

### Notes
[Any observations or issues]
```

---

## Quick Reference

### Can I Test Resources?

**Using Claude Desktop?** → ✅ **YES** - Full support, all tests work
**Using Claude Code?** → ✅ **YES** - Full support, all tests work
**Using VS Code Copilot?** → ❌ **NO** - Resources not supported (Tools only)
**Using Terminal/CLI?** → ✅ **YES** - Manual JSON-RPC testing

### Best Client for Testing

**Recommended:** **Claude Desktop** or **Claude Code**
- Natural language testing
- Automatic URI construction
- Formatted output
- Error handling
- Complete MCP protocol support

### Quick Test Command (Any Client)

**Claude Desktop / Claude Code:**
```
Show me all available MCP resources and read the system health
```

**Terminal:**
```bash
./run-all-resource-tests.sh
```

---

## Summary

✅ **Claude Desktop** - Best for interactive resource testing
✅ **Claude Code** - Best for development and documentation
❌ **VS Code Copilot** - Cannot test resources (tools only)
✅ **Terminal/CLI** - Best for automated testing

**You can test MCP resources right now using Claude Code (this client)!** Just ask me to read any resource and I'll use the MCP protocol to fetch it.

---

**For more information:**
- [MCP_RESOURCE_TEMPLATES_TEST_PLAN.md](./MCP_RESOURCE_TEMPLATES_TEST_PLAN.md) - Complete test procedures
- [MCP_PHASE_2C_COMPLETION.md](./MCP_PHASE_2C_COMPLETION.md) - Implementation details
- [MCP Specification](https://spec.modelcontextprotocol.io/) - Official protocol docs

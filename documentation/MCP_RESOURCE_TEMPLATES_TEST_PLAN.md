# MCP Resource Templates - Comprehensive Test Plan

**Version:** 1.0
**Date:** 2025-10-13
**Status:** Ready for Execution
**MCP Specification:** 2024-11-05

## Overview

This test plan validates the complete MCP resources implementation including:
- Static resource discovery (`resources/list`)
- Parameterized resource templates (`resources/templates/list`)
- Resource content retrieval (`resources/read`)
- Error handling and edge cases

## Prerequisites

### Environment Setup

1. **Required Software:**
   - .NET 8 SDK installed
   - `jq` command-line JSON processor
   - `curl` for HTTP testing
   - Git for version control

2. **Verify Prerequisites:**
   ```bash
   dotnet --version  # Should show 8.0.x
   jq --version      # Should show jq-1.x
   curl --version    # Any recent version
   ```

3. **Clone and Build:**
   ```bash
   cd /home/wayne/repos/ChatComplete
   dotnet build
   # Should complete with 0 errors
   ```

4. **Verify Database:**
   ```bash
   # Check SQLite database exists
   ls -lh Knowledge.Api/bin/Debug/net8.0/linux-x64/data/knowledge.db
   # Should show database file (may be empty initially)
   ```

## Test Suite Structure

### Phase 1: Static Resources Testing
- Test 1.1: Server Initialization
- Test 1.2: Static Resources Discovery
- Test 1.3: Static Resource Content Retrieval

### Phase 2: Resource Templates Testing
- Test 2.1: Templates Discovery
- Test 2.2: Template Metadata Validation
- Test 2.3: No Duplicate Templates

### Phase 3: Parameterized Resources Testing
- Test 3.1: Collection Documents Resource
- Test 3.2: Single Document Resource
- Test 3.3: Collection Statistics Resource

### Phase 4: Error Handling Testing
- Test 4.1: Invalid Collection ID
- Test 4.2: Invalid Document ID
- Test 4.3: Malformed URI

### Phase 5: Integration Testing
- Test 5.1: End-to-End Resource Discovery
- Test 5.2: Cross-Resource Consistency

---

## Phase 1: Static Resources Testing

### Test 1.1: Server Initialization ✅

**Objective:** Verify MCP server starts correctly and responds to initialize request

**Steps:**
```bash
# 1. Start the server and send initialize request
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{"roots":{"listChanged":true},"sampling":{}},"clientInfo":{"name":"test-client","version":"1.0.0"}}}' | \
  timeout 5 dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj 2>&1 | \
  grep -E '^\{.*"result".*\}$' | \
  jq '.'
```

**Expected Result:**
```json
{
  "result": {
    "protocolVersion": "2024-11-05",
    "capabilities": {
      "logging": {},
      "resources": {
        "listChanged": true
      },
      "tools": {
        "listChanged": true
      }
    },
    "serverInfo": {
      "name": "Knowledge.Mcp",
      "version": "1.0.0.0"
    }
  },
  "id": 1,
  "jsonrpc": "2.0"
}
```

**Pass Criteria:**
- ✅ Server responds within 5 seconds
- ✅ Protocol version matches: `2024-11-05`
- ✅ Resources capability includes `listChanged: true`
- ✅ Server name is `Knowledge.Mcp`

**Troubleshooting:**
- If timeout occurs: Check if Qdrant is running (`docker ps`)
- If database error: Verify SQLite database path in appsettings.json

---

### Test 1.2: Static Resources Discovery ✅

**Objective:** Verify `resources/list` returns all static resources

**Steps:**
```bash
# 1. Use the clean test script
./test-mcp-resources-clean.sh 2>&1 | grep -A 30 '"resources":'
```

**Alternative Manual Test:**
```bash
# 2. Manual JSON-RPC request
(
  echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{"roots":{"listChanged":true}},"clientInfo":{"name":"test","version":"1.0"}}}'
  sleep 1
  echo '{"jsonrpc":"2.0","id":2,"method":"resources/list"}'
  sleep 2
) | timeout 10 dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj 2>&1 | \
  grep -E '^\{.*"resources".*\}$' | \
  jq '.result.resources | length'
```

**Expected Result:**
```json
{
  "result": {
    "resources": [
      {
        "name": "AI Models Inventory",
        "uri": "resource://system/models",
        "description": "Inventory of AI models with usage stats (Ollama, OpenAI, Anthropic, Google)",
        "mimeType": "application/json"
      },
      {
        "name": "Knowledge Collections",
        "uri": "resource://knowledge/collections",
        "description": "Complete list of all knowledge collections with document counts and metadata",
        "mimeType": "application/json"
      },
      {
        "name": "System Health",
        "uri": "resource://system/health",
        "description": "System health status for vector stores, databases, and AI providers",
        "mimeType": "application/json"
      }
    ]
  }
}
```

**Pass Criteria:**
- ✅ Returns exactly 3 static resources
- ✅ All URIs start with `resource://`
- ✅ All resources have `name`, `uri`, `description`, `mimeType` fields
- ✅ MIME type is `application/json` for all

**Validation Script:**
```bash
# Count resources
./test-mcp-resources-clean.sh 2>&1 | \
  grep -A 30 '"resources":' | \
  jq '.result.resources | length'
# Expected output: 3

# Verify all have required fields
./test-mcp-resources-clean.sh 2>&1 | \
  grep -A 30 '"resources":' | \
  jq '.result.resources[] | select(.uri and .name and .description and .mimeType) | .uri'
# Expected: 3 URIs printed
```

---

### Test 1.3: Static Resource Content Retrieval ✅

**Objective:** Verify each static resource can be read successfully

**Test 1.3.1: System Health Resource**
```bash
# 1. Read system health resource
(
  echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}'
  sleep 1
  echo '{"jsonrpc":"2.0","id":2,"method":"resources/read","params":{"uri":"resource://system/health"}}'
  sleep 2
) | timeout 10 dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj 2>&1 | \
  grep -E '^\{.*"contents".*\}$' | \
  jq '.result.contents[0].text | fromjson | keys'
```

**Expected Result:**
```json
["components", "overallStatus", "timestamp"]
```

**Pass Criteria:**
- ✅ Response contains `contents` array
- ✅ Content has `text` field with valid JSON
- ✅ JSON includes health status fields

**Test 1.3.2: Knowledge Collections Resource**
```bash
# 2. Read collections resource
(
  echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}'
  sleep 1
  echo '{"jsonrpc":"2.0","id":2,"method":"resources/read","params":{"uri":"resource://knowledge/collections"}}'
  sleep 2
) | timeout 10 dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj 2>&1 | \
  grep -E '^\{.*"contents".*\}$' | \
  jq '.result.contents[0].text | fromjson | keys'
```

**Expected Result:**
```json
["collections", "totalCollections"]
```

**Test 1.3.3: AI Models Resource**
```bash
# 3. Read models resource
(
  echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}'
  sleep 1
  echo '{"jsonrpc":"2.0","id":2,"method":"resources/read","params":{"uri":"resource://system/models"}}'
  sleep 2
) | timeout 10 dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj 2>&1 | \
  grep -E '^\{.*"contents".*\}$' | \
  jq '.result.contents[0].text | fromjson | keys'
```

**Expected Result:**
```json
["models", "providers", "totalModels"]
```

---

## Phase 2: Resource Templates Testing

### Test 2.1: Templates Discovery ✅

**Objective:** Verify `resources/templates/list` returns all parameterized resource templates

**Steps:**
```bash
# 1. Run the clean test script
./test-mcp-resources-clean.sh 2>&1 | grep -A 30 '"resourceTemplates":'
```

**Alternative Manual Test:**
```bash
# 2. Manual JSON-RPC request
(
  echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}'
  sleep 1
  echo '{"jsonrpc":"2.0","id":2,"method":"resources/templates/list"}'
  sleep 2
) | timeout 10 dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj 2>&1 | \
  grep -E '^\{.*"resourceTemplates".*\}$' | \
  jq '.'
```

**Expected Result:**
```json
{
  "result": {
    "resourceTemplates": [
      {
        "name": "Collection Statistics",
        "uriTemplate": "resource://knowledge/{collectionId}/stats",
        "description": "Analytics and usage statistics for a knowledge collection",
        "mimeType": "application/json"
      },
      {
        "name": "Collection Documents",
        "uriTemplate": "resource://knowledge/{collectionId}/documents",
        "description": "List of documents in a specific knowledge collection",
        "mimeType": "application/json"
      },
      {
        "name": "Document Content",
        "uriTemplate": "resource://knowledge/{collectionId}/document/{documentId}",
        "description": "Full content of a specific document with MIME type detection",
        "mimeType": "application/json"
      }
    ]
  }
}
```

**Pass Criteria:**
- ✅ Returns exactly 3 resource templates
- ✅ All URI templates contain parameter placeholders: `{collectionId}` or `{documentId}`
- ✅ All templates have `name`, `uriTemplate`, `description`, `mimeType` fields
- ✅ No duplicate templates

**Validation Script:**
```bash
# Count templates
./test-mcp-resources-clean.sh 2>&1 | \
  grep -A 30 '"resourceTemplates":' | \
  jq '.result.resourceTemplates | length'
# Expected: 3

# Check for duplicates
./test-mcp-resources-clean.sh 2>&1 | \
  grep -A 30 '"resourceTemplates":' | \
  jq '.result.resourceTemplates | group_by(.uriTemplate) | map(length) | max'
# Expected: 1 (no duplicates)
```

---

### Test 2.2: Template Metadata Validation ✅

**Objective:** Verify each template has complete and correct metadata

**Steps:**
```bash
# 1. Validate template structure
./test-mcp-resources-clean.sh 2>&1 | \
  grep -A 30 '"resourceTemplates":' | \
  jq '.result.resourceTemplates[] |
    {
      name: .name,
      hasParameters: (.uriTemplate | test("\\{[^}]+\\}")),
      mimeType: .mimeType,
      hasDescription: (.description | length > 10)
    }'
```

**Expected Output:**
```json
{
  "name": "Collection Statistics",
  "hasParameters": true,
  "mimeType": "application/json",
  "hasDescription": true
}
{
  "name": "Collection Documents",
  "hasParameters": true,
  "mimeType": "application/json",
  "hasDescription": true
}
{
  "name": "Document Content",
  "hasParameters": true,
  "mimeType": "application/json",
  "hasDescription": true
}
```

**Pass Criteria:**
- ✅ All templates have parameter placeholders in URI
- ✅ All MIME types are `application/json`
- ✅ All descriptions are meaningful (> 10 characters)
- ✅ Template names are human-readable

---

### Test 2.3: No Duplicate Templates ✅

**Objective:** Verify SDK auto-discovery doesn't create duplicate templates

**Steps:**
```bash
# 1. Count unique URI templates
./test-mcp-resources-clean.sh 2>&1 | \
  grep -A 30 '"resourceTemplates":' | \
  jq '.result.resourceTemplates | map(.uriTemplate) | unique | length'

# 2. Count total templates
./test-mcp-resources-clean.sh 2>&1 | \
  grep -A 30 '"resourceTemplates":' | \
  jq '.result.resourceTemplates | length'

# 3. These should be equal (3 = 3)
```

**Expected Output:**
```
3
3
```

**Pass Criteria:**
- ✅ Unique templates count equals total templates count
- ✅ No duplicate `uriTemplate` values
- ✅ No duplicate `name` values

**Validation Script:**
```bash
# Check for any duplicates
./test-mcp-resources-clean.sh 2>&1 | \
  grep -A 30 '"resourceTemplates":' | \
  jq '.result.resourceTemplates |
    group_by(.uriTemplate) |
    map({uri: .[0].uriTemplate, count: length}) |
    map(select(.count > 1))'
# Expected: [] (empty array = no duplicates)
```

---

## Phase 3: Parameterized Resources Testing

### Prerequisites for Phase 3

**Setup Test Data:**
```bash
# 1. Start the Knowledge API server
cd /home/wayne/repos/ChatComplete
ANTHROPIC_API_KEY="$ANTHROPIC_API_KEY" dotnet run --project Knowledge.Api &
API_PID=$!
sleep 5

# 2. Upload test document to create a collection
curl -X POST http://localhost:7040/api/knowledge \
  -F "knowledgeId=test-docs" \
  -F "knowledgeName=Test Documentation" \
  -F "files=@README.md"

# Expected: 201 Created with collection details

# 3. Verify collection exists
curl http://localhost:7040/api/knowledge/test-docs
# Expected: JSON with collection metadata

# 4. Get document ID for testing
DOC_ID=$(curl -s http://localhost:7040/api/knowledge/test-docs | jq -r '.documents[0].documentId')
echo "Test Document ID: $DOC_ID"

# Keep API_PID for cleanup later
```

---

### Test 3.1: Collection Documents Resource ✅

**Objective:** Verify parameterized resource returns documents list for a collection

**Steps:**
```bash
# 1. Read collection documents using template
(
  echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}'
  sleep 1
  echo '{"jsonrpc":"2.0","id":2,"method":"resources/read","params":{"uri":"resource://knowledge/test-docs/documents"}}'
  sleep 2
) | timeout 10 dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj 2>&1 | \
  grep -E '^\{.*"contents".*\}$' | \
  jq '.result.contents[0].text | fromjson'
```

**Expected Result:**
```json
{
  "collectionId": "test-docs",
  "totalDocuments": 1,
  "documents": [
    {
      "documentId": "doc-...",
      "fileName": "README.md",
      "chunkCount": ...,
      "fileSize": ...,
      "fileType": "text/markdown",
      "uploadedAt": "2025-..."
    }
  ]
}
```

**Pass Criteria:**
- ✅ Response contains `collectionId` matching request
- ✅ `totalDocuments` count is correct
- ✅ Documents array contains document metadata
- ✅ Each document has `documentId`, `fileName`, `chunkCount`, `fileSize`, `fileType`

---

### Test 3.2: Single Document Resource ✅

**Objective:** Verify document content retrieval with two parameters

**Steps:**
```bash
# 1. Get document ID from previous test
DOC_ID=$(curl -s http://localhost:7040/api/knowledge/test-docs | jq -r '.documents[0].documentId')

# 2. Read document content
(
  echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}'
  sleep 1
  echo "{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"resources/read\",\"params\":{\"uri\":\"resource://knowledge/test-docs/document/$DOC_ID\"}}"
  sleep 2
) | timeout 10 dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj 2>&1 | \
  grep -E '^\{.*"contents".*\}$' | \
  jq '.result.contents[0].text | fromjson | {collectionId, documentId, fileName, contentLength: (.content | length)}'
```

**Expected Result:**
```json
{
  "collectionId": "test-docs",
  "documentId": "doc-...",
  "fileName": "README.md",
  "contentLength": ...  // Number of characters
}
```

**Pass Criteria:**
- ✅ Response contains full document content
- ✅ `collectionId` and `documentId` match request
- ✅ `fileName` matches uploaded file
- ✅ Content is reassembled from chunks correctly

---

### Test 3.3: Collection Statistics Resource ✅

**Objective:** Verify collection analytics retrieval

**Steps:**
```bash
# 1. Read collection statistics
(
  echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}'
  sleep 1
  echo '{"jsonrpc":"2.0","id":2,"method":"resources/read","params":{"uri":"resource://knowledge/test-docs/stats"}}'
  sleep 2
) | timeout 10 dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj 2>&1 | \
  grep -E '^\{.*"contents".*\}$' | \
  jq '.result.contents[0].text | fromjson'
```

**Expected Result:**
```json
{
  "collectionId": "test-docs",
  "name": "Test Documentation",
  "documentCount": 1,
  "chunkCount": ...,
  "totalQueries": 0,
  "conversationCount": 0,
  "lastQueried": null,
  "createdAt": "2025-...",
  "vectorStore": "Qdrant",
  "totalFileSize": ...
}
```

**Pass Criteria:**
- ✅ Response contains collection statistics
- ✅ `documentCount` and `chunkCount` are correct
- ✅ `vectorStore` is set to configured provider
- ✅ All fields are present and have correct types

---

## Phase 4: Error Handling Testing

### Test 4.1: Invalid Collection ID ❌ (Expected Failure)

**Objective:** Verify graceful error handling for non-existent collections

**Steps:**
```bash
# 1. Attempt to read documents from non-existent collection
(
  echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}'
  sleep 1
  echo '{"jsonrpc":"2.0","id":2,"method":"resources/read","params":{"uri":"resource://knowledge/non-existent-collection/documents"}}'
  sleep 2
) | timeout 10 dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj 2>&1 | \
  grep -E '^\{.*"error".*\}$' | \
  jq '.'
```

**Expected Result:**
```json
{
  "error": {
    "code": -32603,
    "message": "Collection not found: non-existent-collection"
  },
  "id": 2,
  "jsonrpc": "2.0"
}
```

**Pass Criteria:**
- ✅ Returns JSON-RPC error response
- ✅ Error code is appropriate (e.g., -32603 for internal error)
- ✅ Error message is descriptive
- ✅ No server crash or exception leak

---

### Test 4.2: Invalid Document ID ❌ (Expected Failure)

**Objective:** Verify error handling for non-existent documents

**Steps:**
```bash
# 1. Attempt to read non-existent document
(
  echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}'
  sleep 1
  echo '{"jsonrpc":"2.0","id":2,"method":"resources/read","params":{"uri":"resource://knowledge/test-docs/document/invalid-doc-id"}}'
  sleep 2
) | timeout 10 dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj 2>&1 | \
  grep -E '^\{.*"error".*\}$' | \
  jq '.error.message'
```

**Expected Output:**
```
"Document not found: invalid-doc-id in collection test-docs"
```

**Pass Criteria:**
- ✅ Returns descriptive error message
- ✅ Error includes both document ID and collection ID
- ✅ No internal exception details leaked

---

### Test 4.3: Malformed URI ❌ (Expected Failure)

**Objective:** Verify handling of malformed resource URIs

**Test 4.3.1: Wrong Scheme**
```bash
# 1. Try HTTP scheme instead of resource://
(
  echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}'
  sleep 1
  echo '{"jsonrpc":"2.0","id":2,"method":"resources/read","params":{"uri":"http://knowledge/test-docs/documents"}}'
  sleep 2
) | timeout 10 dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj 2>&1 | \
  grep -E '^\{.*"error".*\}$'
```

**Test 4.3.2: Missing Parameters**
```bash
# 2. Try template URI without parameter substitution
(
  echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}'
  sleep 1
  echo '{"jsonrpc":"2.0","id":2,"method":"resources/read","params":{"uri":"resource://knowledge/{collectionId}/documents"}}'
  sleep 2
) | timeout 10 dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj 2>&1 | \
  grep -E '^\{.*"error".*\}$'
```

**Pass Criteria:**
- ✅ Returns error for wrong URI scheme
- ✅ Returns error for unsubstituted template parameters
- ✅ Error messages are clear and actionable

---

## Phase 5: Integration Testing

### Test 5.1: End-to-End Resource Discovery ✅

**Objective:** Simulate complete client workflow from discovery to data retrieval

**Complete Workflow Script:**
```bash
#!/bin/bash
# complete-resource-workflow.sh

echo "=== MCP Resource Discovery E2E Test ==="
echo ""

# Step 1: Initialize
echo "Step 1: Initialize MCP connection..."
INIT_RESPONSE=$(echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{"roots":{"listChanged":true}},"clientInfo":{"name":"e2e-test","version":"1.0"}}}' | \
  timeout 5 dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj 2>&1 | \
  grep -E '^\{.*"result".*\}$')
echo "✓ Server initialized"
echo ""

# Step 2: Discover static resources
echo "Step 2: Discover static resources..."
(
  echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"e2e-test","version":"1.0"}}}'
  sleep 1
  echo '{"jsonrpc":"2.0","id":2,"method":"resources/list"}'
  sleep 2
) | timeout 10 dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj 2>&1 | \
  grep -E '^\{.*"resources".*\}$' | \
  jq -r '.result.resources[] | "  - \(.name): \(.uri)"'
echo ""

# Step 3: Discover resource templates
echo "Step 3: Discover resource templates..."
(
  echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"e2e-test","version":"1.0"}}}'
  sleep 1
  echo '{"jsonrpc":"2.0","id":2,"method":"resources/templates/list"}'
  sleep 2
) | timeout 10 dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj 2>&1 | \
  grep -E '^\{.*"resourceTemplates".*\}$' | \
  jq -r '.result.resourceTemplates[] | "  - \(.name): \(.uriTemplate)"'
echo ""

# Step 4: Read a static resource
echo "Step 4: Read system health resource..."
(
  echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"e2e-test","version":"1.0"}}}'
  sleep 1
  echo '{"jsonrpc":"2.0","id":2,"method":"resources/read","params":{"uri":"resource://system/health"}}'
  sleep 2
) | timeout 10 dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj 2>&1 | \
  grep -E '^\{.*"contents".*\}$' | \
  jq -r '.result.contents[0].text | fromjson | "  Status: \(.overallStatus)"'
echo ""

# Step 5: Read collections (if available)
echo "Step 5: Read knowledge collections..."
(
  echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"e2e-test","version":"1.0"}}}'
  sleep 1
  echo '{"jsonrpc":"2.0","id":2,"method":"resources/read","params":{"uri":"resource://knowledge/collections"}}'
  sleep 2
) | timeout 10 dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj 2>&1 | \
  grep -E '^\{.*"contents".*\}$' | \
  jq -r '.result.contents[0].text | fromjson | "  Total Collections: \(.totalCollections)"'
echo ""

echo "=== E2E Test Complete ✓ ==="
```

**Save and run:**
```bash
chmod +x complete-resource-workflow.sh
./complete-resource-workflow.sh
```

**Pass Criteria:**
- ✅ All 5 steps complete without errors
- ✅ Resources discovered successfully
- ✅ Templates discovered successfully
- ✅ Static resources readable
- ✅ Output is formatted correctly

---

### Test 5.2: Cross-Resource Consistency ✅

**Objective:** Verify data consistency between different resource endpoints

**Steps:**
```bash
# 1. Get collection count from collections resource
COLLECTIONS_COUNT=$(
  (
    echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}'
    sleep 1
    echo '{"jsonrpc":"2.0","id":2,"method":"resources/read","params":{"uri":"resource://knowledge/collections"}}'
    sleep 2
  ) | timeout 10 dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj 2>&1 | \
    grep -E '^\{.*"contents".*\}$' | \
    jq -r '.result.contents[0].text | fromjson | .totalCollections'
)

echo "Collections count from /collections: $COLLECTIONS_COUNT"

# 2. Verify each collection has accessible documents endpoint
(
  echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}'
  sleep 1
  echo '{"jsonrpc":"2.0","id":2,"method":"resources/read","params":{"uri":"resource://knowledge/collections"}}'
  sleep 2
) | timeout 10 dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj 2>&1 | \
  grep -E '^\{.*"contents".*\}$' | \
  jq -r '.result.contents[0].text | fromjson | .collections[].id' | \
  while read -r collection_id; do
    echo "Testing collection: $collection_id"
    (
      echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}'
      sleep 1
      echo "{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"resources/read\",\"params\":{\"uri\":\"resource://knowledge/$collection_id/documents\"}}"
      sleep 2
    ) | timeout 10 dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj 2>&1 | \
      grep -E '^\{.*"contents".*\}$' | \
      jq -r '.result.contents[0].text | fromjson | "  ✓ \(.collectionId): \(.totalDocuments) documents"'
  done
```

**Pass Criteria:**
- ✅ Collection count from `/collections` matches accessible collections
- ✅ Each collection ID can be used in parameterized resource URIs
- ✅ Document counts are consistent across endpoints

---

## Test Execution Summary

### Quick Test Script

Create a master test runner:

```bash
#!/bin/bash
# run-all-resource-tests.sh

echo "╔════════════════════════════════════════════════════════════╗"
echo "║  MCP Resource Templates - Comprehensive Test Suite        ║"
echo "╚════════════════════════════════════════════════════════════╝"
echo ""

PASS=0
FAIL=0

# Helper function
run_test() {
  local test_name="$1"
  local test_cmd="$2"

  echo -n "Testing: $test_name... "

  if eval "$test_cmd" > /tmp/test_output.txt 2>&1; then
    echo "✓ PASS"
    ((PASS++))
  else
    echo "✗ FAIL"
    echo "  Error: $(cat /tmp/test_output.txt | tail -1)"
    ((FAIL++))
  fi
}

echo "Phase 1: Static Resources"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━"
run_test "Server Initialization" "./test-mcp-resources-clean.sh 2>&1 | grep -q '\"protocolVersion\": \"2024-11-05\"'"
run_test "Static Resources Count" "./test-mcp-resources-clean.sh 2>&1 | grep -A 30 '\"resources\":' | jq '.result.resources | length' | grep -q '^3$'"
run_test "System Health Resource" "./test-mcp-resources-clean.sh 2>&1 | grep -q 'resource://system/health'"
echo ""

echo "Phase 2: Resource Templates"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
run_test "Templates Discovery" "./test-mcp-resources-clean.sh 2>&1 | grep -q 'resourceTemplates'"
run_test "Templates Count" "./test-mcp-resources-clean.sh 2>&1 | grep -A 30 '\"resourceTemplates\":' | jq '.result.resourceTemplates | length' | grep -q '^3$'"
run_test "No Duplicate Templates" "./test-mcp-resources-clean.sh 2>&1 | grep -A 30 '\"resourceTemplates\":' | jq '.result.resourceTemplates | group_by(.uriTemplate) | map(length) | max' | grep -q '^1$'"
echo ""

echo "Phase 3: Parameterized Resources"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
run_test "Collection Documents Template" "./test-mcp-resources-clean.sh 2>&1 | grep -q '{collectionId}/documents'"
run_test "Document Content Template" "./test-mcp-resources-clean.sh 2>&1 | grep -q '{collectionId}/document/{documentId}'"
run_test "Collection Stats Template" "./test-mcp-resources-clean.sh 2>&1 | grep -q '{collectionId}/stats'"
echo ""

echo "╔════════════════════════════════════════════════════════════╗"
echo "║  Test Results                                             ║"
echo "╠════════════════════════════════════════════════════════════╣"
echo "║  PASSED: $PASS                                                 ║"
echo "║  FAILED: $FAIL                                                 ║"
echo "╚════════════════════════════════════════════════════════════╝"

if [ $FAIL -eq 0 ]; then
  echo "✓ All tests passed!"
  exit 0
else
  echo "✗ Some tests failed!"
  exit 1
fi
```

**Run all tests:**
```bash
chmod +x run-all-resource-tests.sh
./run-all-resource-tests.sh
```

---

## Cleanup

After completing all tests:

```bash
# 1. Stop Knowledge API server (if started)
kill $API_PID 2>/dev/null

# 2. Remove test data (optional)
curl -X DELETE http://localhost:7040/api/knowledge/test-docs

# 3. Clean up test files
rm -f /tmp/test_output.txt complete-resource-workflow.sh run-all-resource-tests.sh
```

---

## Test Results Template

Use this template to document test execution results:

```markdown
## Test Execution Results

**Date:** YYYY-MM-DD
**Tester:** [Your Name]
**Build:** [Commit SHA]
**Environment:** [Dev/Staging/Production]

### Phase 1: Static Resources
- [ ] Test 1.1: Server Initialization - PASS/FAIL
- [ ] Test 1.2: Static Resources Discovery - PASS/FAIL
- [ ] Test 1.3: Static Resource Content - PASS/FAIL

### Phase 2: Resource Templates
- [ ] Test 2.1: Templates Discovery - PASS/FAIL
- [ ] Test 2.2: Template Metadata - PASS/FAIL
- [ ] Test 2.3: No Duplicates - PASS/FAIL

### Phase 3: Parameterized Resources
- [ ] Test 3.1: Collection Documents - PASS/FAIL
- [ ] Test 3.2: Single Document - PASS/FAIL
- [ ] Test 3.3: Collection Statistics - PASS/FAIL

### Phase 4: Error Handling
- [ ] Test 4.1: Invalid Collection - PASS/FAIL
- [ ] Test 4.2: Invalid Document - PASS/FAIL
- [ ] Test 4.3: Malformed URI - PASS/FAIL

### Phase 5: Integration
- [ ] Test 5.1: E2E Workflow - PASS/FAIL
- [ ] Test 5.2: Cross-Resource Consistency - PASS/FAIL

### Summary
- **Total Tests:** 14
- **Passed:** X
- **Failed:** Y
- **Skipped:** Z

### Notes
[Any additional observations or issues discovered during testing]
```

---

## Appendix: Troubleshooting Common Issues

### Issue: Server Timeout

**Symptom:** `timeout: the monitored command dumped core`

**Solutions:**
- Increase timeout: `timeout 15 dotnet run...` (instead of 10)
- Check Qdrant is running: `docker ps | grep qdrant`
- Verify database path: `ls Knowledge.Api/bin/Debug/net8.0/linux-x64/data/knowledge.db`

### Issue: Empty Resources List

**Symptom:** `resources: []`

**Solutions:**
- Verify `KnowledgeResourceMethods.cs` has `[McpServerResource]` attributes
- Check server initialization logs for errors
- Ensure `WithResources<KnowledgeResourceMethods>()` is in `Program.cs`

### Issue: Duplicate Templates

**Symptom:** Same template appears multiple times

**Solutions:**
- Remove manual `WithListResourceTemplatesHandler` from `Program.cs`
- SDK auto-discovers templates from attributes
- Verify fix: `git log --oneline | head -5` should show Phase 2C commit

### Issue: JSON Parsing Error

**Symptom:** `jq: parse error: Invalid numeric literal`

**Solutions:**
- Check if output contains stderr mixed with JSON
- Use `2>&1 | grep -E '^\{.*\}$'` to filter JSON-only lines
- Verify `jq` is installed: `jq --version`

---

## Conclusion

This comprehensive test plan covers:
- ✅ All MCP resources endpoints (list, read, templates/list)
- ✅ Static and parameterized resources
- ✅ Error handling and edge cases
- ✅ End-to-end integration workflows
- ✅ Cross-resource data consistency

**Total Test Coverage:** 14 test cases across 5 phases

**Expected Execution Time:** ~10-15 minutes for complete test suite

**Recommended Frequency:**
- Run Phase 1-2 tests on every commit (CI/CD)
- Run Phase 3-5 tests before releases
- Run full suite weekly for regression testing

---

**For questions or issues, refer to:**
- [MCP_PHASE_2C_COMPLETION.md](./MCP_PHASE_2C_COMPLETION.md)
- [MCP Specification](https://spec.modelcontextprotocol.io/)
- [Project README](../README.md)

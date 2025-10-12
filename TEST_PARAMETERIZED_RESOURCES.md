# Test Plan: Parameterized MCP Resources

**Goal**: Verify all 3 parameterized resource templates work correctly

---

## Prerequisites

Based on your earlier Copilot test, you have these collections:
1. **Heliograph_Test_Document** - 4 documents
2. **AI Engineering** - 1 document
3. **Knowledge Manager** - 12 documents
4. **machine Learning With python** - 1 document

---

## Test Suite

### ✅ Test 1: Get Collections (Static Resource - Known Working)

**Request via Copilot**:
```
@knowledge-mcp read resource://knowledge/collections
```

**Expected Result**:
```json
{
  "totalCollections": 4,
  "collections": [
    {"id": "Heliograph_Test_Document", "name": "Heliograph_Test_Document", "documentCount": 4},
    {"id": "AI Engineering", "name": "AI Engineering", "documentCount": 1},
    {"id": "machine Learning With python", "name": "machine Learning With python", "documentCount": 1},
    {"id": "Knowledge Manager", "name": "Knowledge Manager", "documentCount": 12}
  ]
}
```

**Status**: ✅ Already confirmed working

---

### 🧪 Test 2: Parameterized Resource #1 - Document Lists

**Template**: `resource://knowledge/{collectionId}/documents`

#### Test 2A: Heliograph_Test_Document
**Request via Copilot**:
```
@knowledge-mcp read resource://knowledge/Heliograph_Test_Document/documents
```

**Expected Result**:
```json
{
  "collectionId": "Heliograph_Test_Document",
  "totalDocuments": 4,
  "documents": [
    {
      "documentId": "...",
      "fileName": "chapter1.md",
      "chunkCount": ...,
      "fileSize": ...,
      "fileType": "text/markdown",
      "uploadedAt": "..."
    },
    // ... 3 more documents
  ]
}
```

#### Test 2B: AI Engineering
**Request via Copilot**:
```
@knowledge-mcp read resource://knowledge/AI Engineering/documents
```

**Expected Result**:
```json
{
  "collectionId": "AI Engineering",
  "totalDocuments": 1,
  "documents": [
    {
      "documentId": "...",
      "fileName": "...",
      "chunkCount": 1221,
      "fileSize": ...,
      "fileType": "...",
      "uploadedAt": "..."
    }
  ]
}
```

#### Test 2C: Knowledge Manager
**Request via Copilot**:
```
@knowledge-mcp read resource://knowledge/Knowledge Manager/documents
```

**Expected Result**:
```json
{
  "collectionId": "Knowledge Manager",
  "totalDocuments": 12,
  "documents": [...]
}
```

---

### 🧪 Test 3: Parameterized Resource #2 - Individual Documents

**Template**: `resource://knowledge/{collectionId}/document/{documentId}`

#### Test 3A: Get Document IDs First
From Test 2A, note the `documentId` values. Then test reading a specific document.

**Request via Copilot** (replace `{documentId}` with actual ID from Test 2A):
```
@knowledge-mcp read resource://knowledge/Heliograph_Test_Document/document/{documentId}
```

**Expected Result**:
```json
{
  "collectionId": "Heliograph_Test_Document",
  "documentId": "{documentId}",
  "fileName": "chapter1.md",
  "fileType": "text/markdown",
  "chunkCount": ...,
  "content": "# Chapter 1\n\nThe Keeper stood at the edge..."
}
```

**Success Criteria**:
- ✅ Returns full document content
- ✅ Content is reconstructed from chunks in order
- ✅ Correct MIME type detected from filename
- ✅ Metadata included (fileName, fileType, chunkCount)

---

### 🧪 Test 4: Parameterized Resource #3 - Collection Statistics

**Template**: `resource://knowledge/{collectionId}/stats`

#### Test 4A: Heliograph_Test_Document Stats
**Request via Copilot**:
```
@knowledge-mcp read resource://knowledge/Heliograph_Test_Document/stats
```

**Expected Result**:
```json
{
  "collectionId": "Heliograph_Test_Document",
  "name": "Heliograph_Test_Document",
  "documentCount": 4,
  "chunkCount": 20,
  "totalQueries": 17,
  "conversationCount": ...,
  "lastQueried": "...",
  "createdAt": "...",
  "vectorStore": "Qdrant",
  "totalFileSize": ...
}
```

#### Test 4B: AI Engineering Stats
**Request via Copilot**:
```
@knowledge-mcp read resource://knowledge/AI Engineering/stats
```

**Expected Result**:
```json
{
  "collectionId": "AI Engineering",
  "name": "AI Engineering",
  "documentCount": 1,
  "chunkCount": 1221,
  "totalQueries": 5,
  "conversationCount": ...,
  "lastQueried": "...",
  "createdAt": "...",
  "vectorStore": "Qdrant",
  "totalFileSize": ...
}
```

---

### 🧪 Test 5: Error Handling

#### Test 5A: Non-existent Collection
**Request via Copilot**:
```
@knowledge-mcp read resource://knowledge/NonExistentCollection/documents
```

**Expected Result**:
```json
{
  "error": {
    "code": -32002,
    "message": "Collection not found: NonExistentCollection"
  }
}
```

#### Test 5B: Non-existent Document
**Request via Copilot**:
```
@knowledge-mcp read resource://knowledge/AI Engineering/document/fake-doc-id
```

**Expected Result**:
```json
{
  "error": {
    "code": -32002,
    "message": "Document not found: fake-doc-id in collection AI Engineering"
  }
}
```

---

## Testing Instructions

### Option 1: Test with VS Code Copilot (Recommended)

1. Open VS Code in your ChatComplete workspace
2. Open Copilot Chat
3. Run each test command above using `@knowledge-mcp`
4. Record results below

### Option 2: Test with MCP Inspector

1. Use your MCP inspector tool
2. Send `resources/read` requests with the URIs above
3. Verify responses match expected format

### Option 3: Test with Claude Desktop

1. Configure Claude Desktop with your MCP server
2. Ask Claude to read the resources
3. Verify Claude can access the data

---

## Results Template

### Test 2A: Heliograph_Test_Document/documents
- Status: ⬜ Not Tested / ✅ Pass / ❌ Fail
- Notes:

### Test 2B: AI Engineering/documents
- Status: ⬜ Not Tested / ✅ Pass / ❌ Fail
- Notes:

### Test 2C: Knowledge Manager/documents
- Status: ⬜ Not Tested / ✅ Pass / ❌ Fail
- Notes:

### Test 3A: Individual Document
- Status: ⬜ Not Tested / ✅ Pass / ❌ Fail
- Document ID Tested:
- Notes:

### Test 4A: Heliograph_Test_Document/stats
- Status: ⬜ Not Tested / ✅ Pass / ❌ Fail
- Notes:

### Test 4B: AI Engineering/stats
- Status: ⬜ Not Tested / ✅ Pass / ❌ Fail
- Notes:

### Test 5A: Non-existent Collection Error
- Status: ⬜ Not Tested / ✅ Pass / ❌ Fail
- Notes:

### Test 5B: Non-existent Document Error
- Status: ⬜ Not Tested / ✅ Pass / ❌ Fail
- Notes:

---

## Success Criteria

All tests pass if:
- ✅ All parameterized URIs resolve correctly
- ✅ Parameters are extracted and injected properly
- ✅ All data is returned in correct format
- ✅ Error handling works for invalid parameters
- ✅ MIME types detected correctly
- ✅ Document content reconstructed from chunks

---

## Quick Test Command for Copilot

Copy/paste these one at a time:

```
@knowledge-mcp read resource://knowledge/Heliograph_Test_Document/documents

@knowledge-mcp read resource://knowledge/AI Engineering/documents

@knowledge-mcp read resource://knowledge/Heliograph_Test_Document/stats

@knowledge-mcp read resource://knowledge/NonExistentCollection/documents
```

If all 4 commands work correctly, then all your parameterized resources are functioning perfectly! ✅

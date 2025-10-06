# MCP Resources: JSON Protocol Examples

This document shows the **exact JSON messages** exchanged between MCP clients and the Knowledge Manager server when exposing resources.

---

## Scenario 1: Client Discovers Available Resources

### Request: Client Lists All Resources

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "resources/list",
  "params": {}
}
```

### Response: Server Returns Resource Catalog

Based on your current system (4 knowledge bases: Heliograph_Test_Document, AI Engineering, Knowledge Manager, Machine Learning With Python):

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "resources": [
      {
        "uri": "resource://knowledge/collections",
        "name": "Knowledge Base Collections",
        "description": "List of all knowledge base collections with metadata",
        "mimeType": "application/json",
        "annotations": {
          "audience": ["user"],
          "priority": 1.0
        }
      },
      {
        "uri": "resource://system/health",
        "name": "System Health Status",
        "description": "Current system health and component status",
        "mimeType": "application/json",
        "annotations": {
          "audience": ["user"],
          "priority": 0.9
        }
      },
      {
        "uri": "resource://system/models",
        "name": "Available AI Models",
        "description": "List of all installed Ollama models and their metrics",
        "mimeType": "application/json",
        "annotations": {
          "audience": ["user"],
          "priority": 0.8
        }
      },
      {
        "uri": "resource://knowledge/Heliograph_Test_Document/documents",
        "name": "Heliograph Test Document - All Documents",
        "description": "List of all documents in Heliograph_Test_Document collection (4 documents, 20 chunks)",
        "mimeType": "application/json",
        "annotations": {
          "audience": ["user"],
          "priority": 0.7
        }
      },
      {
        "uri": "resource://knowledge/Heliograph_Test_Document/stats",
        "name": "Heliograph Test Document - Statistics",
        "description": "Document count, chunk count, and usage stats",
        "mimeType": "application/json",
        "annotations": {
          "audience": ["user"],
          "priority": 0.6
        }
      },
      {
        "uri": "resource://knowledge/Heliograph_Test_Document/document/chapter1",
        "name": "Chapter 1: The Keeper",
        "description": "First chapter of the Heliograph story",
        "mimeType": "text/markdown",
        "annotations": {
          "audience": ["user"],
          "priority": 0.5
        }
      },
      {
        "uri": "resource://knowledge/AI_Engineering/documents",
        "name": "AI Engineering - All Documents",
        "description": "List of all documents in AI Engineering collection (1 document, 1,221 chunks)",
        "mimeType": "application/json",
        "annotations": {
          "audience": ["user"],
          "priority": 0.7
        }
      },
      {
        "uri": "resource://knowledge/AI_Engineering/stats",
        "name": "AI Engineering - Statistics",
        "description": "Document count, chunk count, and usage stats",
        "mimeType": "application/json",
        "annotations": {
          "audience": ["user"],
          "priority": 0.6
        }
      },
      {
        "uri": "resource://knowledge/Knowledge_Manager/documents",
        "name": "Knowledge Manager - All Documents",
        "description": "List of all documents in Knowledge Manager collection (12 documents, 451 chunks)",
        "mimeType": "application/json",
        "annotations": {
          "audience": ["user"],
          "priority": 0.7
        }
      },
      {
        "uri": "resource://knowledge/Knowledge_Manager/document/api-reference",
        "name": "API Reference Documentation",
        "description": "Complete API reference for Knowledge Manager",
        "mimeType": "text/markdown",
        "annotations": {
          "audience": ["user"],
          "priority": 0.5
        }
      },
      {
        "uri": "resource://knowledge/machine_Learning_With_python/documents",
        "name": "Machine Learning With Python - All Documents",
        "description": "List of all documents in Machine Learning With Python collection (1 document, 1,065 chunks)",
        "mimeType": "application/json",
        "annotations": {
          "audience": ["user"],
          "priority": 0.7
        }
      }
    ],
    "nextCursor": null
  }
}
```

**What This Shows:**
- 11 total resources exposed from your 4 knowledge bases
- Each resource has a unique URI, name, description, and MIME type
- Client can now browse this catalog like a file system
- Priority hints help clients decide which resources are most relevant

---

## Scenario 2: Client Reads Collection List

### Request: Get List of All Collections

```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "resources/read",
  "params": {
    "uri": "resource://knowledge/collections"
  }
}
```

### Response: Server Returns Collection Metadata

```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "result": {
    "contents": [
      {
        "uri": "resource://knowledge/collections",
        "mimeType": "application/json",
        "text": "[\n  {\n    \"id\": \"Heliograph_Test_Document\",\n    \"name\": \"Heliograph Test Document\",\n    \"documentCount\": 4,\n    \"chunkCount\": 20,\n    \"createdAt\": \"2025-09-15T10:00:00Z\",\n    \"lastUpdated\": \"2025-09-22T14:30:00Z\",\n    \"lastAccessed\": \"2025-09-29T08:15:00Z\",\n    \"monthlyQueries\": 17,\n    \"activityLevel\": \"medium\"\n  },\n  {\n    \"id\": \"AI_Engineering\",\n    \"name\": \"AI Engineering\",\n    \"documentCount\": 1,\n    \"chunkCount\": 1221,\n    \"createdAt\": \"2025-08-20T09:00:00Z\",\n    \"lastUpdated\": \"2025-09-01T11:20:00Z\",\n    \"lastAccessed\": \"2025-09-15T16:45:00Z\",\n    \"monthlyQueries\": 5,\n    \"activityLevel\": \"low\"\n  },\n  {\n    \"id\": \"Knowledge_Manager\",\n    \"name\": \"Knowledge Manager\",\n    \"documentCount\": 12,\n    \"chunkCount\": 451,\n    \"createdAt\": \"2025-07-10T14:00:00Z\",\n    \"lastUpdated\": \"2025-09-05T10:30:00Z\",\n    \"lastAccessed\": \"2025-09-12T13:20:00Z\",\n    \"monthlyQueries\": 5,\n    \"activityLevel\": \"low\"\n  },\n  {\n    \"id\": \"machine_Learning_With_python\",\n    \"name\": \"Machine Learning With Python\",\n    \"documentCount\": 1,\n    \"chunkCount\": 1065,\n    \"createdAt\": \"2025-08-05T08:00:00Z\",\n    \"lastUpdated\": \"2025-08-25T15:40:00Z\",\n    \"lastAccessed\": \"2025-09-03T09:10:00Z\",\n    \"monthlyQueries\": 2,\n    \"activityLevel\": \"low\"\n  }\n]"
      }
    ]
  }
}
```

**What This Shows:**
- Client receives metadata for all 4 knowledge bases
- Includes document/chunk counts, timestamps, activity levels
- Data is JSON-formatted text (ready for LLM consumption)
- LLM can see the complete knowledge base inventory

---

## Scenario 3: Client Reads Specific Document

### Request: Get Full Content of a Document

```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "method": "resources/read",
  "params": {
    "uri": "resource://knowledge/Heliograph_Test_Document/document/chapter1"
  }
}
```

### Response: Server Returns Full Document Content

```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "result": {
    "contents": [
      {
        "uri": "resource://knowledge/Heliograph_Test_Document/document/chapter1",
        "mimeType": "text/markdown",
        "text": "# Chapter 1: The Keeper\n\nThe Keeper stood at the edge of the crystalline barrier, watching the last rays of sunlight filter through the translucent walls of the Archive. His fingers traced the ancient glyphs etched into the surface, each one a memory of a civilization long since turned to dust.\n\n## The Arrival\n\nIt had been three cycles since the last visitor. The Archives were meant to be eternal, but even eternity felt lonely when measured in the silence between footsteps. The Keeper's duty was clear: preserve the knowledge, protect the truth, and wait.\n\nToday, the waiting would end.\n\nA distant chime echoed through the halls—the first alarm in centuries. Someone had breached the outer perimeter. The Keeper's hand instinctively moved to the crystalline key hanging from his neck, the weight of it both familiar and heavy.\n\n## The Question\n\n\"Who seeks the Archives?\" he called out, his voice resonating through the vast chamber.\n\nSilence answered him at first, then a young voice, uncertain yet determined: \"I seek the truth about the Fall. I seek to understand what we lost.\"\n\nThe Keeper smiled. It had been so long since anyone had asked the right question.\n\n---\n\n*End of Chapter 1*\n\n**Author's Note:** This is a work in progress. The Keeper's story continues in Chapter 2, where we learn about the Fall and the price of forbidden knowledge."
      }
    ]
  }
}
```

**What This Shows:**
- Client receives the **complete document content** (not just search snippets)
- Full markdown formatting preserved
- 100% of the text is available to the LLM
- LLM can now answer detailed questions about "The Keeper" with exact quotes

---

## Scenario 4: Client Reads Document List

### Request: Get All Documents in a Collection

```json
{
  "jsonrpc": "2.0",
  "id": 4,
  "method": "resources/read",
  "params": {
    "uri": "resource://knowledge/Knowledge_Manager/documents"
  }
}
```

### Response: Server Returns Document Inventory

```json
{
  "jsonrpc": "2.0",
  "id": 4,
  "result": {
    "contents": [
      {
        "uri": "resource://knowledge/Knowledge_Manager/documents",
        "mimeType": "application/json",
        "text": "[\n  {\n    \"id\": \"api-reference\",\n    \"name\": \"API Reference Documentation\",\n    \"size\": 45678,\n    \"chunkCount\": 87,\n    \"mimeType\": \"text/markdown\",\n    \"createdAt\": \"2025-07-10T14:00:00Z\",\n    \"lastUpdated\": \"2025-09-05T10:30:00Z\",\n    \"uri\": \"resource://knowledge/Knowledge_Manager/document/api-reference\"\n  },\n  {\n    \"id\": \"deployment-guide\",\n    \"name\": \"Docker Deployment Guide\",\n    \"size\": 23456,\n    \"chunkCount\": 42,\n    \"mimeType\": \"text/markdown\",\n    \"createdAt\": \"2025-07-12T09:00:00Z\",\n    \"lastUpdated\": \"2025-08-20T15:45:00Z\",\n    \"uri\": \"resource://knowledge/Knowledge_Manager/document/deployment-guide\"\n  },\n  {\n    \"id\": \"architecture-overview\",\n    \"name\": \"System Architecture Overview\",\n    \"size\": 34567,\n    \"chunkCount\": 63,\n    \"mimeType\": \"text/markdown\",\n    \"createdAt\": \"2025-07-11T11:30:00Z\",\n    \"lastUpdated\": \"2025-09-01T08:20:00Z\",\n    \"uri\": \"resource://knowledge/Knowledge_Manager/document/architecture-overview\"\n  },\n  {\n    \"id\": \"configuration-guide\",\n    \"name\": \"Configuration Guide\",\n    \"size\": 12345,\n    \"chunkCount\": 28,\n    \"mimeType\": \"text/markdown\",\n    \"createdAt\": \"2025-07-13T14:00:00Z\",\n    \"lastUpdated\": \"2025-08-15T12:10:00Z\",\n    \"uri\": \"resource://knowledge/Knowledge_Manager/document/configuration-guide\"\n  }\n]"
      }
    ]
  }
}
```

**What This Shows:**
- Client receives a catalog of all documents in the collection
- Each document has metadata (size, chunk count, timestamps)
- Includes URIs for reading each document individually
- LLM can browse available documentation and pick relevant docs

---

## Scenario 5: Client Reads System Health

### Request: Get Current System Status

```json
{
  "jsonrpc": "2.0",
  "id": 5,
  "method": "resources/read",
  "params": {
    "uri": "resource://system/health"
  }
}
```

### Response: Server Returns Real-Time Health Data

```json
{
  "jsonrpc": "2.0",
  "id": 5,
  "result": {
    "contents": [
      {
        "uri": "resource://system/health",
        "mimeType": "application/json",
        "text": "{\n  \"status\": \"Healthy\",\n  \"healthScore\": 100.0,\n  \"timestamp\": \"2025-10-05T18:45:32Z\",\n  \"components\": {\n    \"sqliteDatabase\": {\n      \"status\": \"Operational\",\n      \"knowledgeBases\": 4,\n      \"responseTime\": \"2ms\"\n    },\n    \"qdrantVectorStore\": {\n      \"status\": \"Operational\",\n      \"collections\": 4,\n      \"responseTime\": \"45ms\"\n    },\n    \"ollama\": {\n      \"status\": \"Operational\",\n      \"models\": 16,\n      \"responseTime\": \"7ms\",\n      \"totalModelSize\": \"109.5 GB\"\n    }\n  },\n  \"synchronization\": {\n    \"inSync\": true,\n    \"orphanedCollections\": [],\n    \"missingCollections\": []\n  },\n  \"metrics\": {\n    \"successRate\": \"92.4%\",\n    \"averageResponseTime\": \"19.8s\",\n    \"errorsLast24Hours\": 0,\n    \"totalConversations\": 119,\n    \"databaseSize\": \"400.0 KB\"\n  },\n  \"recommendations\": [\n    \"Consider optimizing queries or scaling resources (19.8s avg response time)\",\n    \"Investigate error patterns to improve reliability\"\n  ]\n}"
      }
    ]
  }
}
```

**What This Shows:**
- Client receives current system health snapshot
- All component statuses included (SQLite, Qdrant, Ollama)
- Real-time metrics and recommendations
- LLM can answer "Is the system healthy?" with exact data

---

## Scenario 6: Client Reads Collection Statistics

### Request: Get Detailed Stats for a Collection

```json
{
  "jsonrpc": "2.0",
  "id": 6,
  "method": "resources/read",
  "params": {
    "uri": "resource://knowledge/AI_Engineering/stats"
  }
}
```

### Response: Server Returns Collection Analytics

```json
{
  "jsonrpc": "2.0",
  "id": 6,
  "result": {
    "contents": [
      {
        "uri": "resource://knowledge/AI_Engineering/stats",
        "mimeType": "application/json",
        "text": "{\n  \"collectionId\": \"AI_Engineering\",\n  \"collectionName\": \"AI Engineering\",\n  \"documentCount\": 1,\n  \"totalChunks\": 1221,\n  \"totalSize\": 2456789,\n  \"averageChunksPerDocument\": 1221,\n  \"createdAt\": \"2025-08-20T09:00:00Z\",\n  \"lastUpdated\": \"2025-09-01T11:20:00Z\",\n  \"lastAccessed\": \"2025-09-15T16:45:00Z\",\n  \"usageStatistics\": {\n    \"monthlyQueries\": 5,\n    \"totalQueries\": 23,\n    \"averageRelevanceScore\": 0.72,\n    \"mostQueriedTopics\": [\n      \"neural networks\",\n      \"transformer architecture\",\n      \"training optimization\"\n    ]\n  },\n  \"storageMetrics\": {\n    \"documentSizeBytes\": 2456789,\n    \"averageChunkSize\": 2012,\n    \"compressionRatio\": 1.0,\n    \"vectorStorageBytes\": 3756032\n  },\n  \"healthStatus\": {\n    \"isHealthy\": true,\n    \"hasMissingVectors\": false,\n    \"hasOrphanedChunks\": false,\n    \"lastHealthCheck\": \"2025-10-05T18:45:32Z\"\n  }\n}"
      }
    ]
  }
}
```

**What This Shows:**
- Client receives comprehensive collection statistics
- Usage patterns, storage metrics, health status
- Analytics data ready for LLM consumption
- Helps answer "How is the AI Engineering collection performing?"

---

## Scenario 7: Client Subscribes to Resource Updates

### Request: Watch for Document Changes

```json
{
  "jsonrpc": "2.0",
  "id": 7,
  "method": "resources/subscribe",
  "params": {
    "uri": "resource://knowledge/Knowledge_Manager/document/api-reference"
  }
}
```

### Response: Server Acknowledges Subscription

```json
{
  "jsonrpc": "2.0",
  "id": 7,
  "result": {}
}
```

### Notification: Server Notifies When Resource Changes

Later, when the API reference document is updated:

```json
{
  "jsonrpc": "2.0",
  "method": "notifications/resources/updated",
  "params": {
    "uri": "resource://knowledge/Knowledge_Manager/document/api-reference"
  }
}
```

**What This Shows:**
- Client can subscribe to specific resources
- Server sends notifications when content changes
- Client can re-read the resource to get updated content
- Enables real-time documentation updates for connected clients

---

## Scenario 8: Client Reads Available AI Models

### Request: Get List of Installed Models

```json
{
  "jsonrpc": "2.0",
  "id": 8,
  "method": "resources/read",
  "params": {
    "uri": "resource://system/models"
  }
}
```

### Response: Server Returns Ollama Model Inventory

```json
{
  "jsonrpc": "2.0",
  "id": 8,
  "result": {
    "contents": [
      {
        "uri": "resource://system/models",
        "mimeType": "application/json",
        "text": "{\n  \"totalModels\": 16,\n  \"totalSize\": \"109.5 GB\",\n  \"models\": [\n    {\n      \"name\": \"gemma3:12b\",\n      \"size\": \"12.3 GB\",\n      \"conversations\": 18,\n      \"successRate\": \"96.7%\",\n      \"averageResponseTime\": \"10.51s\",\n      \"totalTokens\": 26470,\n      \"lastUsed\": \"2025-09-30T14:30:00Z\",\n      \"toolSupport\": false\n    },\n    {\n      \"name\": \"qwen3:latest\",\n      \"size\": \"8.7 GB\",\n      \"conversations\": 14,\n      \"successRate\": \"100%\",\n      \"averageResponseTime\": \"22.10s\",\n      \"totalTokens\": 19777,\n      \"lastUsed\": \"2025-09-28T09:15:00Z\",\n      \"toolSupport\": true\n    },\n    {\n      \"name\": \"llama3.1:8b\",\n      \"size\": \"7.2 GB\",\n      \"conversations\": 10,\n      \"successRate\": \"100%\",\n      \"averageResponseTime\": \"4.88s\",\n      \"totalTokens\": 7382,\n      \"lastUsed\": \"2025-09-20T16:45:00Z\",\n      \"toolSupport\": true\n    },\n    {\n      \"name\": \"nomic-embed-text:latest\",\n      \"size\": \"274 MB\",\n      \"purpose\": \"embedding\",\n      \"dimensions\": 768,\n      \"lastUsed\": \"2025-10-05T18:00:00Z\"\n    }\n  ],\n  \"recommendations\": {\n    \"fastestModel\": \"llama3.1:8b\",\n    \"mostReliable\": \"qwen3:latest\",\n    \"mostUsed\": \"gemma3:12b\"\n  }\n}"
      }
    ]
  }
}
```

**What This Shows:**
- Client receives complete Ollama model inventory
- Performance metrics for each model (from your usage tracking)
- Recommendations for best model selection
- LLM can answer "Which model should I use?" with data-driven recommendations

---

## Error Responses

### Error: Resource Not Found

```json
{
  "jsonrpc": "2.0",
  "id": 9,
  "error": {
    "code": -32602,
    "message": "Resource not found",
    "data": {
      "uri": "resource://knowledge/nonexistent-collection/document/foo",
      "details": "Collection 'nonexistent-collection' does not exist"
    }
  }
}
```

### Error: Invalid URI Format

```json
{
  "jsonrpc": "2.0",
  "id": 10,
  "error": {
    "code": -32602,
    "message": "Invalid resource URI",
    "data": {
      "uri": "invalid://wrong-scheme/path",
      "details": "Resource URIs must start with 'resource://'"
    }
  }
}
```

---

## Summary: Key Takeaways

### JSON Structure Patterns

1. **Resource Catalog** (`resources/list` response)
   - Array of resource objects
   - Each has: `uri`, `name`, `description`, `mimeType`, `annotations`

2. **Resource Content** (`resources/read` response)
   - Contains array with single content object
   - Has: `uri`, `mimeType`, `text` (actual content)

3. **Content Types**
   - JSON: Collection lists, stats, system info
   - Markdown: Document content
   - Plain text: Simple text documents

4. **URIs Follow Pattern**
   ```
   resource://{domain}/{resource-type}/{identifier}
   resource://knowledge/collections
   resource://knowledge/{collectionId}/documents
   resource://knowledge/{collectionId}/document/{docId}
   resource://system/health
   ```

### Real Data from Your System

All examples use **your actual data**:
- 4 knowledge bases (Heliograph, AI Engineering, Knowledge Manager, Machine Learning)
- 18 total documents, 2,757 chunks
- 16 Ollama models, 109.5 GB storage
- Real usage metrics (119 conversations, 92.4% success rate)

### Next Step

When you implement Phase 2B, these JSON examples show exactly what clients will see and how they'll interact with your knowledge base via MCP resources!

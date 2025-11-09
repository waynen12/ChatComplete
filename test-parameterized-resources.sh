#!/bin/bash

# Test MCP Parameterized Resources
# This script tests all 3 parameterized resource templates with real data

echo "=========================================="
echo "MCP Parameterized Resources Test Suite"
echo "=========================================="
echo ""

MCP_SERVER="dotnet /home/wayne/repos/ChatComplete/Knowledge.Mcp/bin/Debug/net8.0/Knowledge.Mcp.dll"

# Helper function to send MCP request and get response
send_request() {
    local request="$1"
    local description="$2"

    echo "----------------------------------------"
    echo "Test: $description"
    echo "Request: $request"
    echo ""

    # Send request and capture output, filtering logs
    echo "$request" | timeout 5 $MCP_SERVER 2>/dev/null | \
        grep -v "^info:" | \
        grep -v "^warn:" | \
        grep -v "^dbug:" | \
        grep -v "MCP Server" | \
        python3 -c "import sys, json; [print(json.dumps(json.loads(line), indent=2)) for line in sys.stdin if line.strip() and line.strip()[0] == '{']" 2>/dev/null | \
        head -150

    echo ""
}

# Test 1: Get collection list (to find collection IDs for parameterized tests)
echo "=========================================="
echo "STEP 1: Get Collection List (Static Resource)"
echo "=========================================="
send_request \
    '{"jsonrpc":"2.0","id":1,"method":"resources/read","params":{"uri":"resource://knowledge/collections"}}' \
    "Get all collections"

# Test 2: Parameterized - Document list for a specific collection
echo "=========================================="
echo "STEP 2: Test Parameterized Resource #1"
echo "Template: resource://knowledge/{collectionId}/documents"
echo "=========================================="

send_request \
    '{"jsonrpc":"2.0","id":2,"method":"resources/read","params":{"uri":"resource://knowledge/AI Engineering/documents"}}' \
    "Get documents in 'AI Engineering' collection"

send_request \
    '{"jsonrpc":"2.0","id":3,"method":"resources/read","params":{"uri":"resource://knowledge/Heliograph_Test_Document/documents"}}' \
    "Get documents in 'Heliograph_Test_Document' collection"

# Test 3: Parameterized - Specific document content
echo "=========================================="
echo "STEP 3: Test Parameterized Resource #2"
echo "Template: resource://knowledge/{collectionId}/document/{documentId}"
echo "=========================================="

send_request \
    '{"jsonrpc":"2.0","id":4,"method":"resources/read","params":{"uri":"resource://knowledge/Heliograph_Test_Document/document/chapter1.md"}}' \
    "Get specific document content (chapter1.md)"

# Test 4: Parameterized - Collection statistics
echo "=========================================="
echo "STEP 4: Test Parameterized Resource #3"
echo "Template: resource://knowledge/{collectionId}/stats"
echo "=========================================="

send_request \
    '{"jsonrpc":"2.0","id":5,"method":"resources/read","params":{"uri":"resource://knowledge/AI Engineering/stats"}}' \
    "Get statistics for 'AI Engineering' collection"

send_request \
    '{"jsonrpc":"2.0","id":6,"method":"resources/read","params":{"uri":"resource://knowledge/Knowledge Manager/stats"}}' \
    "Get statistics for 'Knowledge Manager' collection"

# Test 5: Error handling - Invalid collection
echo "=========================================="
echo "STEP 5: Test Error Handling"
echo "=========================================="

send_request \
    '{"jsonrpc":"2.0","id":7,"method":"resources/read","params":{"uri":"resource://knowledge/NonExistentCollection/documents"}}' \
    "Try to access non-existent collection (should error)"

send_request \
    '{"jsonrpc":"2.0","id":8,"method":"resources/read","params":{"uri":"resource://knowledge/AI Engineering/document/nonexistent.md"}}' \
    "Try to access non-existent document (should error)"

echo "=========================================="
echo "Test Suite Complete"
echo "=========================================="

#!/bin/bash

# Test MCP resources/list endpoint
# This verifies that the MCP server properly advertises its resources

echo "=== Testing MCP Resources Discovery ==="
echo ""
echo "Starting MCP server and testing resources/list..."
echo ""

# Create a simple JSON-RPC request for resources/list
REQUEST='{"jsonrpc":"2.0","id":1,"method":"resources/list","params":{}}'

echo "Request: $REQUEST"
echo ""
echo "Response:"
echo ""

# Send request to MCP server via STDIN and capture response
echo "$REQUEST" | timeout 3 dotnet /home/wayne/repos/ChatComplete/Knowledge.Mcp/bin/Debug/net8.0/Knowledge.Mcp.dll 2>&1 | \
    grep -v "^info:" | \
    grep -v "^warn:" | \
    grep -v "^dbug:" | \
    grep -v "MCP Server" | \
    grep -v "configuration" | \
    grep -v "using database" | \
    grep -v "configuring" | \
    head -100

echo ""
echo "=== Test Complete ==="

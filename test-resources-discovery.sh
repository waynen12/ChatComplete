#!/bin/bash

# Test if MCP server discovers resources
# This sends an "initialize" request to the MCP server and checks the response

echo "Starting MCP server and testing resource discovery..."
echo ""

# Send initialize request via STDIN
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test-client","version":"1.0"}}}' | \
dotnet bin/Debug/net8.0/Knowledge.Mcp.dll 2>&1 | \
grep -A50 "resources\|tools" | \
head -100

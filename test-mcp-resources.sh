#!/bin/bash
# Test MCP server resources/list endpoint

cd /home/wayne/repos/ChatComplete

echo "=== Testing MCP Resources Discovery ==="
echo ""

# Create a temporary file for requests
cat > /tmp/mcp_test.jsonl << 'EOF'
{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{"resources":{}},"clientInfo":{"name":"test-client","version":"1.0"}}}
{"jsonrpc":"2.0","id":2,"method":"initialized","params":{}}
{"jsonrpc":"2.0","id":3,"method":"resources/list","params":{}}
EOF

# Run MCP server with the requests
cat /tmp/mcp_test.jsonl | timeout 5 dotnet Knowledge.Mcp/bin/Debug/net8.0/Knowledge.Mcp.dll 2>&1

echo ""
echo "=== Test Complete ==="

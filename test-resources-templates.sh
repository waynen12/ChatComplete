#!/bin/bash
# Test MCP Resources Templates endpoint

set -e

cd /home/wayne/repos/ChatComplete

echo "=== Testing MCP Resources Templates Endpoint ==="
echo ""

# Create a simple test that sends multiple requests
cat << 'EOF' | timeout 10 dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj 2>&1
{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{"roots":{"listChanged":true},"sampling":{}},"clientInfo":{"name":"test-client","version":"1.0.0"}}}
{"jsonrpc":"2.0","id":2,"method":"resources/list"}
{"jsonrpc":"2.0","id":3,"method":"resources/templates/list"}
EOF

echo ""
echo "=== Test Complete ==="

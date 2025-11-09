#!/bin/bash
# Test MCP Resources and Templates endpoints with clean JSON output

set -e

cd /home/wayne/repos/ChatComplete

echo "=== Testing MCP Resources Endpoints ==="
echo ""

# Create a test that sends multiple requests and captures JSON responses
(
  echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{"roots":{"listChanged":true},"sampling":{}},"clientInfo":{"name":"test-client","version":"1.0.0"}}}'
  sleep 1
  echo '{"jsonrpc":"2.0","id":2,"method":"resources/list"}'
  sleep 1
  echo '{"jsonrpc":"2.0","id":3,"method":"resources/templates/list"}'
  sleep 2
) | timeout 10 dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj 2>&1 | \
  grep -E '^\{.*"result".*\}$' | \
  while IFS= read -r line; do
    echo "$line" | jq '.' 2>/dev/null || echo "$line"
  done

echo ""
echo "=== Test Complete ==="

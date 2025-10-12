#!/bin/bash

# Test MCP server initialization to see capabilities
echo "=== Testing MCP Server Initialization ==="
echo ""

# Send initialize request
REQUEST='{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{"roots":{"listChanged":true},"sampling":{}},"clientInfo":{"name":"test-client","version":"1.0.0"}}}'

echo "Sending initialize request..."
echo ""

echo "$REQUEST" | timeout 3 dotnet /home/wayne/repos/ChatComplete/Knowledge.Mcp/bin/Debug/net8.0/Knowledge.Mcp.dll 2>/dev/null | \
    python3 -c "import sys, json; [print(json.dumps(json.loads(line), indent=2)) for line in sys.stdin if line.strip() and line.strip()[0] == '{']" 2>/dev/null | head -100

echo ""
echo "=== Test Complete ==="

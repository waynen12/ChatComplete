#!/bin/bash
# Test MCP server resources/list endpoint - capture JSON responses only

cd /home/wayne/repos/ChatComplete

# Create requests
cat > /tmp/mcp_test.jsonl << 'EOF'
{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{"resources":{}},"clientInfo":{"name":"test-client","version":"1.0"}}}
{"jsonrpc":"2.0","id":3,"method":"resources/list","params":{}}
EOF

# Run MCP server - capture stdout (JSON responses) and stderr (logs) separately
cat /tmp/mcp_test.jsonl | timeout 5 dotnet Knowledge.Mcp/bin/Debug/net8.0/Knowledge.Mcp.dll 2>/tmp/mcp_stderr.log 1>/tmp/mcp_stdout.log

echo "=== STDOUT (JSON Responses) ==="
cat /tmp/mcp_stdout.log

echo ""
echo "=== Key Log Lines ==="
grep -E "(resources/list|Discover|resource)" /tmp/mcp_stderr.log | head -10

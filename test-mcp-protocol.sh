#!/bin/bash

# Test MCP protocol by sending initialize, then tools/list, then resources/list

cd /home/wayne/repos/ChatComplete

# Start the MCP server in background
dotnet Knowledge.Mcp/bin/Debug/net8.0/Knowledge.Mcp.dll 2>&1 &
MCP_PID=$!

sleep 1

# Send initialize request
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}'

sleep 1

# Send tools/list request
echo '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}'

sleep 1

# Send resources/list request
echo '{"jsonrpc":"2.0","id":3,"method":"resources/list","params":{}}'

sleep 2

# Kill the server
kill $MCP_PID 2>/dev/null

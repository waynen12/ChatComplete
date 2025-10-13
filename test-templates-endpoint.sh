#!/bin/bash
# Test the resources/templates/list endpoint of Knowledge MCP Server

echo "Starting Knowledge.Mcp server..."

# Start the MCP server in the background
cd /home/wayne/repos/ChatComplete
dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj &
SERVER_PID=$!

# Give the server time to start
sleep 3

echo "Testing resources/templates/list endpoint..."

# Send resources/templates/list request via stdio
echo '{"jsonrpc":"2.0","id":1,"method":"resources/templates/list"}' | \
  dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj

# Kill the server
kill $SERVER_PID 2>/dev/null

echo "Test complete!"

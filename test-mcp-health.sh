#!/bin/bash

# Get system health from MCP server on port 5001
# This script properly handles the SSE session management

set -e

SERVER_URL="http://localhost:5001"

echo "🔍 Connecting to MCP server at $SERVER_URL..."
echo ""

# Start SSE connection in background and capture session ID
SSE_OUTPUT=$(mktemp)
curl -N -s "$SERVER_URL/sse" -H 'Accept: text/event-stream' > "$SSE_OUTPUT" &
SSE_PID=$!

# Wait a moment for connection
sleep 1

# Extract session ID
SESSION_ID=$(grep -m1 "data:" "$SSE_OUTPUT" | sed 's/.*sessionId=//' || echo "")

if [ -z "$SESSION_ID" ]; then
    echo "❌ Failed to get session ID from SSE endpoint"
    kill $SSE_PID 2>/dev/null || true
    rm "$SSE_OUTPUT"
    exit 1
fi

echo "✅ Session established: $SESSION_ID"
echo ""

# Make the health check request
echo "📊 Requesting system health..."
echo ""

RESPONSE=$(curl -s -X POST "$SERVER_URL/message?sessionId=$SESSION_ID" \
  -H 'Content-Type: application/json' \
  -H 'Accept: application/json' \
  -d '{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "tools/call",
    "params": {
      "name": "get_system_health",
      "arguments": {}
    }
  }')

# Clean up
kill $SSE_PID 2>/dev/null || true
rm "$SSE_OUTPUT"

# Display results
echo "$RESPONSE" | jq '.' 2>/dev/null || echo "$RESPONSE"

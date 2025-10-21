#!/bin/bash

# Get system health from MCP server with proper SSE handling
# Usage: ./get-system-health.sh [port]

PORT="${1:-5001}"
SERVER_URL="http://localhost:$PORT"

echo "🔍 Connecting to MCP server at $SERVER_URL..."
echo ""

# Create named pipes for communication
SSE_PIPE=$(mktemp -u)
mkfifo "$SSE_PIPE"

# Start SSE listener in background
(curl -N -s "$SERVER_URL/sse" -H 'Accept: text/event-stream' > "$SSE_PIPE") &
SSE_PID=$!

# Function to cleanup
cleanup() {
    kill $SSE_PID 2>/dev/null || true
    rm -f "$SSE_PIPE"
}
trap cleanup EXIT

# Read SSE stream and extract session ID
SESSION_ID=""
while IFS= read -r line; do
    if [[ "$line" =~ sessionId=([a-zA-Z0-9_-]+) ]]; then
        SESSION_ID="${BASH_REMATCH[1]}"
        echo "✅ Session established: $SESSION_ID"
        echo ""
        break
    fi
done < "$SSE_PIPE" &
READER_PID=$!

# Wait for session ID (max 5 seconds)
for i in {1..50}; do
    if [ -n "$SESSION_ID" ]; then
        break
    fi
    sleep 0.1
done

if [ -z "$SESSION_ID" ]; then
    echo "❌ Failed to get session ID"
    exit 1
fi

# Send the health check request
echo "📊 Requesting system health..."
curl -s -X POST "$SERVER_URL/message?sessionId=$SESSION_ID" \
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
  }' > /dev/null

echo ""
echo "📨 Waiting for response..."
echo ""

# Read and display SSE events (timeout after 10 seconds)
timeout 10 grep -m1 -A20 "message" "$SSE_PIPE" | while IFS= read -r line; do
    if [[ "$line" =~ ^data: ]]; then
        DATA="${line#data: }"
        if [ "$DATA" != "[DONE]" ] && [ -n "$DATA" ]; then
            echo "$DATA" | jq '.' 2>/dev/null || echo "$DATA"
        fi
    fi
done

echo ""
echo "✅ Health check complete"

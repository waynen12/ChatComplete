#!/bin/bash

# Test script for MCP HTTP endpoints
# Usage: ./test-http-endpoints.sh

set -e

SERVER_URL="http://localhost:5000"
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "=================================================="
echo "MCP HTTP Server Endpoint Tests"
echo "=================================================="
echo ""
echo "Server URL: $SERVER_URL"
echo ""

# Test 1: SSE endpoint (GET /sse)
echo -e "${YELLOW}Test 1: SSE Endpoint (GET /sse)${NC}"
echo "Command: curl -i $SERVER_URL/sse -H 'Accept: text/event-stream'"
echo "---"
HTTP_CODE=$(curl -s -o /tmp/mcp-sse-response.txt -w "%{http_code}" \
    -H 'Accept: text/event-stream' \
    "$SERVER_URL/sse" 2>&1 || echo "000")

if [ "$HTTP_CODE" = "200" ]; then
    echo -e "${GREEN}✅ SSE endpoint returned 200 OK${NC}"
    echo "Response headers:"
    curl -i -s -N --max-time 2 "$SERVER_URL/sse" -H 'Accept: text/event-stream' 2>&1 | head -n 20 || true
else
    echo -e "${RED}❌ SSE endpoint returned $HTTP_CODE${NC}"
    cat /tmp/mcp-sse-response.txt 2>/dev/null || true
fi
echo ""
echo ""

# Test 2: Initialize request (POST /messages)
echo -e "${YELLOW}Test 2: Initialize Request (POST /messages)${NC}"
INIT_PAYLOAD='{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "initialize",
  "params": {
    "protocolVersion": "2024-11-05",
    "capabilities": {
      "roots": {
        "listChanged": false
      }
    },
    "clientInfo": {
      "name": "test-client",
      "version": "1.0.0"
    }
  }
}'

echo "Command: curl -X POST $SERVER_URL/messages [with initialize payload]"
echo "---"
HTTP_CODE=$(curl -s -o /tmp/mcp-init-response.txt -w "%{http_code}" \
    -X POST \
    -H 'Content-Type: application/json' \
    -H 'Accept: application/json' \
    -d "$INIT_PAYLOAD" \
    "$SERVER_URL/messages" 2>&1 || echo "000")

if [ "$HTTP_CODE" = "200" ]; then
    echo -e "${GREEN}✅ Initialize request returned 200 OK${NC}"
    echo "Response:"
    cat /tmp/mcp-init-response.txt | jq '.' 2>/dev/null || cat /tmp/mcp-init-response.txt
else
    echo -e "${RED}❌ Initialize request returned $HTTP_CODE${NC}"
    echo "Response:"
    cat /tmp/mcp-init-response.txt
fi
echo ""
echo ""

# Test 3: List tools (POST /messages)
echo -e "${YELLOW}Test 3: List Tools Request (POST /messages)${NC}"
LIST_TOOLS_PAYLOAD='{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "tools/list",
  "params": {}
}'

echo "Command: curl -X POST $SERVER_URL/messages [with tools/list payload]"
echo "---"
HTTP_CODE=$(curl -s -o /tmp/mcp-tools-response.txt -w "%{http_code}" \
    -X POST \
    -H 'Content-Type: application/json' \
    -H 'Accept: application/json' \
    -d "$LIST_TOOLS_PAYLOAD" \
    "$SERVER_URL/messages" 2>&1 || echo "000")

if [ "$HTTP_CODE" = "200" ]; then
    echo -e "${GREEN}✅ List tools request returned 200 OK${NC}"
    echo "Response:"
    cat /tmp/mcp-tools-response.txt | jq '.result.tools[] | {name: .name, description: .description}' 2>/dev/null || cat /tmp/mcp-tools-response.txt
else
    echo -e "${RED}❌ List tools request returned $HTTP_CODE${NC}"
    echo "Response:"
    cat /tmp/mcp-tools-response.txt
fi
echo ""
echo ""

# Test 4: Call a simple tool (SayHello for minimal, get_system_health for full)
echo -e "${YELLOW}Test 4: Call Tool (POST /messages)${NC}"
CALL_TOOL_PAYLOAD='{
  "jsonrpc": "2.0",
  "id": 3,
  "method": "tools/call",
  "params": {
    "name": "get_system_health",
    "arguments": {}
  }
}'

echo "Command: curl -X POST $SERVER_URL/messages [with tools/call payload]"
echo "---"
HTTP_CODE=$(curl -s -o /tmp/mcp-call-response.txt -w "%{http_code}" \
    -X POST \
    -H 'Content-Type: application/json' \
    -H 'Accept: application/json' \
    -d "$CALL_TOOL_PAYLOAD" \
    "$SERVER_URL/messages" 2>&1 || echo "000")

if [ "$HTTP_CODE" = "200" ]; then
    echo -e "${GREEN}✅ Call tool request returned 200 OK${NC}"
    echo "Response:"
    cat /tmp/mcp-call-response.txt | jq '.' 2>/dev/null || cat /tmp/mcp-call-response.txt
else
    echo -e "${RED}❌ Call tool request returned $HTTP_CODE${NC}"
    echo "Response:"
    cat /tmp/mcp-call-response.txt
fi
echo ""
echo ""

# Test 5: List resources (POST /messages)
echo -e "${YELLOW}Test 5: List Resources Request (POST /messages)${NC}"
LIST_RESOURCES_PAYLOAD='{
  "jsonrpc": "2.0",
  "id": 4,
  "method": "resources/list",
  "params": {}
}'

echo "Command: curl -X POST $SERVER_URL/messages [with resources/list payload]"
echo "---"
HTTP_CODE=$(curl -s -o /tmp/mcp-resources-response.txt -w "%{http_code}" \
    -X POST \
    -H 'Content-Type: application/json' \
    -H 'Accept: application/json' \
    -d "$LIST_RESOURCES_PAYLOAD" \
    "$SERVER_URL/messages" 2>&1 || echo "000")

if [ "$HTTP_CODE" = "200" ]; then
    echo -e "${GREEN}✅ List resources request returned 200 OK${NC}"
    echo "Response:"
    cat /tmp/mcp-resources-response.txt | jq '.result.resources[] | {uri: .uri, name: .name}' 2>/dev/null || cat /tmp/mcp-resources-response.txt
else
    echo -e "${RED}❌ List resources request returned $HTTP_CODE${NC}"
    echo "Response:"
    cat /tmp/mcp-resources-response.txt
fi
echo ""
echo ""

# Summary
echo "=================================================="
echo "Test Summary"
echo "=================================================="
echo ""
echo "Test files saved in /tmp/mcp-*-response.txt"
echo ""
echo "To connect with MCP Inspector:"
echo "  1. Open MCP Inspector in browser"
echo "  2. Add server with URL: $SERVER_URL/sse"
echo "  3. Test tools and resources interactively"
echo ""

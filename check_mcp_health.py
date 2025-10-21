#!/usr/bin/env python3
"""
Simple MCP client to check system health via HTTP SSE transport
Usage: python3 check_mcp_health.py [port]
"""

import sys
import json
import requests
import re
from urllib.parse import urlparse, parse_qs

def main():
    port = sys.argv[1] if len(sys.argv) > 1 else "5001"
    base_url = f"http://localhost:{port}"
    
    print(f"🔍 Connecting to MCP server at {base_url}...")
    print()
    
    # Connect to SSE endpoint
    try:
        sse_response = requests.get(
            f"{base_url}/sse",
            headers={"Accept": "text/event-stream"},
            stream=True,
            timeout=5
        )
        sse_response.raise_for_status()
    except requests.exceptions.RequestException as e:
        print(f"❌ Failed to connect: {e}")
        return 1
    
    # Read SSE stream to get session ID
    session_id = None
    for line in sse_response.iter_lines(decode_unicode=True):
        if line and line.startswith("data:"):
            data = line[5:].strip()
            # Extract session ID from endpoint URL
            match = re.search(r'sessionId=([a-zA-Z0-9_-]+)', data)
            if match:
                session_id = match.group(1)
                print(f"✅ Session established: {session_id}")
                print()
                break
    
    if not session_id:
        print("❌ Failed to get session ID from SSE")
        return 1
    
    # Make health check request
    print("📊 Requesting system health...")
    endpoint_url = f"{base_url}/message?sessionId={session_id}"
    
    payload = {
        "jsonrpc": "2.0",
        "id": 1,
        "method": "tools/call",
        "params": {
            "name": "get_system_health",
            "arguments": {}
        }
    }
    
    try:
        # Send request (it returns immediately with "Accepted")
        post_response = requests.post(
            endpoint_url,
            json=payload,
            headers={"Content-Type": "application/json", "Accept": "application/json"},
            timeout=5
        )
        print(f"Request status: {post_response.status_code}")
        print()
    except requests.exceptions.RequestException as e:
        print(f"❌ Request failed: {e}")
        return 1
    
    # Continue reading SSE stream for response
    print("📨 Reading response from SSE stream...")
    print()
    
    response_received = False
    for line in sse_response.iter_lines(decode_unicode=True):
        if not line:
            continue
            
        if line.startswith("event:"):
            event_type = line[6:].strip()
            if event_type == "message":
                response_received = True
        elif line.startswith("data:") and response_received:
            data = line[5:].strip()
            if data and data != "[DONE]":
                try:
                    response_json = json.loads(data)
                    
                    # Pretty print the health data
                    if "result" in response_json:
                        result = response_json["result"]
                        if isinstance(result, list) and len(result) > 0:
                            # MCP tool response format
                            content = result[0].get("content", [])
                            if content:
                                text_content = content[0].get("text", "")
                                if text_content:
                                    health_data = json.loads(text_content)
                                    print("=" * 60)
                                    print("SYSTEM HEALTH REPORT")
                                    print("=" * 60)
                                    print(json.dumps(health_data, indent=2))
                                    print("=" * 60)
                                    return 0
                    
                    # Fallback: print raw response
                    print(json.dumps(response_json, indent=2))
                    return 0
                except json.JSONDecodeError:
                    print(f"Response: {data}")
                    return 0
    
    print("⚠️  No response received (timeout)")
    return 1

if __name__ == "__main__":
    sys.exit(main())

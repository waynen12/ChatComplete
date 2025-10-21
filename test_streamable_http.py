#!/usr/bin/env python3
"""
Test MCP Streamable HTTP transport (2025-06-18 protocol)
Usage: python3 test_streamable_http.py [port]
"""

import sys
import json
import requests
import re

def test_streamable_http(port="5001"):
    base_url = f"http://localhost:{port}"
    
    print("=" * 70)
    print("MCP Streamable HTTP Test (Protocol 2025-06-18)")
    print("=" * 70)
    print(f"Server: {base_url}")
    print()
    
    # Step 1: Initialize
    print("Step 1: Initialize Session")
    print("-" * 70)
    
    init_request = {
        "jsonrpc": "2.0",
        "id": 1,
        "method": "initialize",
        "params": {
            "protocolVersion": "2025-06-18",
            "capabilities": {
                "roots": {"listChanged": False}
            },
            "clientInfo": {
                "name": "streamable-http-test",
                "version": "1.0.0"
            }
        }
    }
    
    headers = {
        "Content-Type": "application/json",
        "Accept": "application/json, text/event-stream",
        "MCP-Protocol-Version": "2025-06-18"
    }
    
    try:
        response = requests.post(
            f"{base_url}/",
            json=init_request,
            headers=headers,
            stream=True,
            timeout=10
        )
        response.raise_for_status()
        
        print(f"✅ Status: {response.status_code}")
        print(f"✅ Content-Type: {response.headers.get('Content-Type')}")
        
        session_id = response.headers.get('Mcp-Session-Id')
        if session_id:
            print(f"✅ Mcp-Session-Id: {session_id}")
        else:
            print("⚠️  No Mcp-Session-Id header (optional for stateless servers)")
        
        print()
        print("Initialize Response:")
        
        # Read SSE stream for response
        init_response = None
        for line in response.iter_lines(decode_unicode=True):
            if line.startswith("data: "):
                data = line[6:]
                if data and data != "[DONE]":
                    init_response = json.loads(data)
                    print(json.dumps(init_response, indent=2))
                    break
        
        if not init_response:
            print("❌ No initialize response received")
            return 1
            
    except Exception as e:
        print(f"❌ Initialize failed: {e}")
        return 1
    
    print()
    print("=" * 70)
    print("Step 2: Send Initialized Notification")
    print("-" * 70)
    
    initialized_notif = {
        "jsonrpc": "2.0",
        "method": "notifications/initialized"
    }
    
    notif_headers = {
        "Content-Type": "application/json",
        "Accept": "application/json, text/event-stream",
        "MCP-Protocol-Version": "2025-06-18"
    }
    
    if session_id:
        notif_headers["Mcp-Session-Id"] = session_id
    
    try:
        notif_response = requests.post(
            f"{base_url}/",
            json=initialized_notif,
            headers=notif_headers,
            timeout=5
        )
        
        if notif_response.status_code == 202:
            print(f"✅ Notification accepted: {notif_response.status_code}")
        else:
            print(f"⚠️  Unexpected status: {notif_response.status_code}")
            
    except Exception as e:
        print(f"❌ Notification failed: {e}")
    
    print()
    print("=" * 70)
    print("Step 3: Call get_system_health Tool")
    print("-" * 70)
    
    tool_request = {
        "jsonrpc": "2.0",
        "id": 2,
        "method": "tools/call",
        "params": {
            "name": "get_system_health",
            "arguments": {}
        }
    }
    
    tool_headers = {
        "Content-Type": "application/json",
        "Accept": "application/json, text/event-stream",
        "MCP-Protocol-Version": "2025-06-18"
    }
    
    if session_id:
        tool_headers["Mcp-Session-Id"] = session_id
    
    try:
        tool_response = requests.post(
            f"{base_url}/",
            json=tool_request,
            headers=tool_headers,
            stream=True,
            timeout=30
        )
        tool_response.raise_for_status()
        
        print(f"✅ Status: {tool_response.status_code}")
        print(f"✅ Content-Type: {tool_response.headers.get('Content-Type')}")
        print()
        print("System Health Response:")
        print()
        
        # Read SSE stream for response
        for line in tool_response.iter_lines(decode_unicode=True):
            if line.startswith("data: "):
                data = line[6:]
                if data and data != "[DONE]":
                    try:
                        response_json = json.loads(data)
                        
                        # Extract tool result
                        if "result" in response_json:
                            result = response_json["result"]
                            if isinstance(result, list) and len(result) > 0:
                                content = result[0].get("content", [])
                                if content and isinstance(content, list):
                                    text_content = content[0].get("text", "")
                                    if text_content:
                                        health_data = json.loads(text_content)
                                        print(json.dumps(health_data, indent=2))
                                        break
                        
                        # Fallback: print raw response
                        print(json.dumps(response_json, indent=2))
                        break
                        
                    except json.JSONDecodeError:
                        print(data)
                        break
        
        print()
        print("=" * 70)
        print("✅ Test Complete!")
        print("=" * 70)
        return 0
        
    except Exception as e:
        print(f"❌ Tool call failed: {e}")
        import traceback
        traceback.print_exc()
        return 1

if __name__ == "__main__":
    port = sys.argv[1] if len(sys.argv) > 1 else "5001"
    sys.exit(test_streamable_http(port))

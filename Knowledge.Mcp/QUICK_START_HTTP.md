# Quick Start: MCP HTTP Server

## 🚀 Start Server

```bash
cd /home/wayne/repos/ChatComplete

# Full server (11 tools, 6 resources)
dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj -- --http

# Minimal test server (2 tools)
dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj -- --minimal-http
```

## ✅ Test Endpoints

```bash
cd Knowledge.Mcp
./test-http-endpoints.sh
```

## 🔍 Connect MCP Inspector

1. Open MCP Inspector in browser
2. Add server: `http://localhost:5000/sse`
3. Test tools and resources

## 🐛 Debug Errors

Watch server console for:
```
❌ Unhandled exception in HTTP request:
   Path: /messages
   Method: POST
   Error: [error message]
   Stack: [stack trace]
```

## 📊 Check Dependencies

```bash
# Qdrant
curl http://localhost:6333/health

# Ollama  
curl http://localhost:11434/api/tags

# Database
ls -lh ~/repos/ChatComplete/Knowledge.Api/bin/Debug/net8.0/linux-x64/data/knowledge.db
```

## 🔧 What Was Fixed

1. ✅ **Added CORS** - Web clients can connect
2. ✅ **Fixed middleware order** - Endpoints now work
3. ✅ **Added exception handling** - Errors are visible
4. ✅ **Explicit URL binding** - Listens on all interfaces

## 📖 Full Documentation

See `MCP_HTTP_REVIEW_AND_FIXES.md` for complete details.

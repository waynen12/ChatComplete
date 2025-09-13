# Health Check Debugging Guide

When Qdrant and Ollama health checks fail, use these commands to diagnose the issues:

## 🩺 Quick Diagnostics

### 1. Check Container Status
```bash
# See all container statuses
docker-compose -f docker-compose.dockerhub.yml ps

# Check specific container logs
docker-compose -f docker-compose.dockerhub.yml logs qdrant
docker-compose -f docker-compose.dockerhub.yml logs ollama
docker-compose -f docker-compose.dockerhub.yml logs ai-knowledge-manager
```

### 2. Test Health Check Commands Manually
```bash
# Test Qdrant health check inside container
docker exec qdrant curl -f http://localhost:6333/health

# Test if curl is available in Qdrant container
docker exec qdrant which curl
docker exec qdrant ls /usr/bin/ | grep curl

# Test Ollama health check inside container  
docker exec ollama curl -f http://localhost:11434/api/version

# Test if curl is available in Ollama container
docker exec ollama which curl
```

### 3. Check Network Connectivity
```bash
# Test network connectivity between containers
docker exec ai-knowledge-manager ping qdrant
docker exec ai-knowledge-manager ping ollama

# Check if ports are listening
docker exec qdrant netstat -tlnp | grep 6333
docker exec ollama netstat -tlnp | grep 11434
```

### 4. Check Container Resource Usage
```bash
# Monitor resource usage
docker stats qdrant ollama ai-knowledge-manager

# Check container startup time
docker-compose -f docker-compose.dockerhub.yml logs --timestamps qdrant
docker-compose -f docker-compose.dockerhub.yml logs --timestamps ollama
```

## 🔧 Common Issues & Fixes

### Issue 1: curl not available in containers
**Symptoms:** `curl: command not found` errors
**Solution:** Use alternative health check commands

### Issue 2: Services not ready during health check
**Symptoms:** Connection refused errors
**Solution:** Increase `start_period` in health checks

### Issue 3: Wrong health check endpoints
**Symptoms:** 404 errors from health endpoints
**Solution:** Use correct API endpoints

### Issue 4: Container startup delays
**Symptoms:** Services healthy but AI Knowledge Manager can't connect
**Solution:** Adjust dependency timing

## 🛠️ Manual Service Testing

### Test Qdrant Directly
```bash
# Check if Qdrant is responding
curl http://localhost:6333/
curl http://localhost:6333/collections

# Create a test collection
curl -X PUT http://localhost:6333/collections/test \
  -H "Content-Type: application/json" \
  -d '{"vectors": {"size": 768, "distance": "Cosine"}}'
```

### Test Ollama Directly  
```bash
# Check Ollama API
curl http://localhost:11434/api/version
curl http://localhost:11434/api/tags

# Pull a small model for testing
curl -X POST http://localhost:11434/api/pull \
  -H "Content-Type: application/json" \
  -d '{"name": "tinyllama"}'
```

### Test AI Knowledge Manager
```bash
# Check main application
curl http://localhost:8080/api/ping
curl http://localhost:8080/swagger

# Test knowledge upload (after services are healthy)
curl -X POST http://localhost:8080/api/knowledge \
  -F "files=@test.txt" \
  -F "knowledgeId=test-docs"
```

## 🔍 Log Analysis Tips

### Look for these patterns in logs:

**Qdrant logs:**
- `INFO qdrant: Qdrant HTTP listening on 6333`
- `INFO qdrant: gRPC listening on 6334`
- `ERROR` messages about storage or configuration

**Ollama logs:**  
- `time=... level=INFO msg="Listening on 127.0.0.1:11434"`
- `time=... level=INFO msg="Available"`
- Model loading progress: `time=... msg="loading model"`

**AI Knowledge Manager logs:**
- `Application started. Press Ctrl+C to shut down.`
- Vector store connection errors
- Semantic Kernel initialization messages

## 📋 Debugging Checklist

- [ ] All containers are running (`docker ps`)
- [ ] Container logs show successful startup
- [ ] Health check commands work manually
- [ ] Network connectivity between containers works
- [ ] Ports are correctly mapped and listening
- [ ] No resource constraints (CPU/memory)
- [ ] Correct environment variables are set
- [ ] Volume mounts are working
- [ ] No firewall blocking container communication
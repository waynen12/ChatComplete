# Docker Deployment Guide
## AI Knowledge Manager - One Container, Complete Solution

### 🚀 Quick Start

**Option 1: Full Stack with Qdrant (Recommended)**
```bash
# 1. Set up environment variables
cp .env.example .env
# Edit .env with your API keys

# 2. Start the complete stack
docker-compose up -d

# 3. Access the application
# Frontend: http://localhost:8080
# API: http://localhost:8080/api/knowledge
# Swagger: http://localhost:8080/docs
```

**Option 2: Standalone with MongoDB Atlas**
```bash
# 1. Set up environment variables (including MongoDB connection)
cp .env.example .env
# Edit .env with your API keys and MongoDB connection string

# 2. Start standalone container
docker-compose -f docker-compose.standalone.yml up -d

# 3. Access at http://localhost:8080
```

### 🐳 Manual Docker Commands

**Build the image:**
```bash
docker build -t ai-knowledge-manager .
```

**Run standalone container:**
```bash
docker run -d \
  --name ai-knowledge-manager \
  -p 8080:7040 \
  -v ai-knowledge-data:/app/data \
  -e OPENAI_API_KEY=your_key_here \
  -e MONGODB_CONNECTION_STRING=your_mongodb_uri \
  ai-knowledge-manager
```

### 🔍 Health Checks & Monitoring

**Check container health:**
```bash
# Basic health check
curl http://localhost:8080/api/ping

# Comprehensive health status
curl http://localhost:8080/api/health
```

**View logs:**
```bash
# Docker Compose
docker-compose logs -f ai-knowledge-manager

# Docker run
docker logs -f ai-knowledge-manager
```

### 📂 Data Persistence

Data is stored in Docker volumes:
- **ai-knowledge-data**: Application data, configurations, uploaded documents
- **qdrant-data**: Vector database storage (if using Qdrant)

**Backup data:**
```bash
# Backup application data
docker run --rm -v ai-knowledge-data:/data -v $(pwd):/backup alpine \
  tar czf /backup/ai-knowledge-backup-$(date +%Y%m%d).tar.gz -C /data .

# Backup Qdrant data
docker run --rm -v qdrant-data:/data -v $(pwd):/backup alpine \
  tar czf /backup/qdrant-backup-$(date +%Y%m%d).tar.gz -C /data .
```

**Restore data:**
```bash
# Restore application data
docker run --rm -v ai-knowledge-data:/data -v $(pwd):/backup alpine \
  tar xzf /backup/ai-knowledge-backup-YYYYMMDD.tar.gz -C /data
```

### 🔧 Configuration

**Environment Variables:**
- `OPENAI_API_KEY` - Required for embeddings and chat
- `ANTHROPIC_API_KEY` - Optional for Claude models
- `GEMINI_API_KEY` - Optional for Gemini models
- `MONGODB_CONNECTION_STRING` - Required for standalone mode
- `VectorStore__Provider` - "MongoDB" or "Qdrant" (default: Qdrant)

**Volume Mounts:**
- `/app/data` - Persistent application data
- `/app/data/logs` - Application logs (optional mount)

### 🐛 Troubleshooting

**Container won't start:**
```bash
# Check container logs
docker logs ai-knowledge-manager

# Verify environment variables
docker exec ai-knowledge-manager env | grep -E "(OPENAI|MONGODB)"
```

**Frontend not loading:**
```bash
# Verify static files are served
curl -I http://localhost:8080/

# Check if API is responding
curl http://localhost:8080/api/ping
```

**Database connection issues:**
```bash
# Test MongoDB connection (if using standalone)
curl http://localhost:8080/api/health

# Test Qdrant connection (if using full stack)
curl http://localhost:6333/health
```

**Reset everything:**
```bash
# Stop and remove containers
docker-compose down

# Remove volumes (WARNING: deletes all data)
docker volume rm ai-knowledge-data qdrant-data

# Start fresh
docker-compose up -d
```

### 🔄 Updates

**Update to latest version:**
```bash
# Pull latest changes
git pull

# Rebuild and restart
docker-compose down
docker-compose up -d --build
```

### 📊 Production Deployment

**Resource Requirements:**
- **Memory**: Minimum 2GB, Recommended 4GB
- **CPU**: Minimum 1 core, Recommended 2 cores
- **Storage**: Minimum 10GB for application data
- **Network**: Port 8080 accessible

**Production Docker Compose:**
```yaml
version: '3.8'
services:
  ai-knowledge-manager:
    image: ai-knowledge-manager:latest
    restart: always
    ports:
      - "80:7040"  # Production port
    volumes:
      - /opt/ai-knowledge-data:/app/data
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    deploy:
      resources:
        limits:
          memory: 4G
          cpus: '2'
```

### 🎯 Success Verification

1. **Container Health**: `curl http://localhost:8080/api/health` returns 200
2. **Frontend Loading**: Browser shows React interface at `http://localhost:8080`
3. **API Functional**: `curl http://localhost:8080/api/knowledge` returns JSON
4. **Document Upload**: Can upload and process documents through UI
5. **Chat Working**: Can send messages and receive AI responses

---

**🎉 That's it! You now have a complete RAG system running in Docker!**
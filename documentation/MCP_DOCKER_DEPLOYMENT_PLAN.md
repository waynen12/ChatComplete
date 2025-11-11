# MCP Docker Deployment Integration Plan - REVISED

## Answers to Your Questions

### 1. React Frontend in Docker - ✅ CONFIRMED
**Current container DOES include React frontend**:
```bash
/app/wwwroot/
├── assets/
│   ├── index-D7pUnzIy.css (36KB)
│   └── index-DxSRdvff.js (671KB)
├── index.html
└── vite.svg
```

The multi-stage Dockerfile (lines 1-51) already:
- Stage 1: Builds React frontend with Node 20
- Stage 2: Builds .NET backend
- Stage 3: Copies both frontend (→ wwwroot) and backend to runtime container

**No additional work needed** - your Docker implementation is already complete!

### 2. Multi-Architecture Support - REMOVED
ARM64 support (Section 5.1) has been removed from the plan per your request.

### 3. CI/CD Deployment Strategy - REVISED
**New Priority**: Deploy MCP to test machine FIRST, then Docker integration

**Deployment Order**:
1. **Phase 1** (Week 1): Self-hosted runner → MCP to test machine
2. **Phase 2** (Week 2): Docker integration after testing MCP deployment
3. **Phase 3** (Week 3): Docker Hub + documentation

This ensures MCP is battle-tested on your network before containerization.

---

## Executive Summary

Integrate Knowledge.Mcp server into existing deployment infrastructure with focus on self-hosted testing first.

### Deployment Modes (Priority Order)
1. **Self-Hosted Test Machine** ✅ PRIMARY FOCUS (Week 1)
   - Deploy alongside Knowledge.Api on network test machine
   - Validate MCP functionality in production-like environment
   - Test database sharing and service integration

2. **Docker Sidecar Container** (Week 2)
   - Separate container running alongside Knowledge.Api
   - Shared volume for SQLite database
   - Optional deployment for Docker users

3. **Docker Hub Distribution** (Week 3)
   - Automated builds via GitHub Actions
   - Public image: `waynen12/ai-knowledge-manager-mcp:latest`

---

## Phase 1: Self-Hosted Test Machine Deployment (Week 1) 🎯 PRIMARY FOCUS

### Current Self-Hosted Setup
**From `.github/workflows/deploy-self.yml`**:
- **Runner**: `knowledge-runner` (self-hosted on test machine)
- **Deploy Path**: `/opt/knowledge-api/out`
- **Systemd Service**: `knowledge-api.service`
- **User**: `chatapi`
- **Qdrant**: Managed via `docker-compose.qdrant.yml`

### 1.1 Update deploy-self.yml Workflow

**Add MCP deployment steps after Knowledge.Api deployment**:

```yaml
jobs:
  deploy:
    runs-on: self-hosted
    timeout-minutes: 20

    steps:
      # ... existing steps (checkout, qdrant, publish Knowledge.Api, restart service)

      # ========== NEW: MCP Server Deployment ==========

      - name: Create MCP directories
        run: |
          sudo mkdir -p /opt/knowledge-mcp/out
          sudo chown chatapi:chatapi /opt/knowledge-mcp
          sudo chown chatapi:chatapi /opt/knowledge-mcp/out

      - name: Clean MCP publish folder
        run: rm -rf /opt/knowledge-mcp/out/*

      - name: Publish Knowledge.Mcp
        run: |
          cd Knowledge.Mcp
          dotnet publish -c Release -r linux-x64 \
                         --self-contained true \
                         -o /opt/knowledge-mcp/out
        # Self-contained: includes .NET runtime (no dependency on system .NET)

      - name: Create MCP systemd service
        run: |
          if [ ! -f /etc/systemd/system/knowledge-mcp.service ]; then
            echo "Creating knowledge-mcp.service..."
            sudo tee /etc/systemd/system/knowledge-mcp.service > /dev/null <<'EOF'
          [Unit]
          Description=Knowledge Manager MCP Server
          After=network.target knowledge-api.service
          Requires=knowledge-api.service

          [Service]
          Type=notify
          User=chatapi
          WorkingDirectory=/opt/knowledge-mcp/out
          ExecStart=/opt/knowledge-mcp/out/Knowledge.Mcp --http
          Restart=on-failure
          RestartSec=10
          KillMode=process

          # Environment
          Environment="ASPNETCORE_ENVIRONMENT=Production"
          Environment="ASPNETCORE_URLS=http://+:5001"
          Environment="DOTNET_RUNNING_IN_CONTAINER=false"

          [Install]
          WantedBy=multi-user.target
          EOF
            sudo systemctl daemon-reload
            sudo systemctl enable knowledge-mcp.service
            echo "Service created and enabled"
          else
            echo "Service already exists, will just restart"
          fi

      - name: Restart MCP service
        run: |
          sudo systemctl restart knowledge-mcp.service
          sleep 5
          sudo systemctl --no-pager status knowledge-mcp.service

      - name: Verify MCP health
        run: |
          echo "Waiting for MCP server to start..."
          for i in {1..30}; do
            if curl -f http://localhost:5001/health 2>/dev/null; then
              echo "✅ MCP server is healthy"
              exit 0
            fi
            echo "Attempt $i/30: MCP not ready yet..."
            sleep 2
          done
          echo "❌ MCP server failed to start"
          sudo journalctl -u knowledge-mcp.service --no-pager -n 50
          exit 1
```

**Key Design Decisions**:
- **`Requires=knowledge-api.service`**: MCP won't start if API fails
- **`After=knowledge-api.service`**: Ensures database is initialized first
- **Same user (`chatapi`)**: Shared access to SQLite database
- **Self-contained publish**: No .NET runtime installation needed
- **`--http` flag**: Enables SSE transport for remote clients
- **Port 5001**: Default MCP HTTP port (configurable)

### 1.2 Configuration for Test Machine

**Update Knowledge.Mcp/appsettings.json**:
```json
{
  "McpServerSettings": {
    "HttpTransport": {
      "Host": "0.0.0.0",
      "Port": 5001,
      "Cors": {
        "Enabled": true,
        "AllowedOrigins": [
          "http://localhost:3000",
          "http://localhost:5173",
          "http://192.168.50.50",           // Your test machine IP
          "http://192.168.50.50:8080",      // If Knowledge.Api is on 8080
          "https://copilot.github.com",
          "https://claude.ai"
        ]
      }
    },
    "General": {
      "OllamaBaseUrl": "http://localhost:11434"  // Ollama on same machine
    }
  },
  "ChatCompleteSettings": {
    "DatabasePath": "/opt/knowledge-api/data/knowledge.db",  // SHARED with API
    "VectorStore": {
      "Provider": "Qdrant",
      "Qdrant": {
        "Host": "localhost",  // Qdrant from docker-compose.qdrant.yml
        "Port": 6334
      }
    }
  }
}
```

**Critical Configuration Notes**:
- **Shared Database Path**: MCP must point to same SQLite file as API
- **Localhost Services**: Qdrant and Ollama run on same machine (not in containers)
- **CORS Origins**: Add your test machine's IP address

### 1.3 Test Machine Validation Checklist

**After deployment, run these tests**:

1. **Service Status Check**
   ```bash
   # On test machine
   sudo systemctl status knowledge-api.service
   sudo systemctl status knowledge-mcp.service
   ```

2. **Port Verification**
   ```bash
   sudo ss -tlnp | grep -E ':(7040|5001)'
   # Should show:
   # 7040 - Knowledge.Api
   # 5001 - Knowledge.Mcp
   ```

3. **Health Check**
   ```bash
   curl http://localhost:5001/health
   # Expected: {"status":"healthy", ...}
   ```

4. **MCP Tools Discovery**
   ```bash
   curl -X POST http://localhost:5001/messages \
     -H "Content-Type: application/json" \
     -d '{
       "jsonrpc": "2.0",
       "id": 1,
       "method": "tools/list"
     }'
   # Expected: List of 11 tools
   ```

5. **Database Sharing Test**
   ```bash
   # Upload document via API
   curl -F "file=@test.pdf" http://localhost:7040/api/knowledge

   # Search via MCP (should find the document)
   curl -X POST http://localhost:5001/messages \
     -H "Content-Type: application/json" \
     -d '{
       "jsonrpc": "2.0",
       "id": 2,
       "method": "tools/call",
       "params": {
         "name": "search_all_knowledge",
         "arguments": {"query": "test document content"}
       }
     }'
   ```

6. **Log Inspection**
   ```bash
   # Check for errors
   sudo journalctl -u knowledge-mcp.service -f
   ```

### 1.4 Firewall Configuration (if needed)

**If accessing MCP from external network**:
```bash
# On test machine
sudo firewall-cmd --permanent --add-port=5001/tcp
sudo firewall-cmd --reload

# Or with UFW
sudo ufw allow 5001/tcp
```

---

## Phase 2: Docker Sidecar Integration (Week 2)

**Only proceed after Phase 1 validation is complete**

### 2.1 Create Knowledge.Mcp/Dockerfile.mcp

**Location**: `/home/wayne/repos/ChatComplete/Knowledge.Mcp/Dockerfile.mcp`

```dockerfile
# Multi-stage Dockerfile for Knowledge.Mcp
# Stage 1: Backend Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy solution and project files
COPY *.sln ./
COPY Knowledge.Mcp/*.csproj ./Knowledge.Mcp/
COPY Knowledge.Analytics/*.csproj ./Knowledge.Analytics/
COPY Knowledge.Data/*.csproj ./Knowledge.Data/
COPY Knowledge.Entities/*.csproj ./Knowledge.Entities/
COPY Knowledge.Contracts/*.csproj ./Knowledge.Contracts/
COPY KnowledgeEngine/*.csproj ./KnowledgeEngine/

# Restore dependencies
RUN dotnet restore Knowledge.Mcp/Knowledge.Mcp.csproj

# Copy source code
COPY . ./

# Build and publish
RUN dotnet publish Knowledge.Mcp/Knowledge.Mcp.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copy published application
COPY --from=build /app/publish ./

# Create data directory for shared SQLite database
RUN mkdir -p /app/data && chmod 755 /app/data

# Create non-root user (UID 1002 to differentiate from API's 1001)
RUN groupadd --gid 1002 mcpgroup && \
    useradd --uid 1002 --gid mcpgroup --shell /bin/bash --create-home mcpuser && \
    chown -R mcpuser:mcpgroup /app

USER mcpuser

# Expose MCP HTTP port
EXPOSE 5001

# Health check endpoint
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:5001/health || exit 1

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:5001 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    DOTNET_RUNNING_IN_CONTAINER=true

# Create volume for persistent data (shared with Knowledge.Api)
VOLUME ["/app/data"]

# Start with HTTP mode (STDIO mode only for Claude Desktop)
ENTRYPOINT ["dotnet", "Knowledge.Mcp.dll", "--http"]
```

### 2.2 Update docker-compose.dockerhub.yml

**Add MCP service (after ai-knowledge-manager service)**:

```yaml
  # NEW: MCP Server for remote tool access via HTTP transport
  mcp-server:
    image: waynen12/ai-knowledge-manager-mcp:latest
    container_name: mcp-server
    ports:
      - "5001:5001"  # MCP HTTP transport
    volumes:
      - ai-knowledge-data:/app/data  # SHARED with ai-knowledge-manager
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - DOTNET_RUNNING_IN_CONTAINER=true
      - ChatCompleteSettings__DatabasePath=/app/data/knowledge.db
      - ChatCompleteSettings__VectorStore__Provider=Qdrant
      - ChatCompleteSettings__VectorStore__Qdrant__Host=qdrant
      - ChatCompleteSettings__VectorStore__Qdrant__Port=6334
      - McpServerSettings__General__OllamaBaseUrl=http://ollama:11434
      - McpServerSettings__HttpTransport__Host=0.0.0.0
      - McpServerSettings__HttpTransport__Port=5001
      # OAuth settings (Milestone #23)
      - McpServerSettings__HttpTransport__OAuth__Enabled=false
    depends_on:
      ai-knowledge-manager:
        condition: service_healthy
      qdrant:
        condition: service_healthy
      ollama:
        condition: service_healthy
    healthcheck:
      test: ["CMD-SHELL", "timeout 5s bash -c '</dev/tcp/localhost/5001' || exit 1"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 60s
    restart: unless-stopped
    networks:
      - ai-services
```

**Key Design Decisions**:
- **Shared Volume**: `ai-knowledge-data:/app/data` allows SQLite database access
- **Startup Order**: MCP depends on API to ensure database initialization
- **Container Networking**: Uses service names (qdrant, ollama) not localhost
- **Optional**: Users can comment out MCP service for API-only deployment

### 2.3 Create docker-compose.mcp-only.yml

**Standalone MCP deployment**:

```yaml
version: '3.8'

services:
  mcp-server:
    image: waynen12/ai-knowledge-manager-mcp:latest
    container_name: mcp-server-standalone
    ports:
      - "5001:5001"
    volumes:
      - mcp-data:/app/data
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - DOTNET_RUNNING_IN_CONTAINER=true
      - ChatCompleteSettings__DatabasePath=/app/data/knowledge.db
      - ChatCompleteSettings__VectorStore__Provider=Qdrant
      - ChatCompleteSettings__VectorStore__Qdrant__Host=qdrant
      - ChatCompleteSettings__VectorStore__Qdrant__Port=6334
      - McpServerSettings__General__OllamaBaseUrl=http://ollama:11434
      - OPENAI_API_KEY=${OPENAI_API_KEY:-}
      - ANTHROPIC_API_KEY=${ANTHROPIC_API_KEY:-}
      - GEMINI_API_KEY=${GEMINI_API_KEY:-}
    depends_on:
      qdrant:
        condition: service_healthy
      ollama:
        condition: service_healthy
    healthcheck:
      test: ["CMD-SHELL", "timeout 5s bash -c '</dev/tcp/localhost/5001' || exit 1"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 60s
    restart: unless-stopped
    networks:
      - mcp-network

  qdrant:
    image: qdrant/qdrant:latest
    container_name: qdrant-mcp
    ports:
      - "6333:6333"
      - "6334:6334"
    volumes:
      - qdrant-data:/qdrant/storage
    environment:
      - QDRANT__SERVICE__HTTP_PORT=6333
      - QDRANT__SERVICE__GRPC_PORT=6334
      - QDRANT__LOG_LEVEL=INFO
    networks:
      - mcp-network
    healthcheck:
      test: ["CMD-SHELL", "timeout 5s bash -c '</dev/tcp/localhost/6333' || exit 1"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
    restart: unless-stopped

  ollama:
    image: ollama/ollama:latest
    container_name: ollama-mcp
    ports:
      - "11434:11434"
    volumes:
      - ollama-data:/root/.ollama
    environment:
      - OLLAMA_ORIGINS=*
    networks:
      - mcp-network
    healthcheck:
      test: ["CMD-SHELL", "timeout 5s bash -c '</dev/tcp/localhost/11434' || exit 1"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 120s
    restart: unless-stopped

volumes:
  mcp-data:
    driver: local
    name: mcp-server-data
  qdrant-data:
    driver: local
    name: mcp-qdrant-data
  ollama-data:
    driver: local
    name: mcp-ollama-data

networks:
  mcp-network:
    driver: bridge
    name: mcp-network
```

---

## Phase 3: Docker Hub & Documentation (Week 3)

### 3.1 Update .github/workflows/docker-build.yml

**Add MCP image build job**:

```yaml
name: Docker Build & Push

on:
  push:
    branches: [main]
    tags: ['v*']
  workflow_dispatch:

jobs:
  # Existing job: build-and-push (Knowledge.Api)

  build-and-push-mcp:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Set up Docker Buildx
        uses: docker/setup-buildx-action@v3

      - name: Login to Docker Hub
        uses: docker/login-action@v3
        with:
          username: ${{ secrets.DOCKERHUB_USERNAME }}
          password: ${{ secrets.DOCKERHUB_TOKEN }}

      - name: Extract metadata
        id: meta
        uses: docker/metadata-action@v5
        with:
          images: waynen12/ai-knowledge-manager-mcp
          tags: |
            type=ref,event=branch
            type=ref,event=tag
            type=raw,value=latest,enable={{is_default_branch}}

      - name: Build and push MCP image
        uses: docker/build-push-action@v5
        with:
          context: .
          file: ./Knowledge.Mcp/Dockerfile.mcp
          push: true
          tags: ${{ steps.meta.outputs.tags }}
          labels: ${{ steps.meta.outputs.labels }}
          platforms: linux/amd64  # Single architecture per your request
          cache-from: type=gha
          cache-to: type=gha,mode=max
```

### 3.2 Update CLAUDE.md

**Add MCP Docker deployment section**:

```markdown
## 🐳 MCP Server Deployment (Milestone #22)

### Self-Hosted Deployment (Test Machine)

**Automatic via GitHub Actions**:
- Deploys to `/opt/knowledge-mcp/out`
- Runs as systemd service: `knowledge-mcp.service`
- Shares SQLite database with Knowledge.Api
- Accessible at `http://<test-machine-ip>:5001`

**Manual deployment**:
```bash
cd Knowledge.Mcp
dotnet publish -c Release -r linux-x64 --self-contained -o /opt/knowledge-mcp/out
sudo systemctl restart knowledge-mcp.service
```

### Docker Deployment

**Full Stack with MCP (Recommended)**:
```bash
curl -O https://raw.githubusercontent.com/waynen12/ChatComplete/main/docker-compose.dockerhub.yml
docker-compose -f docker-compose.dockerhub.yml up -d

# Access:
# - Main App: http://localhost:8080
# - MCP Server: http://localhost:5001
```

**MCP-Only Deployment**:
```bash
curl -O https://raw.githubusercontent.com/waynen12/ChatComplete/main/docker-compose.mcp-only.yml
docker-compose -f docker-compose.mcp-only.yml up -d
```

### Connecting Clients

**GitHub Copilot** (.github/copilot-mcp-settings.json):
```json
{
  "mcpServers": {
    "knowledge-manager": {
      "url": "http://localhost:5001"
    }
  }
}
```

**MCP Inspector**:
```bash
npx @anthropic/mcp-inspector http://localhost:5001
```

**Custom Client**:
```bash
# Initialize connection
curl -X POST http://localhost:5001/messages \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "initialize",
    "params": {
      "protocolVersion": "2024-11-05",
      "capabilities": {},
      "clientInfo": {"name": "test-client", "version": "1.0.0"}
    }
  }'
```
```

### 3.3 Create MCP Docker Testing Guide

**File**: `documentation/MCP_DOCKER_TESTING.md`

```markdown
# MCP Docker Testing Guide

## Local Build Testing

### Build MCP Image
```bash
docker build -t mcp-test -f Knowledge.Mcp/Dockerfile.mcp .
```

### Test Standalone
```bash
docker run -p 5001:5001 \
  -e OPENAI_API_KEY=$OPENAI_API_KEY \
  -e ChatCompleteSettings__VectorStore__Provider=Qdrant \
  -e ChatCompleteSettings__VectorStore__Qdrant__Host=host.docker.internal \
  mcp-test
```

### Test with Docker Compose
```bash
# Development build
docker-compose -f docker-compose.full-stack-mcp.yml up --build

# Production images
docker-compose -f docker-compose.dockerhub.yml up -d
```

## Health Check Verification

### Check Services
```bash
docker-compose ps
curl http://localhost:8080/api/ping  # API
curl http://localhost:5001/health    # MCP
```

### View Logs
```bash
docker-compose logs -f mcp-server
docker-compose logs -f ai-knowledge-manager
```

## Tool Execution Tests

### List Tools
```bash
curl -X POST http://localhost:5001/messages \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "tools/list"
  }' | jq
```

### Execute Search Tool
```bash
curl -X POST http://localhost:5001/messages \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 2,
    "method": "tools/call",
    "params": {
      "name": "search_all_knowledge",
      "arguments": {"query": "docker deployment"}
    }
  }' | jq
```

## Database Sharing Verification

### Upload via API
```bash
curl -F "file=@test.pdf" http://localhost:8080/api/knowledge
```

### Search via MCP
```bash
curl -X POST http://localhost:5001/messages \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 3,
    "method": "tools/call",
    "params": {
      "name": "get_knowledge_base_summary",
      "arguments": {}
    }
  }' | jq
```

## Troubleshooting

### Container Won't Start
```bash
docker logs mcp-server
docker inspect mcp-server
```

### Database Access Issues
```bash
# Check volume permissions
docker exec mcp-server ls -la /app/data
docker exec ai-knowledge-manager ls -la /app/data
```

### Network Issues
```bash
# Test connectivity between containers
docker exec mcp-server ping qdrant
docker exec mcp-server curl http://qdrant:6333
```
```

---

## Architecture Diagrams

### Current State (Self-Hosted Test Machine)
```
┌─────────────────┐
│  Test Machine   │
│  192.168.50.50  │
├─────────────────┤
│                 │
│  :7040          │  :5001
│  ┌─────────┐    │  ┌─────────┐
│  │   API   │    │  │   MCP   │
│  └────┬────┘    │  └────┬────┘
│       │         │       │
│       └─────────┴───────┘
│              │
│         SQLite DB
│         /opt/knowledge-api/data/
│
│  :6334         :11434
│  ┌─────────┐   ┌─────────┐
│  │ Qdrant  │   │ Ollama  │
│  │(Docker) │   │(Docker) │
│  └─────────┘   └─────────┘
└─────────────────┘
```

### Target State (Docker Full Stack)
```
┌─────────────┐        ┌─────────────────┐
│   Browser   │        │  GitHub Copilot │
└──────┬──────┘        │  MCP Inspector  │
       │ :8080         └────────┬────────┘
       v                        │ :5001
┌─────────────────────┐         v
│  ai-knowledge-mgr   │  ┌─────────────────┐
│  (Knowledge.Api)    │  │   mcp-server    │
└──────┬──────────────┘  │ (Knowledge.Mcp) │
       │                 └────────┬────────┘
       v                          │
  ┌─────────────┐                 │
  │ SQLite DB   │◄────────────────┘
  │ (shared)    │      (shared volume)
  └──────┬──────┘
         │
         v
  ┌────────┐ ┌────────┐
  │ Qdrant │ │ Ollama │
  └────────┘ └────────┘

All containers in: ai-services-network
```

---

## Implementation Timeline (REVISED)

### Week 1: Self-Hosted Deployment 🎯 PRIMARY FOCUS
**Goal**: Deploy and test MCP on network test machine

- [ ] Day 1: Update `.github/workflows/deploy-self.yml`
  - Add MCP publish steps
  - Create systemd service
  - Add health checks

- [ ] Day 2-3: First Deployment
  - Push changes to trigger workflow
  - Monitor deployment logs
  - Verify services running

- [ ] Day 4: Testing & Validation
  - Run all validation tests (section 1.3)
  - Test database sharing
  - Test MCP tool execution
  - Test from external clients (Copilot, MCP Inspector)

- [ ] Day 5: Troubleshooting & Documentation
  - Fix any issues discovered
  - Document any network-specific configuration
  - Update CLAUDE.md with test machine details

### Week 2: Docker Integration
**Goal**: Containerize MCP after validating on test machine

- [ ] Day 6-7: Docker Implementation
  - Create `Dockerfile.mcp`
  - Update `docker-compose.dockerhub.yml`
  - Create `docker-compose.mcp-only.yml`

- [ ] Day 8-9: Local Docker Testing
  - Build and test MCP image locally
  - Test full stack with docker-compose
  - Verify shared volume and database access

- [ ] Day 10: Documentation
  - Create MCP_DOCKER_TESTING.md
  - Update README.md

### Week 3: CI/CD & Distribution
**Goal**: Automate Docker builds and publish to Docker Hub

- [ ] Day 11-12: GitHub Actions
  - Update `docker-build.yml`
  - Test automated builds
  - Push to Docker Hub

- [ ] Day 13-14: Final Testing
  - Test Docker Hub images
  - End-to-end integration tests
  - Performance testing

- [ ] Day 15: Documentation Finalization
  - Update CLAUDE.md
  - Architecture diagrams
  - User guides

---

## Success Criteria

### Phase 1 (Self-Hosted) ✅ MUST COMPLETE FIRST
- [ ] MCP service deploys via GitHub Actions
- [ ] MCP service starts and passes health check
- [ ] All 11 MCP tools accessible from network
- [ ] SQLite database shared successfully with API
- [ ] Can upload via API and search via MCP
- [ ] External clients (Copilot) can connect

### Phase 2 (Docker)
- [ ] MCP Docker image builds successfully (< 500MB)
- [ ] MCP container starts and passes health check
- [ ] Shared volume works correctly
- [ ] Container networking (Qdrant, Ollama) working

### Phase 3 (CI/CD)
- [ ] GitHub Actions builds and pushes images automatically
- [ ] Docker Hub images available publicly
- [ ] Documentation complete and verified
- [ ] Zero-downtime deployment possible

---

## Risk Assessment & Mitigation

### Phase 1 Risks (Self-Hosted)
1. **Port 5001 Already in Use**
   - **Mitigation**: Check with `ss -tlnp | grep 5001` before deployment
   - **Alternative**: Configure different port in appsettings.json

2. **Database Permission Issues**
   - **Mitigation**: Both services run as same user (chatapi)
   - **Test**: Verify file permissions on `/opt/knowledge-api/data/`

3. **Service Startup Order**
   - **Mitigation**: systemd `After=` and `Requires=` directives
   - **Test**: Stop API and verify MCP stops too

### Phase 2 Risks (Docker)
1. **Volume Permission Conflicts**
   - **Mitigation**: Different UIDs for API (1001) and MCP (1002)
   - **Test**: File creation tests from both containers

2. **Network Discovery Issues**
   - **Mitigation**: Explicit service names in environment variables
   - **Test**: Ping tests between containers

---

## Next Steps

**Ready to begin implementation tomorrow**:
1. Start with Phase 1 (Self-Hosted Deployment)
2. Update `deploy-self.yml` with MCP deployment steps
3. Push to `main` branch to trigger workflow
4. Monitor deployment on test machine
5. Run validation tests

**Questions before starting**:
- What is the IP address of your test machine?
- Are there any firewall rules we need to consider?
- Do you want MCP to be publicly accessible or internal network only?

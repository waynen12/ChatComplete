# Docker & Docker Compose Guide
## Comprehensive Reference for ChatComplete Project

### 🎯 **Overview**

This guide covers Docker and Docker Compose fundamentals as applied to the ChatComplete project, with specific focus on the Qdrant vector store implementation. This serves as both a learning reference and practical guide for container orchestration in development and production environments.

---

## 🐳 **Docker Fundamentals**

### **What is Docker?**
Think of Docker like shipping containers for software:
- **Container**: A lightweight, portable package that includes your application and everything it needs to run
- **Image**: The blueprint/template for creating containers
- **Dockerfile**: The recipe/instructions for building an image

### **What is Docker Compose?**
Docker Compose is like an orchestra conductor for multiple containers:
- **Orchestration**: Manages multiple containers that work together
- **Configuration**: Single YAML file defines your entire application stack
- **Networking**: Automatically connects containers so they can communicate
- **Dependencies**: Controls startup order and relationships

### **Key Concepts**
- **Service**: A container that provides a specific function (database, web server, etc.)
- **Volume**: Persistent storage that survives container restarts
- **Network**: Communication pathway between containers
- **Port Mapping**: Connecting host ports to container ports

---

## 📋 **ChatComplete Qdrant Docker Compose Breakdown**

### **Complete File Structure**
```yaml
# docker-compose.qdrant.yml
version: '3.8'

services:
  qdrant:
    image: qdrant/qdrant:latest
    container_name: qdrant-local
    ports:
      - "6333:6333"  # REST API
      - "6334:6334"  # gRPC API (optional)
    volumes:
      - qdrant_storage:/qdrant/storage
    environment:
      - QDRANT__SERVICE__HTTP_PORT=6333
      - QDRANT__SERVICE__GRPC_PORT=6334
      - QDRANT__LOG_LEVEL=INFO
    networks:
      - qdrant-network
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:6333/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s

volumes:
  qdrant_storage:
    driver: local

networks:
  qdrant-network:
    driver: bridge
```

### **Step-by-Step Explanation**

#### **1. File Header**
```yaml
version: '3.8'
```
- **Purpose**: Tells Docker Compose which syntax version to use
- **Why '3.8'**: Modern version with all required features
- **Think of it as**: Like declaring what version of a programming language you're using

#### **2. Services Definition**
```yaml
services:
  qdrant:
```
- **Purpose**: Defines a service called "qdrant" (customizable name)
- **Service**: A container that provides a specific function
- **Think of it as**: Like defining a class in programming - this is your Qdrant service template

#### **3. Container Image**
```yaml
    image: qdrant/qdrant:latest
    container_name: qdrant-local
```
- **`image`**: Downloads the official Qdrant image from Docker Hub
- **`container_name`**: Assigns a specific name instead of auto-generated
- **Alternative**: Could use `build: .` if you had a custom Dockerfile

#### **4. Port Mapping**
```yaml
    ports:
      - "6333:6333"  # REST API
      - "6334:6334"  # gRPC API
```
- **Purpose**: Maps ports from your computer to the container
- **Format**: `"host_port:container_port"`
- **Example**: `6333:6333` means localhost:6333 → container port 6333
- **Real-world usage**: `curl http://localhost:6333/health` works because of this mapping

#### **5. Volume Storage**
```yaml
    volumes:
      - qdrant_storage:/qdrant/storage
```
- **Purpose**: Persists data even when container stops/restarts
- **Format**: `volume_name:container_path`
- **Critical**: Without this, all Qdrant data disappears when container stops
- **Location**: Docker manages storage at `/var/lib/docker/volumes/`

#### **6. Environment Variables**
```yaml
    environment:
      - QDRANT__SERVICE__HTTP_PORT=6333
      - QDRANT__SERVICE__GRPC_PORT=6334
      - QDRANT__LOG_LEVEL=INFO
```
- **Purpose**: Configures the Qdrant application inside the container
- **Format**: `VARIABLE_NAME=value`
- **Qdrant-specific**: These control ports and logging level

#### **7. Networking**
```yaml
    networks:
      - qdrant-network
```
- **Purpose**: Places container on a custom network
- **Benefit**: Allows secure communication between multiple containers
- **Security**: Isolated from other Docker networks

#### **8. Health Checks**
```yaml
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:6333/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
```
- **Purpose**: Monitors if Qdrant is running properly
- **`test`**: Command to check health (curl to health endpoint)
- **`interval`**: Check every 30 seconds
- **`timeout`**: Wait 10 seconds for response
- **`retries`**: Try 3 times before marking as unhealthy
- **`start_period`**: Wait 40 seconds before starting checks (startup time)

#### **9. Volume Declaration**
```yaml
volumes:
  qdrant_storage:
    driver: local
```
- **Purpose**: Creates a named volume for data persistence
- **`driver: local`**: Stores data on local machine
- **Management**: Docker handles location and permissions

#### **10. Network Declaration**
```yaml
networks:
  qdrant-network:
    driver: bridge
```
- **Purpose**: Creates a custom network for containers
- **`driver: bridge`**: Default Docker networking (containers can communicate)
- **Usage**: Essential for multi-container applications

---

## 🚀 **Docker Compose Commands**

### **Basic Operations**

**Start Services:**
```bash
docker-compose -f docker-compose.qdrant.yml up -d
```
- **`-f`**: Specify which compose file to use
- **`up`**: Start the services
- **`-d`**: Run in background (detached mode)

**Stop Services:**
```bash
docker-compose -f docker-compose.qdrant.yml down
```
- **`down`**: Stop and remove containers
- **Note**: Volumes persist unless explicitly removed

**View Logs:**
```bash
# All services
docker-compose -f docker-compose.qdrant.yml logs

# Specific service
docker-compose -f docker-compose.qdrant.yml logs qdrant

# Follow logs in real-time
docker-compose -f docker-compose.qdrant.yml logs -f qdrant
```

**Check Service Status:**
```bash
docker-compose -f docker-compose.qdrant.yml ps
```

**Restart Services:**
```bash
docker-compose -f docker-compose.qdrant.yml restart qdrant
```

**Execute Commands in Running Container:**
```bash
docker-compose -f docker-compose.qdrant.yml exec qdrant /bin/bash
```

### **Volume Management**

**List Volumes:**
```bash
docker volume ls
```

**Inspect Volume Details:**
```bash
docker volume inspect chatcomplete_qdrant_storage
```

**Remove Volume (DANGER - deletes all data):**
```bash
docker volume rm chatcomplete_qdrant_storage
```

**Backup Volume:**
```bash
docker run --rm -v chatcomplete_qdrant_storage:/data -v $(pwd):/backup alpine tar czf /backup/qdrant-backup.tar.gz -C /data .
```

**Restore Volume:**
```bash
docker run --rm -v chatcomplete_qdrant_storage:/data -v $(pwd):/backup alpine tar xzf /backup/qdrant-backup.tar.gz -C /data
```

---

## 📁 **Volume Storage Deep Dive**

### **Volume Types Comparison**

#### **1. Named Volume (Current Implementation)**
```yaml
volumes:
  - qdrant_storage:/qdrant/storage
```
- **Pros**: Docker-managed, persists across recreations, proper permissions
- **Cons**: Hard to browse manually, requires Docker commands to access
- **Location**: `/var/lib/docker/volumes/PROJECT_qdrant_storage/_data`
- **Best for**: Production, CI/CD, standardized deployments

#### **2. Bind Mount (Alternative)**
```yaml
volumes:
  - ./qdrant_data:/qdrant/storage
```
- **Pros**: Easy to browse, backup, and version control
- **Cons**: Path dependencies, potential permission issues
- **Location**: `./qdrant_data` (relative to docker-compose.yml)
- **Best for**: Development, debugging, direct file access needed

#### **3. Absolute Path Bind Mount**
```yaml
volumes:
  - /home/wayne/repos/ChatComplete/qdrant_data:/qdrant/storage
```
- **Pros**: Explicit path, easy to find and backup
- **Cons**: Not portable across different machines/users
- **Location**: Exactly where specified
- **Best for**: Single-machine deployments, personal projects

### **Why `/var/lib/docker/` is Standard Practice**

#### **Linux File System Hierarchy Standard (FHS)**
- **`/var`**: Variable data files (logs, databases, caches)
- **`/var/lib/`**: State information for programs
- **`/var/lib/docker/`**: Docker's persistent data storage

#### **Design Benefits**
1. **System-wide Storage**: Available to all users with Docker access
2. **Persistent Location**: Survives user account changes and system updates
3. **Standard Practice**: Follows Linux conventions for application data
4. **Permission Control**: Proper ownership and security controls
5. **Backup-Friendly**: System administrators know to back up `/var/lib/`

#### **Other Applications Using `/var/lib/`**
- PostgreSQL: `/var/lib/postgresql/`
- MySQL: `/var/lib/mysql/`
- Redis: `/var/lib/redis/`
- MongoDB: `/var/lib/mongodb/`
- Elasticsearch: `/var/lib/elasticsearch/`

### **Accessing Volume Data**

#### **Method 1: Docker Command**
```bash
# Browse volume contents
docker run --rm -v chatcomplete_qdrant_storage:/data alpine ls -la /data

# Copy files from volume
docker run --rm -v chatcomplete_qdrant_storage:/data -v $(pwd):/backup alpine cp -r /data/. /backup/
```

#### **Method 2: Container Access**
```bash
# Access running container
docker exec -it qdrant-local /bin/bash

# Inside container, browse /qdrant/storage
ls -la /qdrant/storage
```

#### **Method 3: Direct System Access (requires sudo)**
```bash
# View Docker volume location
docker volume inspect chatcomplete_qdrant_storage

# Browse directly (if permissions allow)
sudo ls -la /var/lib/docker/volumes/chatcomplete_qdrant_storage/_data
```

---

## 🏗️ **Environment-Specific Configurations**

### **Development Environment**
```yaml
# docker-compose.dev.yml
version: '3.8'
services:
  qdrant:
    image: qdrant/qdrant:latest
    ports:
      - "6333:6333"
    volumes:
      - qdrant_storage:/qdrant/storage
    environment:
      - QDRANT__LOG_LEVEL=DEBUG  # More verbose logging
    # No resource limits for easy development
```

### **Production Environment**
```yaml
# docker-compose.prod.yml
version: '3.8'
services:
  qdrant:
    image: qdrant/qdrant:1.8.1  # Specific version, not 'latest'
    restart: unless-stopped     # Auto-restart on failure
    ports:
      - "6333:6333"
    volumes:
      - qdrant_storage:/qdrant/storage
    environment:
      - QDRANT__LOG_LEVEL=WARN   # Less verbose logging
    deploy:
      resources:
        limits:
          memory: 2G             # Limit memory usage
          cpus: '1.0'           # Limit CPU usage
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:6333/health"]
      interval: 30s
      timeout: 10s
      retries: 3
```

### **Multi-Environment Usage**
```bash
# Development
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d

# Production
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

---

## 🔧 **Complete Multi-Service Example**

### **Full ChatComplete System**
```yaml
# docker-compose.full.yml
version: '3.8'

services:
  qdrant:
    image: qdrant/qdrant:latest
    container_name: qdrant
    ports:
      - "6333:6333"
      - "6334:6334"
    volumes:
      - qdrant_storage:/qdrant/storage
    networks:
      - chatcomplete-network
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:6333/health"]
      interval: 30s
      timeout: 10s
      retries: 3

  knowledge-api:
    build:
      context: .
      dockerfile: Knowledge.Api/Dockerfile
    container_name: knowledge-api
    ports:
      - "7040:7040"
    depends_on:
      qdrant:
        condition: service_healthy  # Wait for Qdrant to be healthy
    environment:
      - VectorStore__Provider=Qdrant
      - VectorStore__Qdrant__Host=qdrant  # Container name as hostname
      - VectorStore__Qdrant__Port=6333
      - OPENAI_API_KEY=${OPENAI_API_KEY}
    networks:
      - chatcomplete-network
    volumes:
      - ./appsettings.Production.json:/app/appsettings.Production.json:ro

  webclient:
    build:
      context: ./webclient
      dockerfile: Dockerfile
    container_name: webclient
    ports:
      - "3000:3000"
    depends_on:
      - knowledge-api
    environment:
      - REACT_APP_API_URL=http://knowledge-api:7040
    networks:
      - chatcomplete-network

volumes:
  qdrant_storage:

networks:
  chatcomplete-network:
    driver: bridge
```

### **Key Multi-Service Concepts**

#### **Service Dependencies**
```yaml
depends_on:
  qdrant:
    condition: service_healthy
```
- **Purpose**: Ensures Qdrant is healthy before starting API
- **Benefit**: Prevents connection errors during startup

#### **Container Communication**
```yaml
environment:
  - VectorStore__Qdrant__Host=qdrant  # Use container name as hostname
```
- **Key Concept**: Containers use service names as hostnames
- **Example**: `knowledge-api` connects to `qdrant:6333`

#### **Shared Networks**
```yaml
networks:
  - chatcomplete-network
```
- **Purpose**: All services can communicate with each other
- **Security**: Isolated from other Docker networks

---

## 📚 **Common Docker Compose Patterns**

### **Environment Variables from File**
```yaml
# .env file
OPENAI_API_KEY=your_key_here
QDRANT_VERSION=1.8.1

# docker-compose.yml
services:
  qdrant:
    image: qdrant/qdrant:${QDRANT_VERSION}
    environment:
      - OPENAI_API_KEY=${OPENAI_API_KEY}
```

### **Config File Mounting**
```yaml
services:
  knowledge-api:
    volumes:
      - ./appsettings.Production.json:/app/appsettings.Production.json:ro
      # :ro means read-only
```

### **Logging Configuration**
```yaml
services:
  qdrant:
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"
```

### **Resource Limits**
```yaml
services:
  qdrant:
    deploy:
      resources:
        limits:
          memory: 2G
          cpus: '1.0'
        reservations:
          memory: 512M
          cpus: '0.25'
```

### **Restart Policies**
```yaml
services:
  qdrant:
    restart: unless-stopped  # Always restart unless manually stopped
    # Other options: no, always, on-failure
```

---

## 🎯 **Production Best Practices**

### **Security**
1. **Use Specific Versions**: Never use `latest` in production
   ```yaml
   image: qdrant/qdrant:1.8.1  # Not qdrant/qdrant:latest
   ```

2. **Resource Limits**: Set memory and CPU constraints
   ```yaml
   deploy:
     resources:
       limits:
         memory: 2G
         cpus: '1.0'
   ```

3. **Health Checks**: Always include for critical services
   ```yaml
   healthcheck:
     test: ["CMD", "curl", "-f", "http://localhost:6333/health"]
     interval: 30s
     timeout: 10s
     retries: 3
   ```

4. **Restart Policies**: Auto-restart on failure
   ```yaml
   restart: unless-stopped
   ```

5. **Read-Only Filesystems**: When possible
   ```yaml
   read_only: true
   tmpfs:
     - /tmp
   ```

### **Monitoring & Logging**
```yaml
services:
  qdrant:
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"
    labels:
      - "traefik.enable=true"
      - "monitoring.enable=true"
```

### **Secrets Management**
```yaml
# docker-compose.yml
services:
  knowledge-api:
    secrets:
      - openai_api_key
    environment:
      - OPENAI_API_KEY_FILE=/run/secrets/openai_api_key

secrets:
  openai_api_key:
    file: ./secrets/openai_api_key.txt
```

### **Backup Strategy**
```bash
# Automated backup script
#!/bin/bash
DATE=$(date +%Y%m%d_%H%M%S)
docker run --rm \
  -v chatcomplete_qdrant_storage:/data \
  -v /backup/location:/backup \
  alpine tar czf /backup/qdrant_backup_$DATE.tar.gz -C /data .
```

---

## 🛠️ **Troubleshooting Guide**

### **Common Issues**

#### **Port Already in Use**
```bash
# Error: bind: address already in use
# Solution: Check what's using the port
sudo netstat -tlnp | grep 6333
# Or change the host port
ports:
  - "6334:6333"  # Use different host port
```

#### **Volume Permission Issues**
```bash
# Error: Permission denied
# Solution: Check container user
docker-compose exec qdrant id
# Or fix permissions
docker-compose exec qdrant chown -R qdrant:qdrant /qdrant/storage
```

#### **Container Won't Start**
```bash
# Check logs for errors
docker-compose logs qdrant
# Check container status
docker-compose ps
# Inspect container details
docker inspect qdrant-local
```

#### **Network Issues**
```bash
# Test container networking
docker-compose exec knowledge-api ping qdrant
# Check network configuration
docker network ls
docker network inspect chatcomplete_chatcomplete-network
```

### **Debugging Commands**
```bash
# Enter running container
docker-compose exec qdrant /bin/bash

# Check container resource usage
docker stats qdrant-local

# View container processes
docker-compose top qdrant

# Inspect service configuration
docker-compose config
```

---

## 📊 **Quick Reference Commands**

### **Lifecycle Management**
```bash
# Start services
docker-compose -f docker-compose.qdrant.yml up -d

# Stop services
docker-compose -f docker-compose.qdrant.yml down

# Restart specific service
docker-compose -f docker-compose.qdrant.yml restart qdrant

# Rebuild and start
docker-compose -f docker-compose.qdrant.yml up --build -d

# Stop and remove everything including volumes (DANGER)
docker-compose -f docker-compose.qdrant.yml down -v
```

### **Monitoring & Debugging**
```bash
# View logs
docker-compose -f docker-compose.qdrant.yml logs -f qdrant

# Check service status
docker-compose -f docker-compose.qdrant.yml ps

# Execute commands in container
docker-compose -f docker-compose.qdrant.yml exec qdrant /bin/bash

# Check resource usage
docker stats qdrant-local
```

### **Volume Operations**
```bash
# List volumes
docker volume ls

# Inspect volume
docker volume inspect chatcomplete_qdrant_storage

# Backup volume
docker run --rm -v chatcomplete_qdrant_storage:/data -v $(pwd):/backup alpine tar czf /backup/backup.tar.gz -C /data .

# Clean up unused volumes
docker volume prune
```

---

## 🎓 **Key Takeaways**

1. **Named Volumes**: Standard practice for production, Docker manages storage in `/var/lib/docker/volumes/`
2. **Port Mapping**: Format is `"host_port:container_port"`
3. **Health Checks**: Essential for production deployments
4. **Dependencies**: Use `depends_on` with conditions for startup order
5. **Networking**: Container names become hostnames within Docker networks
6. **Environment Variables**: Configure applications without rebuilding images
7. **Multi-Environment**: Use override files for different deployment scenarios
8. **Resource Limits**: Always set in production to prevent resource exhaustion
9. **Logging**: Configure log rotation to prevent disk space issues
10. **Backup Strategy**: Plan for volume backups and disaster recovery

---

This Docker guide provides a comprehensive reference for understanding and managing containerized deployments in the ChatComplete project. The patterns and practices shown here scale from local development to production environments while maintaining consistency and reliability.

**Remember**: Your Qdrant Docker Compose configuration follows industry best practices and is production-ready with the addition of specific version tags, resource limits, and proper secrets management.
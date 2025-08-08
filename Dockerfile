# Multi-stage Dockerfile for AI Knowledge Manager
# Stage 1: Frontend Build (React + Vite)
FROM node:20-alpine AS frontend-build
WORKDIR /app/frontend

# Copy package files
COPY webclient/package*.json ./
RUN npm ci

# Copy frontend source and build
COPY webclient/ ./
RUN npm run build

# Stage 2: Backend Build (.NET 8)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend-build
WORKDIR /app/backend

# Copy solution and project files
COPY *.sln ./
COPY Knowledge.Api/*.csproj ./Knowledge.Api/
COPY KnowledgeEngine/*.csproj ./KnowledgeEngine/
COPY Knowledge.Contracts/*.csproj ./Knowledge.Contracts/

# Restore dependencies for production projects only
RUN dotnet restore Knowledge.Api/Knowledge.Api.csproj

# Copy source code
COPY . ./

# Build and publish
RUN dotnet publish Knowledge.Api/Knowledge.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Stage 3: Runtime (Final container)
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime

# Install curl for health checks
RUN apk --no-cache add curl

WORKDIR /app

# Copy published application
COPY --from=backend-build /app/publish ./

# Copy built frontend to wwwroot for static file serving
COPY --from=frontend-build /app/frontend/dist ./wwwroot

# Create data directory with proper permissions
RUN mkdir -p /app/data /app/temp && \
    chmod 755 /app/data /app/temp

# Create non-root user for security
RUN addgroup -g 1001 -S appgroup && \
    adduser -u 1001 -S appuser -G appgroup && \
    chown -R appuser:appgroup /app

USER appuser

# Expose the application port
EXPOSE 7040

# Health check endpoint
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:7040/api/ping || exit 1

# Set environment variables for container
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:7040 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    DOTNET_RUNNING_IN_CONTAINER=true

# Create volume for persistent data
VOLUME ["/app/data"]

ENTRYPOINT ["dotnet", "Knowledge.Api.dll"]
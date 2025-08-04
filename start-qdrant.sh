#!/bin/bash

echo "🚀 Starting Qdrant Vector Database..."
echo "This will start Qdrant on localhost:6333"
echo ""

# Start Qdrant using Docker Compose
docker-compose -f docker-compose.qdrant.yml up -d

echo ""
echo "⏳ Waiting for Qdrant to be ready..."

# Wait for Qdrant to be healthy
max_attempts=30
attempt=0

while [ $attempt -lt $max_attempts ]; do
    if curl -s http://localhost:6333/health > /dev/null 2>&1; then
        echo "✅ Qdrant is ready!"
        echo ""
        echo "📊 Qdrant Web UI: http://localhost:6333/dashboard"
        echo "🔌 REST API: http://localhost:6333"
        echo ""
        echo "🧪 Test the connection:"
        echo "curl http://localhost:6333/health"
        echo ""
        echo "📝 To switch to Qdrant, change 'Provider' to 'Qdrant' in appsettings.json"
        break
    fi
    
    attempt=$((attempt + 1))
    echo "   Attempt $attempt/$max_attempts..."
    sleep 2
done

if [ $attempt -eq $max_attempts ]; then
    echo "❌ Qdrant failed to start within expected time"
    echo "Check logs with: docker-compose -f docker-compose.qdrant.yml logs"
    exit 1
fi

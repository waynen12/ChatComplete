#!/bin/bash

echo "🔍 Verifying Qdrant Setup..."
echo ""

# Check if Qdrant is running
if curl -s http://localhost:6333/health > /dev/null 2>&1; then
    echo "✅ Qdrant is running and healthy"
    
    # Get Qdrant version and status
    echo "📊 Qdrant Status:"
    curl -s http://localhost:6333/ | head -n 10
    echo ""
    
    # List collections (should be empty initially)
    echo "📚 Current Collections:"
    curl -s http://localhost:6333/collections | python3 -m json.tool 2>/dev/null || curl -s http://localhost:6333/collections
    echo ""
    
    echo "🎯 Next Steps:"
    echo "1. Visit Qdrant Web UI: http://localhost:6333/dashboard"
    echo "2. Switch your app to use Qdrant by changing 'Provider' to 'Qdrant' in appsettings.json"
    echo "3. Test uploading documents to see Qdrant collections being created"
    echo ""
    echo "🧪 Ready to move to Phase 2: Semantic Kernel Integration!"
    
else
    echo "❌ Qdrant is not responding"
    echo "Try running: ./start-qdrant.sh"
    exit 1
fi

#!/bin/bash

echo "🚀 Starting AI Knowledge Manager Development Environment"
echo "=================================================="
echo ""

# Start Qdrant Vector Database first
echo "📊 Starting Qdrant Vector Database..."
./start-qdrant.sh

if [ $? -ne 0 ]; then
    echo "❌ Failed to start Qdrant. Exiting..."
    exit 1
fi

echo ""
echo "🌐 Starting Frontend Development Server..."
echo ""

# Navigate to webclient directory and start dev server
cd webclient

if [ ! -d "node_modules" ]; then
    echo "📦 Installing frontend dependencies..."
    npm install
fi

echo "🔄 Starting Vite development server..."
echo "Frontend will be available at: http://localhost:5173"
echo ""

# Start the development server
npm run dev
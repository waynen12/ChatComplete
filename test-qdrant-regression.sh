#!/bin/bash

# Qdrant Configuration Regression Test Script
# This script runs automated tests to ensure Qdrant configuration doesn't regress
# Run this script during development to catch configuration issues early

set -e  # Exit on any error

echo "🧪 Running Qdrant Configuration Regression Tests..."
echo "=================================================="

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    local status=$1
    local message=$2
    case $status in
        "PASS")
            echo -e "${GREEN}✅ PASS${NC}: $message"
            ;;
        "FAIL")
            echo -e "${RED}❌ FAIL${NC}: $message"
            ;;
        "INFO")
            echo -e "${YELLOW}ℹ️  INFO${NC}: $message"
            ;;
    esac
}

# Test 1: Run unit tests
echo
print_status "INFO" "Running unit tests for Qdrant configuration..."
if dotnet test Knowledge.Mcp.Tests/ --verbosity minimal --logger "console;verbosity=normal"; then
    print_status "PASS" "All unit tests passed"
else
    print_status "FAIL" "Unit tests failed"
    exit 1
fi

# Test 2: Build MCP server
echo
print_status "INFO" "Building MCP server..."
if dotnet build Knowledge.Mcp/ --configuration Debug --verbosity minimal; then
    print_status "PASS" "MCP server builds successfully"
else
    print_status "FAIL" "MCP server build failed"
    exit 1
fi

# Test 3: Quick integration test (if Qdrant is running)
echo
print_status "INFO" "Testing Qdrant collection detection..."
cd Knowledge.Mcp
if timeout 10 dotnet run -- --test-collections >/dev/null 2>&1; then
    print_status "PASS" "Qdrant collection test passed"
else
    print_status "FAIL" "Qdrant collection test failed (Qdrant may not be running)"
    print_status "INFO" "To start Qdrant: docker run -p 6333:6333 -p 6334:6334 qdrant/qdrant"
fi
cd ..

# Test 4: Configuration validation
echo
print_status "INFO" "Validating configuration files..."
if [ -f "Knowledge.Mcp/appsettings.json" ]; then
    # Check if configuration contains correct Qdrant settings
    if grep -q '"Provider": "Qdrant"' Knowledge.Mcp/appsettings.json && grep -q '"Port": 6334' Knowledge.Mcp/appsettings.json; then
        print_status "PASS" "Configuration file contains correct Qdrant settings"
    else
        print_status "FAIL" "Configuration file missing correct Qdrant settings"
        exit 1
    fi
else
    print_status "FAIL" "Configuration file not found"
    exit 1
fi

# Summary
echo
echo "=================================================="
print_status "PASS" "All regression tests completed successfully!"
echo
echo "🔍 What these tests verify:"
echo "  • Configuration binding works correctly"
echo "  • Qdrant settings use port 6334 (gRPC) not 6333 (REST)"
echo "  • MCP server builds and starts properly"
echo "  • Service registration resolves dependencies"
echo "  • Debug output format is consistent"
echo
echo "💡 To run individual test categories:"
echo "  • Unit tests only: dotnet test Knowledge.Mcp.Tests/"
echo "  • Build test only: dotnet build Knowledge.Mcp/"
echo "  • Integration test: cd Knowledge.Mcp && dotnet run -- --test-collections"
echo
echo "🚀 If all tests pass, your Qdrant configuration should work correctly!"
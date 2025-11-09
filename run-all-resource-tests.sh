#!/bin/bash
# run-all-resource-tests.sh - Comprehensive MCP Resource Templates Test Suite

echo "╔════════════════════════════════════════════════════════════╗"
echo "║  MCP Resource Templates - Comprehensive Test Suite        ║"
echo "╚════════════════════════════════════════════════════════════╝"
echo ""

PASS=0
FAIL=0

# Helper function
run_test() {
  local test_name="$1"
  local test_cmd="$2"

  echo -n "Testing: $test_name... "

  if eval "$test_cmd" > /tmp/test_output.txt 2>&1; then
    echo "✓ PASS"
    ((PASS++))
  else
    echo "✗ FAIL"
    echo "  Error: $(cat /tmp/test_output.txt | tail -1)"
    ((FAIL++))
  fi
}

echo "Phase 1: Static Resources"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━"
run_test "Server Initialization" "./test-mcp-resources-clean.sh 2>&1 | grep -q '\"protocolVersion\": \"2024-11-05\"'"
run_test "Static Resources Count" "./test-mcp-resources-clean.sh 2>&1 | grep -c 'AI Models Inventory\\|Knowledge Collections\\|System Health' | grep -q '^3$'"
run_test "System Health Resource" "./test-mcp-resources-clean.sh 2>&1 | grep -q 'resource://system/health'"
echo ""

echo "Phase 2: Resource Templates"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
run_test "Templates Discovery" "./test-mcp-resources-clean.sh 2>&1 | grep -q 'resourceTemplates'"
run_test "Templates Count" "./test-mcp-resources-clean.sh 2>&1 | grep -c 'Collection Documents\\|Document Content\\|Collection Statistics' | grep -q '^3$'"
run_test "No Duplicate Templates" "./test-mcp-resources-clean.sh 2>&1 | grep 'uriTemplate' | sort | uniq -d | wc -l | grep -q '^0$'"
echo ""

echo "Phase 3: Parameterized Resources"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
run_test "Collection Documents Template" "./test-mcp-resources-clean.sh 2>&1 | grep -q '{collectionId}/documents'"
run_test "Document Content Template" "./test-mcp-resources-clean.sh 2>&1 | grep -q '{collectionId}/document/{documentId}'"
run_test "Collection Stats Template" "./test-mcp-resources-clean.sh 2>&1 | grep -q '{collectionId}/stats'"
echo ""

echo "╔════════════════════════════════════════════════════════════╗"
echo "║  Test Results                                             ║"
echo "╠════════════════════════════════════════════════════════════╣"
printf "║  PASSED: %-3d                                              ║\n" $PASS
printf "║  FAILED: %-3d                                              ║\n" $FAIL
echo "╚════════════════════════════════════════════════════════════╝"

if [ $FAIL -eq 0 ]; then
  echo "✓ All tests passed!"
  exit 0
else
  echo "✗ Some tests failed!"
  exit 1
fi

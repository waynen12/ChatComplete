# Ollama Model Management Integration Tests

## Overview

This directory contains integration tests for the Ollama model management functionality.

## Test Files

- **`OllamaModelDownloadTests.cs`** - Focused test for download → verify → delete workflow
- **`OllamaModelManagementTests.cs`** - Comprehensive test suite with detailed progress monitoring

## Prerequisites

1. **Ollama must be running** on `http://localhost:11434`
2. **Internet connection** required for downloading models
3. **Disk space**: At least 1GB free for test models

## Running the Tests

### Command Line

```bash
# Run all Ollama integration tests
dotnet test --filter "Category=Integration&RequiresOllama=true"

# Run specific test
dotnet test --filter "FullyQualifiedName~OllamaModelDownloadTests.DownloadVerifyDeleteSmallModel_ShouldSucceed"

# Run with verbose output
dotnet test --filter "Category=Integration&RequiresOllama=true" --logger "console;verbosity=detailed"
```

### Visual Studio / Rider

1. Open Test Explorer
2. Filter by traits: `Category = Integration` and `RequiresOllama = true`
3. Right-click and "Run Selected Tests"

## Test Models

The tests use small models to minimize download time and disk usage:

- **Primary**: `tinyllama:1.1b` (~637MB, 1.1B parameters) ✅ VERIFIED WORKING
- **Alternative**: `qwen2.5:0.5b` (~374MB, 500M parameters)

## Test Workflow

1. **Setup**: Initialize SQLite database, verify Ollama connection
2. **Cleanup**: Remove any existing test model
3. **Download**: Pull the small model from Ollama registry
4. **Verify**: Confirm model is installed and functional
5. **Delete**: Remove model from Ollama and database
6. **Verify**: Confirm complete removal

## Expected Duration

- **Fast network**: 2-3 minutes
- **Slow network**: 5-8 minutes  
- **Timeout**: 10 minutes maximum
- **Latest Results**: 12-13 seconds ✅ (all components working)

## Troubleshooting

### Ollama Not Running
```
SkipException: Ollama not available: No connection could be made...
```
**Solution**: Start Ollama service

### Network Issues
```
TimeoutException: Download did not complete within X minutes
```
**Solution**: Check internet connection, increase timeout if needed

### Permission Issues
```
UnauthorizedAccessException: Access to the path ... is denied
```
**Solution**: Run tests with appropriate permissions

## Test Configuration

Tests create temporary databases in:
- Windows: `%TEMP%\OllamaTest\{guid}\test.db`
- Linux/Mac: `/tmp/OllamaTest/{guid}/test.db`

Databases are automatically cleaned up after test completion.
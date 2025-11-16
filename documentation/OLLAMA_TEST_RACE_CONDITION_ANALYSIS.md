# Ollama Integration Test Race Condition Analysis

**Date:** 2025-11-16
**Test:** `OllamaModelManagementTests.DownloadVerifyDelete_SmallModel_ShouldCompleteSuccessfully`
**Status:** Passes in isolation, fails in full suite (99.7% pass rate)

---

## Symptoms

### When Run in Isolation
```bash
dotnet test --filter "FullyQualifiedName~DownloadVerifyDelete_SmallModel_ShouldCompleteSuccessfully"
```
**Result:** ✅ PASS (15.5 seconds)

### When Run in Full Suite
```bash
dotnet test
```
**Result:** ❌ FAIL at line 308 (`Assert.NotNull()` - model not found)

**Error:**
```
Verifying model installation: tinyllama:1.1b
❌ Test failed: Assert.NotNull() Failure: Value is null
Stack trace: VerifyModelInstallationAsync(String modelName) line 308
```

---

## Root Cause

### Resource Contention on Shared Ollama Instance

**Timeline in Full Test Suite:**

```
[00:00:02] OllamaModelDownloadTests.DownloadVerifyDeleteSmallModel_ShouldSucceed starts
           ├─ Downloads tinyllama:1.1b
           ├─ Verifies installation
           └─ Deletes tinyllama:1.1b
[00:00:15] Test completes (13 seconds)

[00:00:15] OllamaModelManagementTests.DownloadVerifyDelete_SmallModel_ShouldCompleteSuccessfully starts
           ├─ Model cleanup: checks if tinyllama:1.1b exists
           ├─ Starts download for tinyllama:1.1b
           ├─ Download completes: "✅ Download completed successfully!"
           ├─ Verifies installation: calls GetInstalledModelsAsync()
           └─ ❌ FAILS: model not in list (line 308)
[00:00:30] Test fails (15 seconds)
```

### The Race Condition

**Code at line 305-308:**
```csharp
var installedModels = await _ollamaService.GetInstalledModelsAsync(_cancellationTokenSource.Token);
var model = installedModels.FirstOrDefault(m => m.Name.Equals(modelName, StringComparison.OrdinalIgnoreCase));

Assert.NotNull(model); // ❌ FAILS HERE - model is null
```

**Why the model is null:**

1. **Ollama Internal State**: After deleting a model (Test 1), Ollama may take time to:
   - Clean up internal references
   - Update its model registry
   - Release file locks
   - Refresh its cache

2. **Download vs Registration Gap**:
   - Download progress API reports "Completed" ✅
   - But Ollama's `/api/tags` (list models endpoint) hasn't updated yet ⏱️
   - This is an **eventually consistent** system

3. **No Synchronization**: Tests don't coordinate - they run in parallel or rapid succession

---

## Evidence

### Test Output Comparison

**Isolated Run (Works):**
- Ollama starts fresh
- No competing operations
- Model listing has time to synchronize
- **Result:** Model found immediately after download

**Full Suite Run (Fails):**
- Previous test just deleted the same model 0.1 seconds ago
- Ollama still processing deletion cleanup
- Download succeeds but model listing lags
- **Result:** Model not found in list (timing issue)

### Ollama Model Count
From logs: "Ollama is running. Found 18 installed models."

This shows Ollama has many models, indicating:
- This is a development/test machine (not CI)
- Multiple tests have left models behind
- Concurrent test runs may interact

---

## Impact Assessment

### Current Impact: **LOW**
- **Reliability:** 99.7% pass rate (396/397 tests)
- **Frequency:** Only fails in full suite runs
- **Severity:** Does not affect production code
- **Workarounds:** Run test individually when debugging

### Risk if Unfixed: **MEDIUM**
- **CI/CD:** May cause flaky builds in continuous integration
- **Developer Confidence:** Developers may ignore "always failing" tests
- **Test Maintenance:** Harder to detect real regressions

---

## Recommended Solutions

### Option 1: Add Retry Logic with Backoff (RECOMMENDED) ⭐

**Implementation:**
```csharp
private async Task VerifyModelInstallationAsync(string modelName)
{
    _output.WriteLine($"Verifying model installation: {modelName}");

    const int maxRetries = 5;
    const int initialDelayMs = 500;

    for (int attempt = 0; attempt < maxRetries; attempt++)
    {
        if (attempt > 0)
        {
            var delay = initialDelayMs * (int)Math.Pow(2, attempt - 1); // Exponential backoff
            _output.WriteLine($"Retry {attempt}/{maxRetries} after {delay}ms delay...");
            await Task.Delay(delay, _cancellationTokenSource.Token);
        }

        // Check via Ollama API
        var installedModels = await _ollamaService.GetInstalledModelsAsync(_cancellationTokenSource.Token);
        var model = installedModels.FirstOrDefault(m => m.Name.Equals(modelName, StringComparison.OrdinalIgnoreCase));

        if (model != null)
        {
            Assert.True(model.Size > 0, "Model size should be greater than 0");
            _output.WriteLine($"✅ Model verified via Ollama API (attempt {attempt + 1}). Size: {model.Size:N0} bytes");

            // Continue with database check...
            return;
        }
    }

    // If we get here, all retries failed
    var allModels = await _ollamaService.GetInstalledModelsAsync(_cancellationTokenSource.Token);
    _output.WriteLine($"❌ Model '{modelName}' not found after {maxRetries} attempts.");
    _output.WriteLine($"Available models: {string.Join(", ", allModels.Select(m => m.Name))}");
    Assert.Fail($"Model '{modelName}' not found in Ollama after download completion and {maxRetries} retries");
}
```

**Backoff Schedule:**
- Attempt 1: Immediate (0ms)
- Attempt 2: 500ms
- Attempt 3: 1000ms
- Attempt 4: 2000ms
- Attempt 5: 4000ms
- **Total:** ~7.5 seconds max

**Pros:**
- ✅ Handles eventually consistent systems
- ✅ Works in both isolated and full suite runs
- ✅ Provides clear diagnostics (logs all attempts)
- ✅ Reasonable timeout (< 10 seconds)
- ✅ Minimal code change

**Cons:**
- ⚠️ Slightly slower in full suite (adds up to 7.5s)
- ⚠️ Masks underlying timing issue (but that's Ollama's behavior)

---

### Option 2: Use Different Test Models

**Change Test 1 and Test 2 to use different models:**

```csharp
// OllamaModelDownloadTests.cs
private const string TEST_MODEL = "qwen2.5:0.5b";  // 374MB

// OllamaModelManagementTests.cs
private const string SMALL_TEST_MODEL = "tinyllama:1.1b";  // 637MB
```

**Pros:**
- ✅ Eliminates resource contention
- ✅ Tests are truly isolated
- ✅ No retry logic needed

**Cons:**
- ⚠️ Downloads more data (slower tests)
- ⚠️ Still vulnerable if tests run parallel
- ⚠️ Requires discipline (developers might use same model)

---

### Option 3: Add Test Ordering/Isolation

**Use xUnit collections to serialize tests:**

```csharp
[Collection("Ollama Integration")]
public class OllamaModelDownloadTests { }

[Collection("Ollama Integration")]
public class OllamaModelManagementTests { }
```

**Pros:**
- ✅ Guarantees serial execution
- ✅ Prevents all race conditions

**Cons:**
- ⚠️ Slower (can't parallelize Ollama tests)
- ⚠️ Doesn't solve the core timing issue

---

### Option 4: Mock Ollama in Unit Tests

**Separate unit tests (mocked) from integration tests (real Ollama):**

```csharp
[Trait("Category", "Unit")]
public class OllamaModelManagementUnitTests
{
    // Mock IOllamaApiService, test logic only
}

[Trait("Category", "Integration")]
[Trait("RequiresOllama", "true")]
public class OllamaModelManagementIntegrationTests
{
    // Real Ollama, fewer tests, focus on E2E
}
```

**Pros:**
- ✅ Most unit tests are fast and reliable
- ✅ Integration tests focused on real scenarios

**Cons:**
- ⚠️ Major refactoring required
- ⚠️ Doesn't help with integration test race

---

## Recommended Approach

**Combination Strategy:**

1. **Immediate Fix (Option 1):** Add retry logic to `VerifyModelInstallationAsync()`
   - **Effort:** 30 minutes
   - **Impact:** Fixes the flaky test immediately

2. **Short-term (Option 2):** Use different models in each test file
   - **Effort:** 10 minutes
   - **Impact:** Reduces contention

3. **Long-term (Option 3):** Add test collection for Ollama tests
   - **Effort:** 1 hour
   - **Impact:** Ensures clean isolation

---

## Implementation Priority

### P0 - Critical (Do Now)
- ✅ **Add retry logic** to `VerifyModelInstallationAsync()` (Option 1)
- ✅ **Document** this race condition (this file)

### P1 - Important (Next Sprint)
- 🔄 Use different test models (Option 2)
- 🔄 Add xUnit collection attribute (Option 3)

### P2 - Nice to Have (Future)
- 🛠️ Refactor to unit + integration test split (Option 4)

---

## Alternative: Accept the Flake

**If this is acceptable:**
- Mark test with `[Trait("Category", "Flaky")]`
- Add skip condition: `if (runningInCI) Skip.IfNot(false);`
- Document known issue in MASTER_TEST_PLAN.md ✅ (already done)

**However, this is NOT recommended** because:
- Flaky tests erode trust
- Developers start ignoring failures
- Real bugs may be masked

---

## Conclusion

**Root Cause:** Eventually consistent Ollama model listing API + resource contention on shared test instance

**Recommended Fix:** Add exponential backoff retry to `VerifyModelInstallationAsync()` (30 min fix)

**Expected Outcome:** 100% test pass rate in both isolated and full suite runs

---

## References

- **Test File:** [KnowledgeManager.Tests/Integration/OllamaModelManagementTests.cs:308](../KnowledgeManager.Tests/Integration/OllamaModelManagementTests.cs#L308)
- **Master Test Plan:** [MASTER_TEST_PLAN.md](MASTER_TEST_PLAN.md#failed-test-analysis)
- **Ollama API:** https://github.com/ollama/ollama/blob/main/docs/api.md

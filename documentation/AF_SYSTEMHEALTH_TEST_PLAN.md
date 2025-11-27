# SystemHealthPlugin Testing Guide

**Plugin:** SystemHealthPlugin (Agent Framework)
**Status:** ✅ Migrated, ⏳ Awaiting Testing
**Functions:** 6 total
**Branch:** feature/agent-framework-tool-calling

---

## Testing Strategy

Since ChatComplete.cs AF mode is not yet implemented, we have two testing approaches:

### Option 1: Manual Tool Registration Test ✅ RECOMMENDED

Use the existing `SystemHealthPluginTest.TestToolRegistration()` method to verify tool discovery works correctly.

**Steps:**
1. Add test call in Knowledge.Api startup (temporary)
2. Run the API
3. Check console output for tool registration
4. Verify 6 tools are discovered

**Expected Output:**
```
🧪 ========== SystemHealthPlugin Tool Registration Test ==========

✅ SystemHealthPlugin instance created
📦 Registering tools from plugin: SystemHealthPlugin
✅ Registered AF tool: GetSystemHealthAsync from SystemHealthPlugin
✅ Registered AF tool: CheckComponentHealthAsync from SystemHealthPlugin
✅ Registered AF tool: GetSystemMetricsAsync from SystemHealthPlugin
✅ Registered AF tool: GetHealthRecommendationsAsync from SystemHealthPlugin
✅ Registered AF tool: GetAvailableComponentsAsync from SystemHealthPlugin
✅ Registered AF tool: GetQuickHealthOverviewAsync from SystemHealthPlugin

📊 Tool Registration Results:
   Total tools registered: 6
   Expected: 6 functions
   ✅ SUCCESS: All 6 functions registered!

📋 Registered Tools:
   - Tool #1
   - Tool #2
   - Tool #3
   - Tool #4
   - Tool #5
   - Tool #6

✅ SystemHealthPlugin test complete!
```

---

### Option 2: Full Integration Test (After ChatComplete.cs AF Mode) 🔜 LATER

Once ChatComplete.cs supports AF mode, test with actual tool calling.

**Prerequisites:**
- ChatComplete.cs modified to support `UseAgentFramework: true`
- OpenAI API key configured
- Knowledge.Api running

**Test Queries:**
1. "How is the system?" → Should call GetSystemHealthAsync()
2. "Check OpenAI status" → Should call CheckComponentHealthAsync()
3. "Show me system performance" → Should call GetSystemMetricsAsync()
4. "Any recommendations?" → Should call GetHealthRecommendationsAsync()
5. "What components can you monitor?" → Should call GetAvailableComponentsAsync()
6. "Quick health check" → Should call GetQuickHealthOverviewAsync()

---

## Quick Registration Test (Recommended for Now)

Add this to `Knowledge.Api/Program.cs` after services are built (around line 200+):

```csharp
// TEMPORARY: Test SystemHealthPlugin AF tool registration
if (builder.Environment.IsDevelopment())
{
    var testServiceProvider = app.Services;
    KnowledgeEngine.Agents.AgentFramework.SystemHealthPluginTest.TestToolRegistration(testServiceProvider);
}
```

Then run:
```bash
cd /home/wayne/repos/ChatComplete
dotnet run --project Knowledge.Api/Knowledge.Api.csproj
```

Check console output for test results.

---

## Manual Verification Checklist

- [ ] Build succeeds without errors
- [ ] SystemHealthPlugin.cs compiles
- [ ] SystemHealthPluginTest.cs compiles
- [ ] Tool registration test runs without exceptions
- [ ] All 6 functions discovered by reflection
- [ ] AIFunctionFactory.Create() succeeds for each method
- [ ] Tools added to list successfully

---

## Function Details

| # | Function Name | Parameters | Description |
|---|---------------|------------|-------------|
| 1 | GetSystemHealthAsync | 3 params | Comprehensive health overview |
| 2 | CheckComponentHealthAsync | 2 params | Individual component status |
| 3 | GetSystemMetricsAsync | 2 params | Performance metrics |
| 4 | GetHealthRecommendationsAsync | 1 param | Health recommendations |
| 5 | GetAvailableComponentsAsync | 0 params | List monitorable components |
| 6 | GetQuickHealthOverviewAsync | 0 params | Quick status summary |

**Total Parameters:** 8 across all functions

---

## Expected Behavior

### Tool Discovery
- All public methods should be discovered via reflection
- Methods from `Object` base class should be skipped
- Each method should successfully convert to AITool via AIFunctionFactory

### Parameter Handling
- Boolean parameters with defaults should be preserved
- String parameters with defaults should be preserved
- Description attributes should be extracted for tool metadata

### Error Handling
- Plugin initialization should not throw
- Tool registration failures should be logged but not crash
- Individual tool registration errors should be caught and logged

---

## Test Results

**Date:** _Pending_
**Tester:** _Pending_
**Environment:** _Pending_

### Registration Test
- [ ] PASS: All 6 tools registered
- [ ] PASS: No exceptions during registration
- [ ] PASS: Tool count matches expected (6)
- [ ] PASS: Console output looks correct

### Integration Test (When Available)
- [ ] PASS: GetSystemHealthAsync called correctly
- [ ] PASS: CheckComponentHealthAsync called correctly
- [ ] PASS: GetSystemMetricsAsync called correctly
- [ ] PASS: GetHealthRecommendationsAsync called correctly
- [ ] PASS: GetAvailableComponentsAsync called correctly
- [ ] PASS: GetQuickHealthOverviewAsync called correctly

---

## Known Issues

None currently - plugin is a straightforward migration from SK version.

---

## Next Steps After Testing

Once SystemHealthPlugin is verified:
1. Update this document with test results
2. Move to next plugin migration:
   - ModelRecommendationAgent (3 functions) OR
   - KnowledgeAnalyticsAgent (1 function)
3. Repeat this testing pattern for each plugin

---

**Last Updated:** 2025-01-24
**Status:** Ready for testing

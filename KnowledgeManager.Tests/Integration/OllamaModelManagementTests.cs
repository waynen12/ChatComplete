using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Knowledge.Api.Services;
using KnowledgeEngine.Persistence.Sqlite;
using KnowledgeEngine.Persistence.Sqlite.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace KnowledgeManager.Tests.Integration;

/// <summary>
/// Integration tests for Ollama model management workflow
/// Tests the complete download -> verify -> delete cycle
/// </summary>
public class OllamaModelManagementTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly SqliteDbContext _dbContext;
    private readonly SqliteOllamaRepository _repository;
    private readonly OllamaApiService _ollamaService;
    private readonly OllamaDownloadService _downloadService;
    private readonly string _testDatabasePath;
    private readonly CancellationTokenSource _cancellationTokenSource;

    // Small models for testing (< 4B parameters)
    private const string SMALL_TEST_MODEL = "tinyllama:1.1b";  // ~637MB
    private const string ALTERNATIVE_SMALL_MODEL = "qwen2.5:0.5b";  // ~374MB
    
    public OllamaModelManagementTests(ITestOutputHelper output)
    {
        _output = output;
        _cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(10)); // 10 minute timeout
        
        // Create temporary database for testing
        var tempDir = Path.Combine(Path.GetTempPath(), "OllamaTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        _testDatabasePath = Path.Combine(tempDir, "test_ollama.db");
        
        // Initialize database context
        _dbContext = new SqliteDbContext(_testDatabasePath);
        _repository = new SqliteOllamaRepository(_dbContext);
        
        // Create mock configuration for Ollama service
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ollama:BaseUrl"] = "http://localhost:11434"
            })
            .Build();
            
        // Create logger
        using var loggerFactory = LoggerFactory.Create(builder => 
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        var logger = loggerFactory.CreateLogger<OllamaApiService>();
        
        // Initialize services
        var httpClient = new HttpClient();
        _ollamaService = new OllamaApiService(httpClient, logger, configuration);
        
        var downloadLogger = loggerFactory.CreateLogger<OllamaDownloadService>();
        _downloadService = new OllamaDownloadService(_ollamaService, _repository, downloadLogger);
        
        _output.WriteLine($"Test setup complete. Database: {_testDatabasePath}");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("RequiresOllama", "true")]
    public async Task DownloadVerifyDelete_SmallModel_ShouldCompleteSuccessfully()
    {
        var modelName = SMALL_TEST_MODEL;
        _output.WriteLine($"Testing complete workflow with model: {modelName}");
        
        try
        {
            // Step 1: Ensure database is initialized
            await InitializeDatabaseAsync();
            
            // Step 2: Check if Ollama is running
            await VerifyOllamaConnectionAsync();
            
            // Step 3: Ensure model is not already installed
            await CleanupExistingModelAsync(modelName);
            
            // Step 4: Start download and verify tracking
            var downloadStarted = await StartDownloadAndVerifyAsync(modelName);
            Assert.True(downloadStarted, "Download should start successfully");
            
            // Step 5: Monitor download progress
            await MonitorDownloadProgressAsync(modelName);
            
            // Step 6: Verify model is installed and available
            await VerifyModelInstallationAsync(modelName);
            
            // Step 7: Test model details retrieval
            await VerifyModelDetailsAsync(modelName);
            
            // Step 8: Clean up - delete the model
            await DeleteModelAndVerifyAsync(modelName);
            
            _output.WriteLine("✅ Complete model management workflow completed successfully!");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"❌ Test failed: {ex.Message}");
            _output.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DatabaseOperations_ShouldWorkCorrectly()
    {
        _output.WriteLine("Testing database operations without Ollama dependency");
        
        // Initialize database
        await InitializeDatabaseAsync();
        
        var testModelName = "test-model:latest";
        
        // Test download progress tracking
        var downloadRecord = new OllamaDownloadRecord
        {
            ModelName = testModelName,
            Status = "Downloading",
            BytesDownloaded = 1024 * 1024, // 1MB
            TotalBytes = 10 * 1024 * 1024, // 10MB
            PercentComplete = 10.0
        };
        
        await _repository.UpsertDownloadProgressAsync(downloadRecord);
        
        // Verify record was inserted
        var retrievedStatus = await _repository.GetDownloadStatusAsync(testModelName);
        Assert.NotNull(retrievedStatus);
        Assert.Equal(testModelName, retrievedStatus.ModelName);
        Assert.Equal("Downloading", retrievedStatus.Status);
        Assert.Equal(10.0, retrievedStatus.PercentComplete);
        
        // Test model record
        var modelRecord = new OllamaModelRecord
        {
            Name = testModelName,
            Size = 10 * 1024 * 1024,
            Family = "test",
            Status = "Ready"
        };
        
        await _repository.UpsertModelAsync(modelRecord);
        
        // Verify model was inserted
        var models = await _repository.GetInstalledModelsAsync();
        Assert.Contains(models, m => m.Name == testModelName);
        
        // Test deletion
        await _repository.DeleteModelAsync(testModelName);
        
        // Verify both records were deleted
        models = await _repository.GetInstalledModelsAsync();
        Assert.DoesNotContain(models, m => m.Name == testModelName);
        
        retrievedStatus = await _repository.GetDownloadStatusAsync(testModelName);
        Assert.Null(retrievedStatus);
        
        _output.WriteLine("✅ Database operations test completed successfully!");
    }

    private async Task InitializeDatabaseAsync()
    {
        _output.WriteLine("Initializing test database...");
        // Initialize database by getting a connection
        var connection = await _dbContext.GetConnectionAsync();
        Assert.NotNull(connection);
        _output.WriteLine("Database initialized successfully");
    }

    private async Task VerifyOllamaConnectionAsync()
    {
        _output.WriteLine("Verifying Ollama connection...");
        
        try
        {
            var models = await _ollamaService.GetInstalledModelsAsync(_cancellationTokenSource.Token);
            _output.WriteLine($"Ollama is running. Found {models.Count} installed models.");
        }
        catch (HttpRequestException ex)
        {
            _output.WriteLine($"Ollama connection failed: {ex.Message}");
            throw new SkipException("Ollama is not running. Skipping integration test.");
        }
    }

    private async Task CleanupExistingModelAsync(string modelName)
    {
        _output.WriteLine($"Checking if model {modelName} is already installed...");
        
        try
        {
            var existingModels = await _ollamaService.GetInstalledModelsAsync(_cancellationTokenSource.Token);
            if (existingModels.Any(m => m.Name.Equals(modelName, StringComparison.OrdinalIgnoreCase)))
            {
                _output.WriteLine($"Model {modelName} is already installed. Removing for clean test...");
                await _ollamaService.DeleteModelAsync(modelName, _cancellationTokenSource.Token);
                await _repository.DeleteModelAsync(modelName, _cancellationTokenSource.Token);
                
                // Wait a moment for cleanup
                await Task.Delay(2000, _cancellationTokenSource.Token);
            }
            
            _output.WriteLine("Model cleanup completed");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Cleanup warning: {ex.Message}");
            // Continue with test even if cleanup fails
        }
    }

    private async Task<bool> StartDownloadAndVerifyAsync(string modelName)
    {
        _output.WriteLine($"Starting download for model: {modelName}");
        
        var downloadStarted = await _downloadService.StartDownloadAsync(modelName, _cancellationTokenSource.Token);
        
        if (downloadStarted)
        {
            _output.WriteLine("Download started successfully");
            
            // Verify download record was created
            var downloadStatus = await _repository.GetDownloadStatusAsync(modelName, _cancellationTokenSource.Token);
            Assert.NotNull(downloadStatus);
            Assert.Equal(modelName, downloadStatus.ModelName);
            _output.WriteLine($"Download tracking confirmed. Status: {downloadStatus.Status}");
        }
        else
        {
            _output.WriteLine("Failed to start download");
        }
        
        return downloadStarted;
    }

    private async Task MonitorDownloadProgressAsync(string modelName)
    {
        _output.WriteLine($"Monitoring download progress for {modelName}...");
        
        var timeout = TimeSpan.FromMinutes(8); // 8 minute timeout for download
        var startTime = DateTime.UtcNow;
        var lastProgress = -1.0;
        
        while (DateTime.UtcNow - startTime < timeout)
        {
            var status = await _repository.GetDownloadStatusAsync(modelName, _cancellationTokenSource.Token);
            
            if (status == null)
            {
                _output.WriteLine("Download status not found");
                break;
            }
            
            // Log progress updates
            if (Math.Abs(status.PercentComplete - lastProgress) > 5.0)
            {
                _output.WriteLine($"Download progress: {status.PercentComplete:F1}% " +
                                $"({status.BytesDownloaded:N0}/{status.TotalBytes:N0} bytes) " +
                                $"Status: {status.Status}");
                lastProgress = status.PercentComplete;
            }
            
            // Check if download completed
            if (status.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
            {
                _output.WriteLine("✅ Download completed successfully!");
                return;
            }
            
            // Check if download failed
            if (status.Status.Equals("Failed", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Download failed: {status.ErrorMessage}");
            }
            
            // Wait before next check
            await Task.Delay(5000, _cancellationTokenSource.Token); // Check every 5 seconds
        }
        
        throw new TimeoutException($"Download did not complete within {timeout.TotalMinutes} minutes");
    }

    private async Task VerifyModelInstallationAsync(string modelName)
    {
        _output.WriteLine($"Verifying model installation: {modelName}");
        
        // Check via Ollama API
        var installedModels = await _ollamaService.GetInstalledModelsAsync(_cancellationTokenSource.Token);
        var model = installedModels.FirstOrDefault(m => m.Name.Equals(modelName, StringComparison.OrdinalIgnoreCase));
        
        Assert.NotNull(model);
        Assert.True(model.Size > 0, "Model size should be greater than 0");
        _output.WriteLine($"✅ Model verified via Ollama API. Size: {model.Size:N0} bytes");
        
        // Check in local database
        var localModels = await _repository.GetInstalledModelsAsync(_cancellationTokenSource.Token);
        var localModel = localModels.FirstOrDefault(m => m.Name.Equals(modelName, StringComparison.OrdinalIgnoreCase));
        
        if (localModel != null)
        {
            _output.WriteLine($"✅ Model found in local database. Status: {localModel.Status}");
        }
        else
        {
            _output.WriteLine("⚠️ Model not yet synced to local database (this is expected)");
        }
    }

    private async Task VerifyModelDetailsAsync(string modelName)
    {
        _output.WriteLine($"Retrieving model details: {modelName}");
        
        var details = await _ollamaService.GetModelDetailsAsync(modelName, _cancellationTokenSource.Token);
        
        Assert.NotNull(details);
        Assert.Equal(modelName, details.Name, StringComparer.OrdinalIgnoreCase);
        Assert.True(details.Size > 0, "Model details should include size");
        
        _output.WriteLine($"✅ Model details retrieved. " +
                         $"Size: {details.Size:N0} bytes, " +
                         $"Family: {details.Details?.Family ?? "unknown"}");
    }

    private async Task DeleteModelAndVerifyAsync(string modelName)
    {
        _output.WriteLine($"Deleting model: {modelName}");
        
        // Delete via Ollama API
        var deleted = await _ollamaService.DeleteModelAsync(modelName, _cancellationTokenSource.Token);
        Assert.True(deleted, "Model deletion should succeed");
        
        // Delete from local database
        await _repository.DeleteModelAsync(modelName, _cancellationTokenSource.Token);
        
        // Verify deletion
        var modelsAfterDeletion = await _ollamaService.GetInstalledModelsAsync(_cancellationTokenSource.Token);
        var modelStillExists = modelsAfterDeletion.Any(m => m.Name.Equals(modelName, StringComparison.OrdinalIgnoreCase));
        
        Assert.False(modelStillExists, "Model should not exist after deletion");
        
        // Verify local database cleanup
        var localModels = await _repository.GetInstalledModelsAsync(_cancellationTokenSource.Token);
        var localModelExists = localModels.Any(m => m.Name.Equals(modelName, StringComparison.OrdinalIgnoreCase));
        
        Assert.False(localModelExists, "Model should not exist in local database after deletion");
        
        _output.WriteLine("✅ Model successfully deleted and verified");
    }

    public void Dispose()
    {
        try
        {
            _cancellationTokenSource?.Cancel();
            _dbContext?.Dispose();
            
            // Clean up test database file
            if (File.Exists(_testDatabasePath))
            {
                File.Delete(_testDatabasePath);
                var directory = Path.GetDirectoryName(_testDatabasePath);
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }
        catch (Exception ex)
        {
            _output?.WriteLine($"Cleanup warning: {ex.Message}");
        }
    }
}

/// <summary>
/// Exception thrown to skip a test when prerequisites are not met
/// </summary>
public class SkipException : Exception
{
    public SkipException(string message) : base(message) { }
}
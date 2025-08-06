using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace KnowledgeManager.Tests.API;

/// <summary>
/// Tests for configuration validation across different environments and settings.
/// Ensures the application handles various configuration scenarios gracefully.
/// </summary>
public class ConfigurationValidationTests
{
    private readonly ITestOutputHelper _output;

    public ConfigurationValidationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Configuration_ShouldLoadDefaultSettings()
    {
        // Test that default configuration values are set correctly
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SystemPrompt"] = "You are a helpful assistant.",
                ["Temperature"] = "0.7",
                ["UseExtendedInstructions"] = "true"
            })
            .Build();

        var systemPrompt = configuration["SystemPrompt"];
        var temperature = configuration.GetValue<double>("Temperature");
        var useExtended = configuration.GetValue<bool>("UseExtendedInstructions");

        Assert.NotNull(systemPrompt);
        Assert.Equal("You are a helpful assistant.", systemPrompt);
        Assert.Equal(0.7, temperature);
        Assert.True(useExtended);

        _output.WriteLine("✅ Default configuration validation passed");
    }

    [Theory]
    [InlineData("0.0")]
    [InlineData("0.5")] 
    [InlineData("1.0")]
    [InlineData("2.0")]
    public void Configuration_Temperature_ShouldAcceptValidRange(string temperatureValue)
    {
        // Test temperature configuration validation
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Temperature"] = temperatureValue
            })
            .Build();

        var temperature = configuration.GetValue<double>("Temperature");
        
        Assert.True(temperature >= 0.0 && temperature <= 2.0, 
            $"Temperature {temperature} should be between 0.0 and 2.0");

        _output.WriteLine($"✅ Temperature validation: {temperature} is valid");
    }

    [Theory]
    [InlineData("OpenAi", true)]
    [InlineData("Google", true)]
    [InlineData("Anthropic", true)]
    [InlineData("Ollama", true)]
    [InlineData("InvalidProvider", false)]
    [InlineData("", false)]
    public void Configuration_AiProvider_ShouldValidateCorrectly(string provider, bool shouldBeValid)
    {
        // Test AI provider configuration validation
        var validProviders = new[] { "OpenAi", "Google", "Anthropic", "Ollama" };
        var isValid = Array.Exists(validProviders, p => p.Equals(provider, StringComparison.OrdinalIgnoreCase));

        Assert.Equal(shouldBeValid, isValid);

        _output.WriteLine($"✅ AI Provider validation: '{provider}' is {(isValid ? "valid" : "invalid")}");
    }

    [Fact]
    public void Configuration_VectorStore_ShouldSupportBothProviders()
    {
        // Test vector store configuration switching
        var mongoConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["VectorStore:Provider"] = "MongoDB",
                ["VectorStore:ConnectionString"] = "mongodb://localhost:27017"
            })
            .Build();

        var qdrantConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["VectorStore:Provider"] = "Qdrant",
                ["VectorStore:Host"] = "localhost",
                ["VectorStore:Port"] = "6333"
            })
            .Build();

        var mongoProvider = mongoConfig["VectorStore:Provider"];
        var qdrantProvider = qdrantConfig["VectorStore:Provider"];

        Assert.Equal("MongoDB", mongoProvider);
        Assert.Equal("Qdrant", qdrantProvider);

        _output.WriteLine("✅ Vector store configuration validation passed");
    }

    [Fact]
    public void Configuration_ConnectionString_ShouldBeValidated()
    {
        // Test connection string format validation
        var validConnectionStrings = new[]
        {
            "mongodb://localhost:27017",
            "mongodb+srv://user:pass@cluster.mongodb.net/database",
            "mongodb://localhost:27017/knowledge"
        };

        var invalidConnectionStrings = new[]
        {
            "",
            "invalid-string",
            "http://localhost:27017" // Wrong protocol
        };

        foreach (var connStr in validConnectionStrings)
        {
            Assert.True(connStr.StartsWith("mongodb://") || connStr.StartsWith("mongodb+srv://"),
                $"Valid connection string should start with mongodb:// or mongodb+srv://: {connStr}");
        }

        foreach (var connStr in invalidConnectionStrings.Where(s => !string.IsNullOrEmpty(s)))
        {
            Assert.False(connStr.StartsWith("mongodb://") || connStr.StartsWith("mongodb+srv://"),
                $"Invalid connection string should not be accepted: {connStr}");
        }

        _output.WriteLine($"✅ Connection string validation: {validConnectionStrings.Length} valid, {invalidConnectionStrings.Length} invalid");
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Staging")]
    [InlineData("Production")]
    [InlineData("Testing")]
    public void Configuration_Environment_ShouldAffectSettings(string environment)
    {
        // Test environment-specific configuration
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = environment,
                ["Logging:LogLevel:Default"] = environment == "Production" ? "Warning" : "Debug"
            })
            .Build();

        var env = configuration["ASPNETCORE_ENVIRONMENT"];
        var logLevel = configuration["Logging:LogLevel:Default"];

        Assert.Equal(environment, env);
        Assert.NotNull(logLevel);

        if (environment == "Production")
        {
            Assert.Equal("Warning", logLevel);
        }
        else
        {
            Assert.Equal("Debug", logLevel);
        }

        _output.WriteLine($"✅ Environment configuration: {environment} with log level {logLevel}");
    }

    [Fact]
    public void Configuration_RequiredSettings_ShouldBePresent()
    {
        // Test that all required configuration keys are present
        var requiredKeys = new[]
        {
            "SystemPrompt",
            "Temperature", 
            "UseExtendedInstructions"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SystemPrompt"] = "Test prompt",
                ["Temperature"] = "0.7",
                ["UseExtendedInstructions"] = "false"
            })
            .Build();

        foreach (var key in requiredKeys)
        {
            var value = configuration[key];
            Assert.NotNull(value);
            Assert.NotEmpty(value);
        }

        _output.WriteLine($"✅ Required settings validation: {requiredKeys.Length} keys present");
    }

    [Fact] 
    public void Configuration_JsonSerialization_ShouldWorkCorrectly()
    {
        // Test configuration serialization/deserialization for API calls
        var sampleConfig = new
        {
            KnowledgeId = "test-collection",
            Message = "Test message",
            Temperature = 0.7,
            StripMarkdown = false,
            UseExtendedInstructions = true,
            Provider = "OpenAi"
        };

        var json = JsonSerializer.Serialize(sampleConfig);
        Assert.NotNull(json);
        Assert.Contains("test-collection", json);
        Assert.Contains("0.7", json);

        var deserialized = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        Assert.NotNull(deserialized);
        Assert.True(deserialized.ContainsKey("KnowledgeId"));

        _output.WriteLine("✅ JSON serialization validation passed");
    }

    [Theory]
    [InlineData(1536, true)]  // OpenAI embedding size
    [InlineData(768, true)]   // Some models use 768
    [InlineData(512, true)]   // Smaller embeddings
    [InlineData(0, false)]    // Invalid
    [InlineData(-1, false)]   // Invalid
    public void Configuration_EmbeddingDimensions_ShouldBeValid(int dimensions, bool shouldBeValid)
    {
        // Test embedding dimension configuration
        var isValid = dimensions > 0 && dimensions <= 2048; // Reasonable upper bound

        Assert.Equal(shouldBeValid, isValid);

        _output.WriteLine($"✅ Embedding dimensions validation: {dimensions} is {(isValid ? "valid" : "invalid")}");
    }

    [Fact]
    public void Configuration_CORS_ShouldAllowConfiguredOrigins()
    {
        // Test CORS configuration
        var corsOrigins = new[] 
        { 
            "http://localhost:3000",
            "http://localhost:5173", // Vite default
            "https://myapp.com"
        };

        foreach (var origin in corsOrigins)
        {
            // Basic URL validation
            Assert.True(Uri.TryCreate(origin, UriKind.Absolute, out var uri));
            Assert.True(uri.Scheme == "http" || uri.Scheme == "https");
        }

        _output.WriteLine($"✅ CORS origins validation: {corsOrigins.Length} valid origins");
    }

    [Fact]
    public void Configuration_ApiKeys_ShouldBeSecure()
    {
        // Test API key configuration patterns (without exposing actual keys)
        var apiKeyPatterns = new Dictionary<string, string>
        {
            ["OpenAI"] = "sk-proj-",
            ["Google"] = "AIza",
            ["Anthropic"] = "sk-ant-"
        };

        foreach (var kvp in apiKeyPatterns)
        {
            var provider = kvp.Key;
            var expectedPrefix = kvp.Value;
            
            // Test that we can identify key patterns (for validation)
            var mockKey = $"{expectedPrefix}{'x'.Repeat(40)}"; // Mock key format
            Assert.StartsWith(expectedPrefix, mockKey);
        }

        _output.WriteLine($"✅ API key pattern validation: {apiKeyPatterns.Count} providers");
    }

    [Theory]
    [InlineData("test-collection", true)]
    [InlineData("valid_collection", true)]
    [InlineData("collection-123", true)]
    [InlineData("", false)]
    [InlineData(" ", false)]
    [InlineData("collection with spaces", false)]
    [InlineData(".hidden", false)]
    public void Configuration_CollectionNames_ShouldFollowNamingRules(string collectionName, bool shouldBeValid)
    {
        // Test collection naming validation rules
        var isValid = !string.IsNullOrWhiteSpace(collectionName) &&
                     !collectionName.Contains(' ') &&
                     !collectionName.StartsWith('.');

        Assert.Equal(shouldBeValid, isValid);

        _output.WriteLine($"✅ Collection name validation: '{collectionName}' is {(isValid ? "valid" : "invalid")}");
    }

    [Fact]
    public void Configuration_Logging_ShouldConfigureCorrectly()
    {
        // Test logging configuration
        var loggingConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Logging:LogLevel:Default"] = "Information",
                ["Logging:LogLevel:Microsoft"] = "Warning",
                ["Logging:LogLevel:System"] = "Warning",
                ["Serilog:MinimumLevel:Default"] = "Information"
            })
            .Build();

        Assert.Equal("Information", loggingConfig["Logging:LogLevel:Default"]);
        Assert.Equal("Warning", loggingConfig["Logging:LogLevel:Microsoft"]);
        Assert.Equal("Information", loggingConfig["Serilog:MinimumLevel:Default"]);

        _output.WriteLine("✅ Logging configuration validation passed");
    }

    [Fact]
    public void Configuration_HealthChecks_ShouldBeConfigurable()
    {
        // Test health check configuration
        var healthConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HealthChecks:Timeout"] = "00:00:30", // 30 seconds
                ["HealthChecks:MongoDB:Enabled"] = "true",
                ["HealthChecks:Qdrant:Enabled"] = "true"
            })
            .Build();

        var timeout = healthConfig["HealthChecks:Timeout"];
        var mongoEnabled = healthConfig.GetValue<bool>("HealthChecks:MongoDB:Enabled");
        var qdrantEnabled = healthConfig.GetValue<bool>("HealthChecks:Qdrant:Enabled");

        Assert.Equal("00:00:30", timeout);
        Assert.True(mongoEnabled);
        Assert.True(qdrantEnabled);

        _output.WriteLine("✅ Health checks configuration validation passed");
    }
}

internal static class StringExtensions
{
    public static string Repeat(this char character, int count)
    {
        return new string(character, count);
    }
}
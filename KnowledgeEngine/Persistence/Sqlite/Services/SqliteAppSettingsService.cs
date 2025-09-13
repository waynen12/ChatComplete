using ChatCompletion.Config;
using KnowledgeEngine.Persistence.Sqlite.Repositories;
using Microsoft.Extensions.Options;

namespace KnowledgeEngine.Persistence.Sqlite.Services;

/// <summary>
/// Service for managing application settings stored in SQLite with encryption support
/// Provides dynamic configuration updates and encrypted storage for sensitive values
/// </summary>
public class SqliteAppSettingsService
{
    private readonly SqliteAppSettingsRepository _repository;
    private readonly IOptionsMonitor<ChatCompleteSettings> _defaultSettings;

    public SqliteAppSettingsService(SqliteAppSettingsRepository repository, IOptionsMonitor<ChatCompleteSettings> defaultSettings)
    {
        _repository = repository;
        _defaultSettings = defaultSettings;
    }

    /// <summary>
    /// Gets a ChatCompleteSettings object populated with values from SQLite,
    /// falling back to appsettings.json defaults where SQLite values don't exist
    /// </summary>
    public async Task<ChatCompleteSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        // Start with defaults from appsettings.json
        var settings = new ChatCompleteSettings
        {
            OpenAIModel = _defaultSettings.CurrentValue.OpenAIModel,
            GoogleModel = _defaultSettings.CurrentValue.GoogleModel,
            OllamaBaseUrl = _defaultSettings.CurrentValue.OllamaBaseUrl,
            OllamaModel = _defaultSettings.CurrentValue.OllamaModel,
            AnthropicModel = _defaultSettings.CurrentValue.AnthropicModel,
            EmbeddingProviders = _defaultSettings.CurrentValue.EmbeddingProviders,
            ChunkCharacterLimit = _defaultSettings.CurrentValue.ChunkCharacterLimit,
            ChunkLineTokens = _defaultSettings.CurrentValue.ChunkLineTokens,
            ChunkParagraphTokens = _defaultSettings.CurrentValue.ChunkParagraphTokens,
            ChunkOverlap = _defaultSettings.CurrentValue.ChunkOverlap,
            FilePath = _defaultSettings.CurrentValue.FilePath,
            LogFileSizeLimit = _defaultSettings.CurrentValue.LogFileSizeLimit,
            SystemPrompt = _defaultSettings.CurrentValue.SystemPrompt,
            SystemPromptWithCoding = _defaultSettings.CurrentValue.SystemPromptWithCoding,
            Temperature = _defaultSettings.CurrentValue.Temperature,
            ChatMaxTurns = _defaultSettings.CurrentValue.ChatMaxTurns,
            MaxCodeFenceSize = _defaultSettings.CurrentValue.MaxCodeFenceSize,
            TruncateOversizedCodeFences = _defaultSettings.CurrentValue.TruncateOversizedCodeFences,
            Atlas = _defaultSettings.CurrentValue.Atlas,
            VectorStore = _defaultSettings.CurrentValue.VectorStore
        };

        // Override with SQLite values where available
        var llmSettings = await _repository.GetCategoryAsync("LLM", cancellationToken);
        var chatSettings = await _repository.GetCategoryAsync("Chat", cancellationToken);
        var embeddingSettings = await _repository.GetCategoryAsync("Embedding", cancellationToken);
        var documentSettings = await _repository.GetCategoryAsync("Documents", cancellationToken);

        // Apply LLM settings
        if (llmSettings.TryGetValue("OpenAI.Model", out var openAIModel) && !string.IsNullOrEmpty(openAIModel))
            settings.OpenAIModel = openAIModel;

        if (llmSettings.TryGetValue("Gemini.Model", out var geminiModel) && !string.IsNullOrEmpty(geminiModel))
            settings.GoogleModel = geminiModel;

        if (llmSettings.TryGetValue("Ollama.BaseUrl", out var ollamaBaseUrl) && !string.IsNullOrEmpty(ollamaBaseUrl))
            settings.OllamaBaseUrl = ollamaBaseUrl;

        if (llmSettings.TryGetValue("Ollama.Model", out var ollamaModel) && !string.IsNullOrEmpty(ollamaModel))
            settings.OllamaModel = ollamaModel;

        if (llmSettings.TryGetValue("Anthropic.Model", out var anthropicModel) && !string.IsNullOrEmpty(anthropicModel))
            settings.AnthropicModel = anthropicModel;

        // Apply chat settings
        if (chatSettings.TryGetValue("Chat.DefaultTemperature", out var tempStr) && double.TryParse(tempStr, out var temperature))
            settings.Temperature = temperature;

        if (chatSettings.TryGetValue("Chat.MaxTurns", out var maxTurnsStr) && int.TryParse(maxTurnsStr, out var maxTurns))
            settings.ChatMaxTurns = maxTurns;

        // Apply embedding settings
        if (embeddingSettings.TryGetValue("Embedding.Model", out var embeddingModel) && !string.IsNullOrEmpty(embeddingModel))
        {
            // Update the active provider's model name
            var activeProvider = settings.EmbeddingProviders.GetActiveProvider();
            activeProvider.ModelName = embeddingModel;
        }

        // Apply document settings
        if (documentSettings.TryGetValue("Documents.ChunkSize", out var chunkSizeStr) && int.TryParse(chunkSizeStr, out var chunkSize))
            settings.ChunkCharacterLimit = chunkSize;

        if (documentSettings.TryGetValue("Documents.ChunkOverlap", out var overlapStr) && int.TryParse(overlapStr, out var overlap))
            settings.ChunkOverlap = overlap;

        return settings;
    }

    /// <summary>
    /// Updates a specific setting in SQLite
    /// </summary>
    public async Task SetSettingAsync(string name, string? value, bool isEncrypted = false, CancellationToken cancellationToken = default)
    {
        await _repository.SetValueAsync(name, value, isEncrypted, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Gets a specific setting value from SQLite
    /// </summary>
    public async Task<string?> GetSettingAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _repository.GetValueAsync(name, cancellationToken);
    }

    /// <summary>
    /// Sets an encrypted API key
    /// </summary>
    public async Task SetApiKeyAsync(string provider, string apiKey, CancellationToken cancellationToken = default)
    {
        var keyName = $"{provider}.ApiKey";
        await _repository.SetValueAsync(keyName, apiKey, isEncrypted: true, description: $"{provider} API Key", category: "LLM", cancellationToken);
    }

    /// <summary>
    /// Gets an encrypted API key
    /// </summary>
    public async Task<string?> GetApiKeyAsync(string provider, CancellationToken cancellationToken = default)
    {
        var keyName = $"{provider}.ApiKey";
        return await _repository.GetValueAsync(keyName, cancellationToken);
    }

    /// <summary>
    /// Initializes default settings if they don't exist
    /// </summary>
    public async Task InitializeDefaultSettingsAsync(CancellationToken cancellationToken = default)
    {
        await _repository.InitializeDefaultsAsync(cancellationToken);
    }

    /// <summary>
    /// Gets all settings in a category for configuration UI
    /// </summary>
    public async Task<Dictionary<string, string?>> GetCategorySettingsAsync(string category, CancellationToken cancellationToken = default)
    {
        return await _repository.GetCategoryAsync(category, cancellationToken);
    }

    /// <summary>
    /// Updates multiple settings in a category
    /// </summary>
    public async Task UpdateCategorySettingsAsync(string category, Dictionary<string, (string? value, bool isEncrypted)> settings, CancellationToken cancellationToken = default)
    {
        foreach (var (key, (value, isEncrypted)) in settings)
        {
            await _repository.SetValueAsync(key, value, isEncrypted, category: category, cancellationToken: cancellationToken);
        }
    }
}
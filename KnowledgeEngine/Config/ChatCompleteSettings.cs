namespace ChatCompletion.Config
{
    public class ChatCompleteSettings
    {
        public MongoAtlasSettings Atlas { get; set; } = new();
        public VectorStoreSettings VectorStore { get; set; } = new();

        public string OpenAIModel { get; set; } = "gpt-3.5-turbo";
        public string GoogleModel { get; set; } = "gemini-2.5-flash";
        
        public string OllamaBaseUrl  { get; set; } = "http://localhost:11434";
        public string OllamaModel    { get; set; } = "gemma3:12b";

        public string AnthropicModel { get; set; } = "claude-sonnet-4-20250514";

        public string TextEmbeddingModelName { get; set; } = "text-embedding-ada-002";
        public int ChunkCharacterLimit { get; set; } = 4096;
        public int ChunkLineTokens { get; set; }
        public int ChunkParagraphTokens { get; set; }
        public int ChunkOverlap { get; set; }
        public string FilePath { get; set; } = string.Empty;

        public int LogFileSizeLimit { get; set; } = 10485760; // 10 MB

        public string SystemPrompt { get; set; }
        public string SystemPromptWithCoding { get; set; }

        public double Temperature { get; set; } = 0.7;
        
        public int ChatMaxTurns { get; set; } = 12;   // assistant+user pairs ⇒ 24 msgs
        
        // Code fence protection settings
        public int MaxCodeFenceSize { get; set; } = 10240; // 10KB max per code block
        public bool TruncateOversizedCodeFences { get; set; } = true;
        
        // SQLite database configuration
        public string? DatabasePath { get; set; } = null; // null = use smart default
    }
}

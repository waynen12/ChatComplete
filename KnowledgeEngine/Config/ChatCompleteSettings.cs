namespace ChatCompletion.Config
{
    public class ChatCompleteSettings
    {
        public MongoAtlasSettings Atlas { get; set; } = new();

        public string OpenAIModel { get; set; } = "gpt-3.5-turbo";

        public string TextEmbeddingModelName { get; set; } = "text-embedding-ada-002";
        public int ChunkCharacterLimit { get; set; } = 4096;
        public int ChunkLineTokens { get; set; }
        public int ChunkParagraphTokens { get; set; }
        public int ChunkOverlap { get; set; }
        public string FilePath { get; set; } = string.Empty;

        public int LogFileSizeLimit { get; set; } = 10485760; // 10 MB

        public string SystemPrompt { get; set; } =
            "You are a helpful assistant for users on a web portal. Always base your answers on the context provided and keep answers clear and accurate."
            + "Never provide awnsers that are not based on the context. If you don't know the answer, say 'I don't know'. Do not make up answers. Do not engage in Roleplay'.";
    }
}

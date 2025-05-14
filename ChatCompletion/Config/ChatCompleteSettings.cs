
namespace ChatCompletion.Config
{
    public class ChatCompleteSettings
    {
        public MongoAtlasSettings Atlas { get; set; } = new();

        public string TextEmbeddingModelName { get; set; } = "text-embedding-ada-002";
        public int ChunkCharacterLimit { get; set; } = 4096;
        public string FilePath {get; set;} = string.Empty;

        public int LogFileSizeLimit { get; set; } = 10485760; // 10 MB
    }
}
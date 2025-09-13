
namespace ChatCompletion.Config
{  
    public class MongoAtlasSettings
    {
        public string BaseUrl { get; set; } = "https://cloud.mongodb.com/api/atlas/v1.0";
        public string ProjectName { get; set; } = "";
        public string ClusterName { get; set; } = "";
        public string DatabaseName { get; set; } = "Tracker";
        public string CollectionName { get; set; } = "Tracker";
        public string SearchIndexName { get; set; } = "default";
        public string VectorField { get; set; } = "embedding";
        public int NumDimensions { get; set; } = 768;
        public string SimilarityFunction { get; set; } = "cosine";
    }
}
namespace ChatCompletion.Config
{
    public class QdrantSettings
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 6333; // REST API port (6334 for gRPC, but REST may be more stable)
        public bool UseHttps { get; set; } = false;
        public string? ApiKey { get; set; } = null; // For production deployments
        public int VectorSize { get; set; } = 1536; // Match OpenAI embeddings
        public string DistanceMetric { get; set; } = "Cosine"; // Cosine, Dot, Euclidean
        
        /// <summary>
        /// Gets the base URL for Qdrant REST API
        /// </summary>
        public string BaseUrl => $"{(UseHttps ? "https" : "http")}://{Host}:{Port}";
        
        /// <summary>
        /// Default collection configuration settings
        /// </summary>
        public QdrantCollectionConfig DefaultCollection { get; set; } = new();
    }

    public class QdrantCollectionConfig
    {
        public int DefaultSegmentNumber { get; set; } = 2;
        public int MaxSegmentSize { get; set; } = 20000;
        public int ReplicationFactor { get; set; } = 1;
        public int WriteConsistencyFactor { get; set; } = 1;
        
        /// <summary>
        /// HNSW (Hierarchical Navigable Small World) index configuration
        /// </summary>
        public HnswConfig Hnsw { get; set; } = new();
    }

    public class HnswConfig
    {
        /// <summary>
        /// Number of bi-directional links created for every new element during construction
        /// Higher values lead to better search results but slower indexing
        /// </summary>
        public int M { get; set; } = 16;
        
        /// <summary>
        /// Size of the dynamic candidate list
        /// Higher values lead to better search results but slower search
        /// </summary>
        public int EfConstruct { get; set; } = 200;
        
        /// <summary>
        /// Minimal number of connections for each element
        /// </summary>
        public int MMax { get; set; } = 16;
        
        /// <summary>
        /// Controls the recall-speed tradeoff during search
        /// Higher values lead to better recall but slower search
        /// </summary>
        public int Ef { get; set; } = 128;
    }

    public class VectorStoreSettings
    {
        public string Provider { get; set; } = "MongoDB"; // "MongoDB" or "Qdrant"
        public QdrantSettings Qdrant { get; set; } = new();
        public MongoAtlasSettings MongoDB { get; set; } = new();
    }
}

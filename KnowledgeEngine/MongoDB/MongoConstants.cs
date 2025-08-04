
namespace KnowledgeEngine.MongoDB;

public static class MongoConstants
{
    public const string AtlasBaseUrl = "https://cloud.mongodb.com/api/atlas/v1.0";
    public const string AtlasProjectUrl = $"{AtlasBaseUrl}/groups";
    public const string AtlasIndexUrl = $"{AtlasBaseUrl}/groups/{{projectId}}/processes/{{clusterName}}/fts/indexes";
    public const string AtlasSearchIndexUrl = $"{AtlasBaseUrl}/groups/{{projectId}}/clusters/{{clusterName}}/fts/indexes";
}
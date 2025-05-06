public static class MongoConstants
{
    public const string AtlasBaseUrl = "https://cloud.mongodb.com/api/atlas/v1.0";
    public const string AtlasProjectUrl = $"{AtlasBaseUrl}/groups";
    public const string AtlasIndexUrl = $"{AtlasBaseUrl}/groups/{{projectId}}/processes/{{clusterName}}/fts/indexes";
    public const string AtlasSearchIndexUrl = $"{AtlasBaseUrl}/groups/{{projectId}}/clusters/{{clusterName}}/fts/indexes";

    public const string AtlasSearchIndexName = "default";
    public const string AtlasSearchIndexType = "search";
    public const string AtlasSearchIndexDatabase = "Tracker";
    public const string AtlasSearchIndexCollection = "Tracker";
    public const string AtlasSearchIndexField = "content";
    public const string AtlasSearchIndexFieldType = "string";
    public const string AtlasSearchIndexFieldAnalyzer = "lucene.standard";
}
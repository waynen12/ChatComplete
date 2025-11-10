namespace Knowledge.Api.Constants;

/// <summary>
/// Contains constants used throughout the Knowledge API.
/// </summary>
public static class ApiConstants
{
    /// <summary>
    /// API route paths.
    /// </summary>
    public static class Routes
    {
        /// <summary>Base API route prefix.</summary>
        public const string Api = "/api";
        /// <summary>Knowledge management route.</summary>
        public const string Knowledge = "/knowledge";
        /// <summary>Chat endpoint route.</summary>
        public const string Chat = "/chat";
        /// <summary>Ollama service route.</summary>
        public const string Ollama = "/ollama";
        /// <summary>Analytics endpoint route.</summary>
        public const string Analytics = "/analytics";
        /// <summary>Health check route.</summary>
        public const string Health = "/health";
        /// <summary>Ping endpoint route.</summary>
        public const string Ping = "/ping";
        /// <summary>Ollama models sub-route.</summary>
        public const string OllamaModels = "/models";
    }

    /// <summary>
    /// Configuration section names.
    /// </summary>
    public static class ConfigSections
    {
        /// <summary>Main application settings section name.</summary>
        public const string ChatCompleteSettings = "ChatCompleteSettings";
        /// <summary>CORS configuration section name.</summary>
        public const string Cors = "Cors";
    }

    /// <summary>
    /// CORS policy names.
    /// </summary>
    public static class CorsPolicies
    {
        /// <summary>CORS policy name for development frontend.</summary>
        public const string DevFrontend = "DevFrontend";
    }

    /// <summary>
    /// Environment variable names.
    /// </summary>
    public static class EnvironmentVariables
    {
        /// <summary>ASP.NET Core environment variable (Development, Production, etc.).</summary>
        public const string AspNetCoreEnvironment = "ASPNETCORE_ENVIRONMENT";
        /// <summary>Indicates if the application is running in a container.</summary>
        public const string DotNetRunningInContainer = "DOTNET_RUNNING_IN_CONTAINER";
        /// <summary>OpenAI API key environment variable.</summary>
        public const string OpenAiApiKey = "OPENAI_API_KEY";
    }

    /// <summary>
    /// Swagger and OpenAPI tags.
    /// </summary>
    public static class Tags
    {
        /// <summary>Knowledge management API tag.</summary>
        public const string Knowledge = "Knowledge";
        /// <summary>Chat API tag.</summary>
        public const string Chat = "Chat";
        /// <summary>Health check API tag.</summary>
        public const string Health = "Health";
        /// <summary>Ollama service API tag.</summary>
        public const string Ollama = "Ollama";
        /// <summary>Analytics API tag.</summary>
        public const string Analytics = "Analytics";
    }

    /// <summary>
    /// File system paths.
    /// </summary>
    public static class Paths
    {
        /// <summary>Application data directory path (container default).</summary>
        public const string AppData = "/app/data";
        /// <summary>Swagger JSON endpoint path.</summary>
        public const string SwaggerEndpoint = "/swagger/v1/swagger.json";
    }

    /// <summary>
    /// Status values for health checks.
    /// </summary>
    public static class HealthStatus
    {
        /// <summary>System is fully operational.</summary>
        public const string Healthy = "healthy";
        /// <summary>System is operational with warnings.</summary>
        public const string Warning = "warning";
        /// <summary>System is not operational.</summary>
        public const string Unhealthy = "unhealthy";
        /// <summary>System is operational with reduced performance.</summary>
        public const string Degraded = "degraded";
        /// <summary>System encountered an error.</summary>
        public const string Error = "error";
        /// <summary>System status is unknown.</summary>
        public const string Unknown = "unknown";
    }

    /// <summary>
    /// Health check component names.
    /// </summary>
    public static class HealthComponents
    {
        /// <summary>Vector store component (MongoDB/Qdrant).</summary>
        public const string VectorStore = "VectorStore";
        /// <summary>Disk space component.</summary>
        public const string DiskSpace = "DiskSpace";
        /// <summary>Memory component.</summary>
        public const string Memory = "Memory";
    }

    /// <summary>
    /// Common response messages.
    /// </summary>
    public static class Messages
    {
        /// <summary>Error message when no files are uploaded.</summary>
        public const string NoFilesUploaded = "No files uploaded.";
        /// <summary>Ping endpoint response message.</summary>
        public const string Pong = "pong";
        /// <summary>Error message when data directory is not found.</summary>
        public const string DataDirectoryNotFound = "Data directory not found";
        /// <summary>Error message when Ollama command fails to start.</summary>
        public const string FailedToStartOllamaCommand = "Failed to start ollama command";
        /// <summary>Error message when fetching Ollama models fails.</summary>
        public const string FailedToFetchOllamaModels = "Failed to fetch Ollama models";
    }

    /// <summary>
    /// Content types and media types.
    /// </summary>
    public static class ContentTypes
    {
        /// <summary>Multipart form data content type.</summary>
        public const string MultipartFormData = "multipart/form-data";
        /// <summary>JSON content type.</summary>
        public const string ApplicationJson = "application/json";
    }
}
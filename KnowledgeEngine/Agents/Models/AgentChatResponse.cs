using KnowledgeEngine.Models;

namespace KnowledgeEngine.Agents.Models;

public class AgentChatResponse
{
    public string Response { get; set; } = string.Empty;
    public List<AgentToolExecution> ToolExecutions { get; set; } = new();
    public List<KnowledgeSearchResult> TraditionalSearchResults { get; set; } = new();
    public bool UsedAgentCapabilities { get; set; }
}

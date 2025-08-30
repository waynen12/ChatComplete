namespace KnowledgeEngine.Agents.Models;

public class AgentToolExecution
{
    public string ToolName { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public bool Success { get; set; }
}

namespace DeltaSupportHub.Api.Models;

public sealed class AutomationRule
{
    public int Id { get; set; }
    public required string Keyword { get; set; }
    public required string SystemArea { get; set; }
    public PriorityLevel Priority { get; set; }
    public required string Assignee { get; set; }
    public required string TagsCsv { get; set; }
    public required string AutomationHint { get; set; }
    public required string SuggestedReply { get; set; }
    public int Weight { get; set; }
}

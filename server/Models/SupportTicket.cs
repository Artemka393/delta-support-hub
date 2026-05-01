namespace DeltaSupportHub.Api.Models;

public sealed class SupportTicket
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Requester { get; set; }
    public required string System { get; set; }
    public required string Description { get; set; }
    public PriorityLevel Priority { get; set; }
    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public required string Assignee { get; set; }
    public required string TagsCsv { get; set; }
    public required string AutomationHint { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset DueAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
}

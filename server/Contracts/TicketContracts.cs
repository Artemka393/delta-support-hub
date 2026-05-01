using DeltaSupportHub.Api.Models;

namespace DeltaSupportHub.Api.Contracts;

public sealed record CreateTicketRequest(
    string Title,
    string Requester,
    string System,
    string Description);

public sealed record ChangeTicketStatusRequest(TicketStatus Status);

public sealed record TicketResponse(
    int Id,
    string Title,
    string Requester,
    string System,
    string Description,
    PriorityLevel Priority,
    TicketStatus Status,
    string Assignee,
    string[] Tags,
    string AutomationHint,
    DateTimeOffset CreatedAt,
    DateTimeOffset DueAt);

public sealed record TicketClassification(
    PriorityLevel Priority,
    string Assignee,
    string[] Tags,
    string AutomationHint,
    string SuggestedReply);

public sealed record KnowledgeArticleResponse(
    int Id,
    string Title,
    string System,
    string Problem,
    string Resolution,
    string[] Tags);

public sealed record DashboardSummary(
    int OpenCount,
    int InProgressCount,
    int ResolvedTodayCount,
    int OverdueCount,
    int AutomationCoveragePercent);

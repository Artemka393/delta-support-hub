using DeltaSupportHub.Api.Contracts;
using DeltaSupportHub.Api.Models;

namespace DeltaSupportHub.Api.Services;

public sealed class TicketClassifier
{
    public TicketClassification Classify(CreateTicketRequest request, IEnumerable<AutomationRule> rules)
    {
        var source = $"{request.Title} {request.Description} {request.System}".ToLowerInvariant();
        var system = request.System.Trim();

        var matchedRule = rules
            .Where(rule => source.Contains(rule.Keyword.ToLowerInvariant()) || string.Equals(rule.SystemArea, system, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(rule => rule.Weight)
            .FirstOrDefault();

        if (matchedRule is null)
        {
            return new TicketClassification(
                PriorityLevel.Medium,
                "Первая линия",
                ["triage"],
                "Уточнить систему, шаги воспроизведения и ожидаемый результат.",
                "Приняли обращение. Уточним детали и передадим его в профильный контур.");
        }

        return new TicketClassification(
            matchedRule.Priority,
            matchedRule.Assignee,
            SplitTags(matchedRule.TagsCsv),
            matchedRule.AutomationHint,
            matchedRule.SuggestedReply);
    }

    public static string[] SplitTags(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

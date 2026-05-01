namespace DeltaSupportHub.Api.Models;

public sealed class KnowledgeArticle
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string System { get; set; }
    public required string Problem { get; set; }
    public required string Resolution { get; set; }
    public required string TagsCsv { get; set; }
}

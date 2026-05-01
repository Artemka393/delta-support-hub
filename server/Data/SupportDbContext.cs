using DeltaSupportHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DeltaSupportHub.Api.Data;

public sealed class SupportDbContext(DbContextOptions<SupportDbContext> options) : DbContext(options)
{
    public DbSet<SupportTicket> Tickets => Set<SupportTicket>();
    public DbSet<AutomationRule> AutomationRules => Set<AutomationRule>();
    public DbSet<KnowledgeArticle> KnowledgeArticles => Set<KnowledgeArticle>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SupportTicket>(entity =>
        {
            entity.Property(ticket => ticket.Title).HasMaxLength(180);
            entity.Property(ticket => ticket.Requester).HasMaxLength(120);
            entity.Property(ticket => ticket.System).HasMaxLength(80);
            entity.Property(ticket => ticket.Assignee).HasMaxLength(120);
            entity.Property(ticket => ticket.TagsCsv).HasMaxLength(240);
            entity.Property(ticket => ticket.Priority).HasConversion<string>().HasMaxLength(20);
            entity.Property(ticket => ticket.Status).HasConversion<string>().HasMaxLength(30);
            entity.HasIndex(ticket => new { ticket.Status, ticket.Priority });
        });

        modelBuilder.Entity<AutomationRule>(entity =>
        {
            entity.Property(rule => rule.Keyword).HasMaxLength(80);
            entity.Property(rule => rule.SystemArea).HasMaxLength(80);
            entity.Property(rule => rule.Assignee).HasMaxLength(120);
            entity.Property(rule => rule.TagsCsv).HasMaxLength(240);
            entity.Property(rule => rule.Priority).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<KnowledgeArticle>(entity =>
        {
            entity.Property(article => article.Title).HasMaxLength(180);
            entity.Property(article => article.System).HasMaxLength(80);
            entity.Property(article => article.TagsCsv).HasMaxLength(240);
        });
    }
}

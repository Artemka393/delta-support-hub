using System.Text.Json.Serialization;
using DeltaSupportHub.Api.Contracts;
using DeltaSupportHub.Api.Data;
using DeltaSupportHub.Api.Models;
using DeltaSupportHub.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<SupportDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SupportDb")));

builder.Services.AddScoped<TicketClassifier>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var frontendOrigins = builder.Configuration
    .GetSection("Cors:FrontendOrigins")
    .Get<string[]>() ?? ["http://localhost:5173"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(frontendOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SupportDbContext>();
    await SeedData.EnsureSeededAsync(db);
}

var api = app.MapGroup("/api");

api.MapGet("/dashboard/summary", async (SupportDbContext db) =>
{
    var today = DateTimeOffset.UtcNow.Date;
    var openCount = await db.Tickets.CountAsync(ticket => ticket.Status == TicketStatus.Open);
    var inProgressCount = await db.Tickets.CountAsync(ticket => ticket.Status == TicketStatus.InProgress);
    var resolvedTodayCount = await db.Tickets.CountAsync(ticket =>
        ticket.Status == TicketStatus.Resolved &&
        ticket.ResolvedAt.HasValue &&
        ticket.ResolvedAt.Value.Date == today);
    var overdueCount = await db.Tickets.CountAsync(ticket => ticket.Status != TicketStatus.Resolved && ticket.DueAt < DateTimeOffset.UtcNow);
    var classifiedCount = await db.Tickets.CountAsync(ticket => ticket.TagsCsv != "triage");
    var totalCount = await db.Tickets.CountAsync();
    var automationCoverage = totalCount == 0 ? 0 : (int)Math.Round(classifiedCount * 100m / totalCount);

    return Results.Ok(new DashboardSummary(openCount, inProgressCount, resolvedTodayCount, overdueCount, automationCoverage));
});

api.MapGet("/tickets", async (SupportDbContext db, TicketStatus? status, PriorityLevel? priority) =>
{
    var query = db.Tickets.AsNoTracking();

    if (status is not null)
    {
        query = query.Where(ticket => ticket.Status == status);
    }

    if (priority is not null)
    {
        query = query.Where(ticket => ticket.Priority == priority);
    }

    var tickets = await query
        .OrderBy(ticket => ticket.Status == TicketStatus.Resolved)
        .ThenByDescending(ticket => ticket.Priority)
        .ThenBy(ticket => ticket.DueAt)
        .Select(ticket => ToResponse(ticket))
        .ToListAsync();

    return Results.Ok(tickets);
});

api.MapPost("/tickets", async (CreateTicketRequest request, SupportDbContext db, TicketClassifier classifier) =>
{
    if (string.IsNullOrWhiteSpace(request.Title) ||
        string.IsNullOrWhiteSpace(request.Requester) ||
        string.IsNullOrWhiteSpace(request.Description))
    {
        return Results.BadRequest(new { message = "Title, requester and description are required." });
    }

    var rules = await db.AutomationRules.AsNoTracking().ToListAsync();
    var classification = classifier.Classify(request, rules);
    var slaHours = classification.Priority switch
    {
        PriorityLevel.Critical => 4,
        PriorityLevel.High => 24,
        PriorityLevel.Medium => 48,
        _ => 96
    };

    var ticket = new SupportTicket
    {
        Title = request.Title.Trim(),
        Requester = request.Requester.Trim(),
        System = request.System.Trim(),
        Description = request.Description.Trim(),
        Priority = classification.Priority,
        Status = TicketStatus.Open,
        Assignee = classification.Assignee,
        TagsCsv = string.Join(',', classification.Tags),
        AutomationHint = classification.AutomationHint,
        CreatedAt = DateTimeOffset.UtcNow,
        DueAt = DateTimeOffset.UtcNow.AddHours(slaHours)
    };

    db.Tickets.Add(ticket);
    await db.SaveChangesAsync();

    return Results.Created($"/api/tickets/{ticket.Id}", ToResponse(ticket));
});

api.MapPatch("/tickets/{id:int}/status", async (int id, ChangeTicketStatusRequest request, SupportDbContext db) =>
{
    var ticket = await db.Tickets.FindAsync(id);

    if (ticket is null)
    {
        return Results.NotFound();
    }

    ticket.Status = request.Status;
    ticket.ResolvedAt = request.Status == TicketStatus.Resolved ? DateTimeOffset.UtcNow : null;
    await db.SaveChangesAsync();

    return Results.Ok(ToResponse(ticket));
});

api.MapPost("/automation/classify", async (CreateTicketRequest request, SupportDbContext db, TicketClassifier classifier) =>
{
    var rules = await db.AutomationRules.AsNoTracking().ToListAsync();
    return Results.Ok(classifier.Classify(request, rules));
});

api.MapGet("/knowledge", async (SupportDbContext db) =>
{
    var articles = await db.KnowledgeArticles
        .AsNoTracking()
        .OrderBy(article => article.System)
        .ThenBy(article => article.Title)
        .Select(article => new KnowledgeArticleResponse(
            article.Id,
            article.Title,
            article.System,
            article.Problem,
            article.Resolution,
            TicketClassifier.SplitTags(article.TagsCsv)))
        .ToListAsync();

    return Results.Ok(articles);
});

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "SupportFlow Hub API" }));

app.Run();

static TicketResponse ToResponse(SupportTicket ticket) =>
    new(
        ticket.Id,
        ticket.Title,
        ticket.Requester,
        ticket.System,
        ticket.Description,
        ticket.Priority,
        ticket.Status,
        ticket.Assignee,
        TicketClassifier.SplitTags(ticket.TagsCsv),
        ticket.AutomationHint,
        ticket.CreatedAt,
        ticket.DueAt);

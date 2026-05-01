using DeltaSupportHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DeltaSupportHub.Api.Data;

public static class SeedData
{
    public static async Task EnsureSeededAsync(SupportDbContext db)
    {
        await db.Database.EnsureCreatedAsync();

        if (await db.Tickets.AnyAsync())
        {
            return;
        }

        db.AutomationRules.AddRange(
            new AutomationRule
            {
                Keyword = "ошибка",
                SystemArea = "CRM",
                Priority = PriorityLevel.High,
                Assignee = "Контур CRM",
                TagsCsv = "incident,logs,crm",
                AutomationHint = "Собрать текст ошибки, correlation id и проверить последние записи журнала.",
                SuggestedReply = "Приняли обращение в работу. Проверяем журналы приложения и доступность связанных сервисов.",
                Weight = 100
            },
            new AutomationRule
            {
                Keyword = "500",
                SystemArea = "CRM",
                Priority = PriorityLevel.High,
                Assignee = "Контур CRM",
                TagsCsv = "http-500,api,crm",
                AutomationHint = "Проверить API, договорный сервис и последнюю запись в журнале интеграции.",
                SuggestedReply = "Проверяем ошибку сервера и связанные интеграции. Сообщим статус после диагностики.",
                Weight = 120
            },
            new AutomationRule
            {
                Keyword = "доступ",
                SystemArea = "Identity",
                Priority = PriorityLevel.Low,
                Assignee = "Контур доступа",
                TagsCsv = "access,approval,security",
                AutomationHint = "Проверить согласование руководителя и текущие роли пользователя.",
                SuggestedReply = "Для изменения доступа нужно подтверждение руководителя. После согласования применим роль.",
                Weight = 80
            },
            new AutomationRule
            {
                Keyword = "выгруз",
                SystemArea = "DWH",
                Priority = PriorityLevel.Medium,
                Assignee = "Контур отчетности",
                TagsCsv = "reporting,sql,dwh",
                AutomationHint = "Проверить регламентную задачу, период и контрольные суммы выгрузки.",
                SuggestedReply = "Проверим статус регламентной выгрузки и вернемся с результатом сверки.",
                Weight = 90
            });

        db.KnowledgeArticles.AddRange(
            new KnowledgeArticle
            {
                Title = "Ошибка 500 в карточке договора",
                System = "CRM",
                Problem = "Пользователь видит ошибку при открытии договора.",
                Resolution = "Проверить correlation id в логах API, состояние интеграции с договорным сервисом и наличие записи в MS SQL.",
                TagsCsv = "crm,http-500,logs"
            },
            new KnowledgeArticle
            {
                Title = "Повторная выгрузка платежей",
                System = "DWH",
                Problem = "Нужна повторная выгрузка данных за период.",
                Resolution = "Проверить закрытие периода, запустить регламентную задачу и сверить контрольные суммы.",
                TagsCsv = "dwh,export,sql"
            },
            new KnowledgeArticle
            {
                Title = "Смена роли сотрудника",
                System = "Identity",
                Problem = "Сотруднику нужен доступ к новому модулю.",
                Resolution = "Получить согласование руководителя, назначить роль и проверить аудит изменения прав.",
                TagsCsv = "access,security"
            });

        db.Tickets.AddRange(
            new SupportTicket
            {
                Title = "Не открывается карточка договора",
                Requester = "Ирина Соколова",
                System = "CRM",
                Description = "При открытии договора появляется ошибка 500, проблема повторяется у двух менеджеров.",
                Priority = PriorityLevel.High,
                Status = TicketStatus.Open,
                Assignee = "Контур CRM",
                TagsCsv = "crm,http-500,договоры",
                AutomationHint = "Проверить последние ошибки API по договору и доступность сервиса интеграции.",
                CreatedAt = DateTimeOffset.UtcNow.AddHours(-7),
                DueAt = DateTimeOffset.UtcNow.AddHours(17)
            },
            new SupportTicket
            {
                Title = "Нужна выгрузка платежей за месяц",
                Requester = "Алексей Романов",
                System = "DWH",
                Description = "Финансовому отделу нужна повторная выгрузка платежей за апрель.",
                Priority = PriorityLevel.Medium,
                Status = TicketStatus.InProgress,
                Assignee = "Контур отчетности",
                TagsCsv = "dwh,выгрузка,финансы",
                AutomationHint = "Запустить регламентную выгрузку и сверить количество строк с журналом загрузок.",
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
                DueAt = DateTimeOffset.UtcNow.AddDays(1)
            },
            new SupportTicket
            {
                Title = "Сбросить роль пользователю",
                Requester = "Мария Никифорова",
                System = "Identity",
                Description = "После перевода в новый отдел сотруднику нужны права на модуль лизинговых сделок.",
                Priority = PriorityLevel.Low,
                Status = TicketStatus.WaitingForUser,
                Assignee = "Контур доступа",
                TagsCsv = "access,role,approval",
                AutomationHint = "Запросить подтверждение руководителя и применить роль через консоль администрирования.",
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
                DueAt = DateTimeOffset.UtcNow.AddDays(2)
            });

        await db.SaveChangesAsync();
    }
}

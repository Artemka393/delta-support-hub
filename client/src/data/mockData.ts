import type { DashboardSummary, KnowledgeArticle, Ticket, TicketClassification } from '../types';

export const demoTickets: Ticket[] = [
  {
    id: 1042,
    title: 'Не открывается карточка договора',
    requester: 'Ирина Соколова',
    system: 'CRM',
    description: 'При открытии договора появляется ошибка 500, проблема повторяется у двух менеджеров.',
    priority: 'High',
    status: 'Open',
    assignee: 'Контур CRM',
    tags: ['crm', 'http-500', 'договоры'],
    automationHint: 'Проверить последние ошибки API по договору и доступность сервиса интеграции.',
    createdAt: '2026-04-30T09:14:00Z',
    dueAt: '2026-05-01T09:14:00Z'
  },
  {
    id: 1041,
    title: 'Нужна выгрузка платежей за месяц',
    requester: 'Алексей Романов',
    system: 'DWH',
    description: 'Финансовому отделу нужна повторная выгрузка платежей за апрель.',
    priority: 'Medium',
    status: 'InProgress',
    assignee: 'Контур отчетности',
    tags: ['dwh', 'выгрузка', 'финансы'],
    automationHint: 'Запустить регламентную выгрузку и сверить количество строк с журналом загрузок.',
    createdAt: '2026-04-30T07:40:00Z',
    dueAt: '2026-05-02T07:40:00Z'
  },
  {
    id: 1038,
    title: 'Сбросить роль пользователю',
    requester: 'Мария Никифорова',
    system: 'Identity',
    description: 'После перевода в новый отдел сотруднику нужны права на модуль лизинговых сделок.',
    priority: 'Low',
    status: 'WaitingForUser',
    assignee: 'Контур доступа',
    tags: ['access', 'role', 'approval'],
    automationHint: 'Запросить подтверждение руководителя и применить роль через консоль администрирования.',
    createdAt: '2026-04-29T11:20:00Z',
    dueAt: '2026-05-03T11:20:00Z'
  }
];

export const demoKnowledge: KnowledgeArticle[] = [
  {
    id: 1,
    title: 'Ошибка 500 в карточке договора',
    system: 'CRM',
    problem: 'Пользователь видит ошибку при открытии договора.',
    resolution: 'Проверить correlation id в логах API, состояние интеграции с договорным сервисом и наличие записи в MS SQL.',
    tags: ['crm', 'http-500', 'logs']
  },
  {
    id: 2,
    title: 'Повторная выгрузка платежей',
    system: 'DWH',
    problem: 'Нужна повторная выгрузка данных за период.',
    resolution: 'Проверить закрытие периода, запустить регламентную задачу и сверить контрольные суммы.',
    tags: ['dwh', 'export', 'sql']
  },
  {
    id: 3,
    title: 'Смена роли сотрудника',
    system: 'Identity',
    problem: 'Сотруднику нужен доступ к новому модулю.',
    resolution: 'Получить согласование руководителя, назначить роль и проверить аудит изменения прав.',
    tags: ['access', 'security']
  }
];

export const demoSummary: DashboardSummary = {
  openCount: 1,
  inProgressCount: 1,
  resolvedTodayCount: 5,
  overdueCount: 1,
  automationCoveragePercent: 72
};

export function classifyLocally(title: string, description: string, system: string): TicketClassification {
  const text = `${title} ${description} ${system}`.toLowerCase();

  if (text.includes('500') || text.includes('ошибка') || text.includes('не открывается')) {
    return {
      priority: 'High',
      assignee: 'Контур CRM',
      tags: ['incident', 'logs', system.toLowerCase()],
      automationHint: 'Собрать текст ошибки, correlation id и проверить последние записи журнала.',
      suggestedReply: 'Приняли обращение в работу. Проверяем журналы приложения и доступность связанных сервисов.'
    };
  }

  if (text.includes('доступ') || text.includes('роль') || text.includes('права')) {
    return {
      priority: 'Low',
      assignee: 'Контур доступа',
      tags: ['access', 'approval'],
      automationHint: 'Проверить согласование руководителя и текущие роли пользователя.',
      suggestedReply: 'Для изменения доступа нужно подтверждение руководителя. После согласования применим роль.'
    };
  }

  if (text.includes('выгруз') || text.includes('отчет') || text.includes('sql')) {
    return {
      priority: 'Medium',
      assignee: 'Контур отчетности',
      tags: ['reporting', 'sql'],
      automationHint: 'Проверить регламентную задачу, период и контрольные суммы выгрузки.',
      suggestedReply: 'Проверим статус регламентной выгрузки и вернемся с результатом сверки.'
    };
  }

  return {
    priority: 'Medium',
    assignee: 'Первая линия',
    tags: ['triage'],
    automationHint: 'Уточнить систему, шаги воспроизведения и ожидаемый результат.',
    suggestedReply: 'Приняли обращение. Уточним детали и передадим его в профильный контур.'
  };
}

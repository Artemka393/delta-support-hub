<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue';
import { classifyLocally, demoKnowledge, demoSummary, demoTickets } from './data/mockData';
import { supportApi } from './services/api';
import type { CreateTicketRequest, DashboardSummary, KnowledgeArticle, Ticket, TicketClassification, TicketStatus } from './types';

const tickets = ref<Ticket[]>([]);
const articles = ref<KnowledgeArticle[]>([]);
const summary = ref<DashboardSummary>(demoSummary);
const selectedTicketId = ref<number | null>(null);
const activeStatus = ref<TicketStatus | 'All'>('All');
const apiMode = ref<'online' | 'demo'>('demo');
const isLoading = ref(true);
const isMetricsOpen = ref(false);
const formError = ref('');

const draft = reactive<CreateTicketRequest>({
  title: '',
  requester: '',
  system: 'CRM',
  description: ''
});

const classification = ref<TicketClassification>(classifyLocally('', '', 'CRM'));

const statusLabels: Record<TicketStatus | 'All', string> = {
  All: 'Все',
  Open: 'Новые',
  InProgress: 'В работе',
  WaitingForUser: 'Ждем пользователя',
  Resolved: 'Решены'
};

const priorityLabels: Record<Ticket['priority'], string> = {
  Low: 'Низкий',
  Medium: 'Средний',
  High: 'Высокий',
  Critical: 'Критичный'
};

const selectedTicket = computed(() => tickets.value.find((ticket) => ticket.id === selectedTicketId.value) ?? tickets.value[0]);

const filteredTickets = computed(() => {
  if (activeStatus.value === 'All') {
    return tickets.value;
  }

  return tickets.value.filter((ticket) => ticket.status === activeStatus.value);
});

const slaRiskCount = computed(() => tickets.value.filter((ticket) => new Date(ticket.dueAt).getTime() < Date.now()).length);

const queueHealth = computed(() => {
  if (slaRiskCount.value > 0) {
    return 'Требует внимания';
  }

  if (tickets.value.some((ticket) => ticket.priority === 'Critical' || ticket.priority === 'High')) {
    return 'Есть высокий приоритет';
  }

  return 'Стабильно';
});

function formatDate(value: string) {
  return new Intl.DateTimeFormat('ru-RU', {
    day: '2-digit',
    month: 'short',
    hour: '2-digit',
    minute: '2-digit'
  }).format(new Date(value));
}

async function loadWorkspace() {
  isLoading.value = true;

  try {
    const [summaryResult, ticketsResult, knowledgeResult] = await Promise.all([
      supportApi.getSummary(),
      supportApi.getTickets(),
      supportApi.getKnowledge()
    ]);

    summary.value = summaryResult;
    tickets.value = ticketsResult;
    articles.value = knowledgeResult;
    apiMode.value = 'online';
  } catch {
    summary.value = demoSummary;
    tickets.value = [...demoTickets];
    articles.value = [...demoKnowledge];
    apiMode.value = 'demo';
  } finally {
    selectedTicketId.value = tickets.value[0]?.id ?? null;
    isLoading.value = false;
  }
}

async function previewClassification() {
  const payload = { ...draft };

  if (!payload.title && !payload.description) {
    classification.value = classifyLocally('', '', payload.system);
    return;
  }

  try {
    classification.value = await supportApi.classify(payload);
    apiMode.value = 'online';
  } catch {
    classification.value = classifyLocally(payload.title, payload.description, payload.system);
    apiMode.value = 'demo';
  }
}

async function createTicket() {
  formError.value = '';

  if (!draft.title.trim() || !draft.requester.trim() || !draft.description.trim()) {
    formError.value = 'Заполните тему, автора и описание обращения.';
    return;
  }

  const payload = { ...draft };

  try {
    const created = await supportApi.createTicket(payload);
    tickets.value = [created, ...tickets.value];
    selectedTicketId.value = created.id;
    apiMode.value = 'online';
  } catch {
    const local = classifyLocally(payload.title, payload.description, payload.system);
    const created: Ticket = {
      id: Math.max(...tickets.value.map((ticket) => ticket.id), 1000) + 1,
      ...payload,
      priority: local.priority,
      status: 'Open',
      assignee: local.assignee,
      tags: local.tags,
      automationHint: local.automationHint,
      createdAt: new Date().toISOString(),
      dueAt: new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString()
    };

    tickets.value = [created, ...tickets.value];
    selectedTicketId.value = created.id;
    classification.value = local;
    apiMode.value = 'demo';
  }

  draft.title = '';
  draft.requester = '';
  draft.description = '';
}

async function setStatus(ticket: Ticket, status: TicketStatus) {
  if (ticket.status === status) {
    return;
  }

  try {
    const updated = await supportApi.updateStatus(ticket.id, status);
    tickets.value = tickets.value.map((item) => (item.id === ticket.id ? updated : item));
    apiMode.value = 'online';
  } catch {
    tickets.value = tickets.value.map((item) => (item.id === ticket.id ? { ...item, status } : item));
    apiMode.value = 'demo';
  }
}

onMounted(async () => {
  await loadWorkspace();
  await previewClassification();
});
</script>

<template>
  <main class="console">
    <header class="masthead">
      <div class="brand">
        <span class="brand-mark">SF</span>
        <div>
          <strong>SupportFlow Hub</strong>
          <span>service desk console</span>
        </div>
      </div>

      <nav class="nav">
        <a href="#queue" class="active">Очередь</a>
        <a href="#automation">Заведение</a>
        <a href="#knowledge">Решения</a>
      </nav>

      <div class="masthead-actions">
        <button
          type="button"
          class="metrics-trigger"
          :aria-expanded="isMetricsOpen"
          aria-controls="metrics-widget"
          @click="isMetricsOpen = !isMetricsOpen"
        >
          <span>{{ summary.openCount + summary.inProgressCount }}</span>
          Сводка
        </button>

        <div class="status-pill" :class="apiMode">
          <span></span>
          {{ apiMode === 'online' ? 'API online' : 'demo data' }}
        </div>
      </div>
    </header>

    <section v-if="isMetricsOpen" id="metrics-widget" class="metrics-widget" aria-label="Показатели очереди">
      <div class="metrics-widget-head">
        <div>
          <p class="eyebrow">Shift snapshot</p>
          <h2>Сводка очереди</h2>
        </div>
        <button type="button" aria-label="Закрыть сводку" @click="isMetricsOpen = false">×</button>
      </div>

      <div class="metrics-grid">
        <article class="metric open">
          <span>Новые</span>
          <strong>{{ summary.openCount }}</strong>
        </article>
        <article class="metric progress">
          <span>В работе</span>
          <strong>{{ summary.inProgressCount }}</strong>
        </article>
        <article class="metric done">
          <span>Решено</span>
          <strong>{{ summary.resolvedTodayCount }}</strong>
        </article>
        <article class="metric risk">
          <span>SLA риск</span>
          <strong>{{ summary.overdueCount || slaRiskCount }}</strong>
        </article>
        <article class="metric automation">
          <span>Авто</span>
          <strong>{{ summary.automationCoveragePercent }}%</strong>
        </article>
      </div>
    </section>

    <section class="hero-console">
      <div class="hero-copy">
        <p class="eyebrow">Open support · Delta leasing · 2026</p>
        <h1>Support desk, который держит SLA <em>под контролем</em></h1>
        <p class="hero-lede">
          Темная операторская сцена, плотная очередь и автоматические подсказки для первой линии.
        </p>
      </div>

      <div class="hero-visual" aria-hidden="true">
        <div class="visual-card visual-card-main">
          <span class="visual-kicker">incident queue</span>
          <strong>CRM / 500</strong>
          <small>logs · договоры · high</small>
          <div class="visual-bars">
            <i></i>
            <i></i>
            <i></i>
            <i></i>
            <i></i>
          </div>
        </div>
        <div class="visual-card visual-card-side">
          <span class="visual-kicker">auto reply</span>
          <strong>готов шаблон</strong>
          <small>проверяем журналы API</small>
        </div>
        <div class="visual-stamp">SLA</div>
      </div>

      <div class="health-board">
        <span>Состояние очереди</span>
        <strong>{{ queueHealth }}</strong>
        <small>{{ summary.automationCoveragePercent }}% обращений получают автоподсказку</small>
      </div>
    </section>

    <section id="queue" class="queue-layout">
      <aside class="filter-rail">
        <p class="eyebrow">Фильтр</p>
        <h2>Статусы</h2>

        <div class="tabs" role="tablist">
          <button
            v-for="(label, status) in statusLabels"
            :key="status"
            type="button"
            :class="{ active: activeStatus === status }"
            @click="activeStatus = status"
          >
            {{ label }}
          </button>
        </div>

        <div class="rail-note">
          <span>Очередь</span>
          <strong>{{ filteredTickets.length }}</strong>
          <small>видимых обращений</small>
        </div>
      </aside>

      <section class="ticket-board">
        <div class="section-head">
          <div>
            <p class="eyebrow">Live queue</p>
            <h2>Обращения пользователей</h2>
          </div>
          <button type="button" class="ghost-button" @click="loadWorkspace">Обновить</button>
        </div>

        <div class="ticket-table">
          <div class="table-head">
            <span>Номер</span>
            <span>Заявка</span>
            <span>Контур</span>
            <span>Приоритет</span>
          </div>

          <div v-if="isLoading" class="empty">Загружаем обращения...</div>
          <div v-else-if="filteredTickets.length === 0" class="empty">По выбранному статусу обращений нет.</div>
          <button
            v-for="ticket in filteredTickets"
            v-else
            :key="ticket.id"
            type="button"
            class="ticket-row"
            :class="[ticket.priority.toLowerCase(), { selected: selectedTicket?.id === ticket.id }]"
            @click="selectedTicketId = ticket.id"
          >
            <span class="ticket-code">#{{ ticket.id }}</span>
            <span class="ticket-main">
              <strong>{{ ticket.title }}</strong>
              <small>{{ ticket.requester }} · до {{ formatDate(ticket.dueAt) }}</small>
            </span>
            <span class="system-cell">{{ ticket.system }}</span>
            <span class="badge" :class="ticket.priority.toLowerCase()">{{ priorityLabels[ticket.priority] }}</span>
          </button>
        </div>
      </section>

      <section class="ticket-inspector">
        <template v-if="selectedTicket">
          <div class="section-head compact">
            <div>
              <p class="eyebrow">Инспектор #{{ selectedTicket.id }}</p>
              <h2>{{ selectedTicket.title }}</h2>
            </div>
            <span class="badge neutral">{{ statusLabels[selectedTicket.status] }}</span>
          </div>

          <p class="description">{{ selectedTicket.description }}</p>

          <dl class="details">
            <div>
              <dt>Система</dt>
              <dd>{{ selectedTicket.system }}</dd>
            </div>
            <div>
              <dt>Ответственный</dt>
              <dd>{{ selectedTicket.assignee }}</dd>
            </div>
            <div>
              <dt>Создано</dt>
              <dd>{{ formatDate(selectedTicket.createdAt) }}</dd>
            </div>
            <div>
              <dt>SLA</dt>
              <dd>{{ formatDate(selectedTicket.dueAt) }}</dd>
            </div>
          </dl>

          <div class="hint">
            <span>Подсказка автоматизации</span>
            <p>{{ selectedTicket.automationHint }}</p>
          </div>

          <div class="tags">
            <span v-for="tag in selectedTicket.tags" :key="tag">#{{ tag }}</span>
          </div>

          <div class="actions">
            <button type="button" @click="setStatus(selectedTicket, 'InProgress')">В работу</button>
            <button type="button" @click="setStatus(selectedTicket, 'WaitingForUser')">Ждем ответ</button>
            <button type="button" class="primary" @click="setStatus(selectedTicket, 'Resolved')">Решено</button>
          </div>
        </template>
      </section>
    </section>

    <section id="automation" class="automation-desk">
      <form class="intake-panel" @submit.prevent="createTicket">
        <div class="section-head">
          <div>
            <p class="eyebrow">Новая заявка</p>
            <h2>Регистрация обращения</h2>
          </div>
        </div>

        <div class="form-grid">
          <label>
            Тема
            <input v-model="draft.title" type="text" placeholder="Например: не открывается договор" @input="previewClassification" />
          </label>

          <label>
            Автор
            <input v-model="draft.requester" type="text" placeholder="Имя сотрудника" />
          </label>

          <label>
            Система
            <select v-model="draft.system" @change="previewClassification">
              <option>CRM</option>
              <option>DWH</option>
              <option>Identity</option>
              <option>Billing</option>
              <option>Core Leasing</option>
            </select>
          </label>

          <label class="wide-field">
            Описание
            <textarea v-model="draft.description" rows="5" placeholder="Что произошло, у кого и как повторить" @input="previewClassification"></textarea>
          </label>
        </div>

        <p v-if="formError" class="form-error">{{ formError }}</p>
        <button type="submit" class="primary wide">Создать обращение</button>
      </form>

      <section class="automation-panel">
        <div class="section-head compact">
          <div>
            <p class="eyebrow">Автоклассификация</p>
            <h2>Рекомендация системы</h2>
          </div>
          <span class="badge" :class="classification.priority.toLowerCase()">{{ priorityLabels[classification.priority] }}</span>
        </div>

        <dl class="details">
          <div>
            <dt>Ответственный</dt>
            <dd>{{ classification.assignee }}</dd>
          </div>
          <div>
            <dt>Теги</dt>
            <dd>{{ classification.tags.join(', ') }}</dd>
          </div>
        </dl>

        <div class="hint">
          <span>Следующий шаг</span>
          <p>{{ classification.automationHint }}</p>
        </div>

        <div class="reply">
          <span>Шаблон ответа</span>
          <p>{{ classification.suggestedReply }}</p>
        </div>
      </section>
    </section>

    <section id="knowledge" class="knowledge-band">
      <div class="section-head">
        <div>
          <p class="eyebrow">База знаний</p>
          <h2>Типовые решения</h2>
        </div>
      </div>

      <div class="knowledge-list">
        <article v-for="article in articles" :key="article.id">
          <div class="knowledge-topline">
            <span>runbook / {{ article.system }}</span>
            <small>{{ article.tags.join(' · ') }}</small>
          </div>
          <div class="knowledge-title">
            <strong>{{ article.title }}</strong>
          </div>
          <p>{{ article.problem }}</p>
          <div class="knowledge-resolution">
            <span>resolution</span>
            <small>{{ article.resolution }}</small>
          </div>
        </article>
      </div>
    </section>
  </main>
</template>

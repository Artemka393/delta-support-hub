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
  <main class="shell">
    <aside class="sidebar">
      <div class="brand">
        <span class="brand-mark">SF</span>
        <div>
          <strong>SupportFlow Hub</strong>
          <span>внутренняя поддержка</span>
        </div>
      </div>

      <nav class="nav">
        <a href="#queue" class="active">Очередь</a>
        <a href="#automation">Автоматизация</a>
        <a href="#knowledge">База знаний</a>
      </nav>

      <div class="status-pill" :class="apiMode">
        <span></span>
        {{ apiMode === 'online' ? 'API подключен' : 'Демо-режим' }}
      </div>
    </aside>

    <section class="workspace">
      <header class="topbar">
        <div>
          <p class="eyebrow">Рабочее место первой линии</p>
          <h1>Контроль обращений и автоматизация поддержки</h1>
        </div>
        <div class="health">
          <span>Состояние очереди</span>
          <strong>{{ queueHealth }}</strong>
        </div>
      </header>

      <section class="metrics" aria-label="Показатели очереди">
        <article>
          <span>Новые</span>
          <strong>{{ summary.openCount }}</strong>
        </article>
        <article>
          <span>В работе</span>
          <strong>{{ summary.inProgressCount }}</strong>
        </article>
        <article>
          <span>Решено сегодня</span>
          <strong>{{ summary.resolvedTodayCount }}</strong>
        </article>
        <article class="risk">
          <span>SLA риск</span>
          <strong>{{ summary.overdueCount || slaRiskCount }}</strong>
        </article>
        <article>
          <span>Автоматизация</span>
          <strong>{{ summary.automationCoveragePercent }}%</strong>
        </article>
      </section>

      <section id="queue" class="grid">
        <div class="panel queue-panel">
          <div class="panel-head">
            <div>
              <p class="eyebrow">Очередь</p>
              <h2>Обращения пользователей</h2>
            </div>
            <button type="button" @click="loadWorkspace">Обновить</button>
          </div>

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

          <div v-if="isLoading" class="empty">Загружаем обращения...</div>
          <div v-else-if="filteredTickets.length === 0" class="empty">По выбранному статусу обращений нет.</div>
          <div v-else class="ticket-list">
            <button
              v-for="ticket in filteredTickets"
              :key="ticket.id"
              type="button"
              class="ticket-row"
              :class="{ selected: selectedTicket?.id === ticket.id }"
              @click="selectedTicketId = ticket.id"
            >
              <span class="ticket-main">
                <strong>{{ ticket.title }}</strong>
                <small>{{ ticket.requester }} · {{ ticket.system }} · до {{ formatDate(ticket.dueAt) }}</small>
              </span>
              <span class="badge" :class="ticket.priority.toLowerCase()">{{ priorityLabels[ticket.priority] }}</span>
            </button>
          </div>
        </div>

        <div class="panel detail-panel">
          <template v-if="selectedTicket">
            <div class="panel-head">
              <div>
                <p class="eyebrow">Заявка #{{ selectedTicket.id }}</p>
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
        </div>
      </section>

      <section id="automation" class="grid lower-grid">
        <form class="panel intake-panel" @submit.prevent="createTicket">
          <div class="panel-head">
            <div>
              <p class="eyebrow">Новая заявка</p>
              <h2>Регистрация обращения</h2>
            </div>
          </div>

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

          <label>
            Описание
            <textarea v-model="draft.description" rows="5" placeholder="Что произошло, у кого и как повторить" @input="previewClassification"></textarea>
          </label>

          <p v-if="formError" class="form-error">{{ formError }}</p>
          <button type="submit" class="primary wide">Создать обращение</button>
        </form>

        <div class="panel automation-panel">
          <div class="panel-head">
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
        </div>
      </section>

      <section id="knowledge" class="panel knowledge-panel">
        <div class="panel-head">
          <div>
            <p class="eyebrow">База знаний</p>
            <h2>Типовые решения</h2>
          </div>
        </div>

        <div class="knowledge-list">
          <article v-for="article in articles" :key="article.id">
            <div>
              <strong>{{ article.title }}</strong>
              <span>{{ article.system }}</span>
            </div>
            <p>{{ article.problem }}</p>
            <small>{{ article.resolution }}</small>
          </article>
        </div>
      </section>
    </section>
  </main>
</template>

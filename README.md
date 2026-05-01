# SupportFlow Hub

Портфолио-проект под вакансию Junior разработчика (.NET, Vue.js): веб-сервис для поддержки пользователей внутренних систем и автоматизации первой линии обработки обращений.

## Что показывает проект

- ASP.NET Core Minimal API на C# с EF Core и SQL Server.
- Vue 3 + TypeScript + Vite на клиенте.
- Очередь обращений с приоритетами, SLA и статусами.
- Автоматическую классификацию обращений по правилам.
- Базу знаний для типовых проблем.
- Dashboard для оценки нагрузки поддержки.

## Сценарий

Сотрудник пишет обращение по внутренней системе. Сервис определяет область проблемы, предлагает приоритет, ответственный контур, теги и шаблон ответа. Специалист поддержки видит очередь, меняет статусы и использует подсказки из базы знаний.

## Структура

```text
client/  Vue 3 + TypeScript интерфейс
server/  ASP.NET Core API + EF Core модели
docs/    краткое описание для собеседования
```

## Быстрый запуск frontend

```powershell
cd client
npm install
npm run dev
```

По умолчанию клиент обращается к `https://localhost:7180`. Для другого адреса API задайте:

```powershell
$env:VITE_API_BASE_URL="http://localhost:5080"
npm run dev
```

Если API не запущен, интерфейс работает на демонстрационных данных, чтобы можно было оценить UX.

## Запуск backend

Требуется .NET 8 SDK и SQL Server или LocalDB.

```powershell
cd server
dotnet restore
dotnet run
```

Connection string задается в `server/appsettings.Development.json`. Для Docker SQL Server можно использовать:

```powershell
docker compose up -d mssql
```

## API

- `GET /api/dashboard/summary` - агрегаты по обращениям.
- `GET /api/tickets` - список обращений.
- `POST /api/tickets` - создать обращение с классификацией.
- `PATCH /api/tickets/{id}/status` - сменить статус.
- `POST /api/automation/classify` - получить автоматическую классификацию.
- `GET /api/knowledge` - база знаний.


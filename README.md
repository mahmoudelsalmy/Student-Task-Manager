# TaskMate

TaskMate is a student productivity web application for managing academic tasks, courses, deadlines, and study time. The current application is an ASP.NET Core API with a same-origin HTML/CSS/JavaScript interface.

> **Status:** Active development on `docs/taskmate-v2-development-plan`. Pull request [#3](https://github.com/mahmoudelsalmy/Student-Task-Manager/pull/3) is open for review and must not be merged without approval.

## Current features

- Email/password registration and login with JWT authentication.
- User-scoped task management, search, filters, pagination, status and priority updates.
- Course management and optional task-course links with ownership validation.
- Task categories for assignments, exams, projects and personal work.
- Dashboard task totals and overdue/upcoming counts.
- Monthly task deadline calendar, status board and seven-day due-date study planner.
- Persisted study sessions through authenticated API endpoints, with session history and weekly/monthly summaries available from the backend.

**Known gaps:** Study sessions now have a Pomodoro focus timer, course/task association, recent-session history, and weekly/monthly summaries. The planner suggests time from task deadlines; it does not persist plans. Independent exam records, study goals, notifications, advanced task features and analytics remain on the roadmap. See [the audit and roadmap](docs/AUDIT_AND_ROADMAP.md).

## Technology

- .NET 10 / ASP.NET Core Web API
- Entity Framework Core 10 with SQLite
- JWT bearer authentication
- HTML, CSS and vanilla JavaScript; no frontend package build step
- xUnit and ASP.NET Core integration tests

## Run locally

Requirements: .NET SDK 10.0 or later.

From `app/`:

```powershell
dotnet restore TaskMate.slnx
dotnet run --project backend/TaskMate.Api/TaskMate.Api.csproj
```

Or use `scripts/run.ps1` on Windows or `scripts/run.sh` on macOS/Linux. The default launch profile listens at `http://localhost:5117`. The app applies EF migrations on startup and creates its SQLite database under `backend/TaskMate.Api/App_Data/`.

Production requires a unique JWT signing key of at least 32 bytes and an explicit `AllowedHosts` value. Configure secrets through environment variables; never commit production secrets. See [app/README.md](app/README.md) for configuration, routes, migrations, backups and Docker deployment.

## Run tests

From `app/`:

```powershell
dotnet test TaskMate.slnx --configuration Release
```

GitHub Actions restores, builds and runs the xUnit suite for changes under `app/`. Current automated coverage includes authentication, task/course ownership, task-course relationships, password hashing and validation. Study-session API coverage is being added; browser-level tests are not configured.

## Repository map

- `app/backend/TaskMate.Api/` — API, domain, contracts, services, repositories and migrations.
- `app/Frontend/` — application HTML, CSS and JavaScript.
- `app/tests/TaskMate.Api.Tests/` — unit and API integration tests.
- `app/database/schema.sql` — SQL for a fresh database at the current migration version.
- `docs/` — project scope, specification, audit and phased roadmap.

## Data and deployment

SQLite is suitable for local use or a single application instance. Docker Compose mounts a persistent volume at `/app/data` and requires a production JWT secret and public host name. Back up the SQLite database before upgrades; see the application guide for the backup and deployment procedure. No production deployment has been verified.

## License

No license has been selected for this repository yet.


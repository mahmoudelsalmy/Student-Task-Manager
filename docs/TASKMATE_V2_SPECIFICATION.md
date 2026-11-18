# TaskMate V2 — Development Specification

## Purpose

Evolve TaskMate from a student task manager into a student productivity platform while preserving the existing ASP.NET Core Web API, Entity Framework Core, SQLite, JWT authentication, and HTML/CSS/JavaScript frontend wherever possible.

This document is a proposed implementation specification. It is not evidence that these features have already been implemented.

## 1. Current repository baseline

The repository's application source must be audited before coding. Verify its actual structure, framework versions, database provider, migrations, authentication flow, API contracts, frontend build/serving model, and existing tests. Do not assume this repository is identical to any similarly named external repository.

At the time this plan was added, the repository contained project planning documentation and did not show the TaskMate application source tree. Import or create the application source only after confirming the intended source and preserving its history where practical.

## 2. Product modules

### 2.1 Task management
- CRUD, search, filters, sorting, pagination.
- Status: ToDo, InProgress, Completed.
- Priority: Low, Medium, High.
- Category and optional course association.
- Due/overdue detection.
- Subtasks and completion percentage.
- Recurrence rules (daily, weekly, monthly).
- Notes and validated file attachments.
- Kanban and calendar views.

### 2.2 Courses
- Course name, code, instructor, credit hours, description, display color, created timestamp.
- User-scoped CRUD and details.
- Link courses to tasks, exams, assignments, and study sessions.

### 2.3 Exams
- Title, course, type (Quiz, Midterm, Final), date/time, location, description, notes.
- Upcoming/past views, countdown, date/course filters, dashboard summaries.

### 2.4 Assignments
Avoid duplicating the task model without a clear reason. Prefer representing an assignment as a task with an academic subtype/details, or use a separate Assignment entity only when requirements justify different lifecycle or submission data.
- Title, course, description, deadline, status, priority, submission notes, attachments.
- Deadline tracking and progress.

### 2.5 Study planner and focus timer
- Daily/weekly planning and study goals.
- Persist completed study sessions with course/task association and actual duration.
- Pomodoro defaults: 25-minute focus, 5-minute short break, 15-minute long break.
- Configurable focus/break durations and session history.
- Validate session duration and timestamps on the server; do not trust client-reported values blindly.

### 2.6 Notifications and reminders
- In-app notification history and read/unread state.
- Reminders for tasks, exams, and assignments.
- Background processing for due reminders; do not rely solely on browser timers.
- Ensure jobs are idempotent and prevent duplicate notifications.

### 2.7 Dashboard and analytics
- Real database-derived task totals, completed, pending, in-progress, overdue.
- Upcoming exams and assignments.
- Study-time summaries and course progress.
- Weekly completions, daily study hours, priority/category distributions.
- Use Chart.js or a comparable lightweight library; never hardcode production metrics.

## 3. UI/UX

Provide a responsive, accessible interface with:
- Dashboard and consistent sidebar/navigation.
- Tasks, Kanban, Calendar, Courses, Course Details, Exams, Assignments.
- Study Planner, Focus Timer, Analytics, Notifications, Profile, Settings.
- Light/dark themes with persisted preference.
- Loading, empty, validation, success, and error states.
- Confirmation for destructive actions.
- Mobile/tablet layouts and keyboard/accessibility support.

Keep every visible control connected to a working API or clearly mark it as unavailable. Do not ship decorative controls pretending to work.

## 4. Proposed data model

Possible entities:
- User
- TaskItem
- Course
- Exam
- SubTask
- StudySession
- StudyGoal
- Notification
- Reminder
- TaskAttachment

Assignment may be represented through TaskItem fields/category or as a separate entity depending on audited business requirements.

Required relationships:
- User owns Courses, Tasks, StudySessions, Goals, Notifications, and Reminders.
- Course belongs to a User and has Tasks and Exams.
- Task has SubTasks and Attachments.
- Every relationship must enforce ownership and prevent cross-user access.

Database requirements:
- Foreign keys, sensible indexes, max lengths, unique constraints, and explicit delete behavior.
- UTC instants for timestamps; define clear semantics for date-only academic deadlines and timezone display.
- Avoid unnecessary cascade chains.
- Preserve existing data.
- Use additive EF Core migrations; never drop/recreate a database to upgrade it.
- Back up SQLite, including handling WAL/SHM safely, before important migrations.

## 5. Backend architecture and API

Maintain the existing architecture if it is sound:
- Domain entities and enums.
- Request/response DTOs (do not expose EF entities directly).
- Controllers limited to HTTP concerns.
- Services for business logic.
- Repositories/data access where useful.
- Dependency injection, async I/O, validation, structured logs.
- Central exception handling and Problem Details.

Suggested endpoints (adjust to existing route conventions):
- /api/courses
- /api/exams
- /api/assignments
- /api/tasks/{taskId}/subtasks
- /api/study/sessions
- /api/study/goals
- /api/notifications
- /api/reminders

Use correct HTTP status codes and consistent pagination/filter contracts. All protected resources must be scoped to the authenticated user, not to a client-supplied user ID.

## 6. Security and reliability

- Validate JWT issuer, audience, signature, expiry, and strong production secrets.
- Keep production secrets outside source control.
- Apply ownership checks to every read, update, delete, attachment, and related-resource lookup.
- Validate uploads by size, extension and content; generate server-side file names; store outside executable/static paths where possible.
- Use rate limiting on sensitive endpoints.
- Configure CORS narrowly.
- Avoid leaking exception details or personal data.
- Add request validation and safe logging.
- Use a background worker with idempotent reminder dispatch.
- Add health checks and production configuration guidance.

## 7. Testing

Add automated unit and integration tests for:
- Registration/login and token validation.
- Task CRUD, filtering, pagination, and validation.
- Course/exam/assignment CRUD.
- Subtasks and relationship constraints.
- Study session persistence and duration validation.
- Reminder idempotency and notification state.
- Cross-user access denial for every protected module.
- Database migrations against a fresh database and an existing populated database.

Report actual commands and results. Never claim tests/builds passed unless executed.

## 8. Delivery phases

1. **Audit and baseline:** inventory files, run existing app/tests, record current schema and API behavior, back up database.
2. **Architecture and schema:** decide Assignment representation, entities, relationships, indexes, DTOs, and migration plan.
3. **Task enhancements:** subtasks, recurrence, attachments, filters, Kanban, calendar.
4. **Courses:** complete user-scoped CRUD and task links.
5. **Exams and assignments:** deadlines, views, filters, dashboard integration.
6. **Study planner:** goals, sessions, Pomodoro, statistics.
7. **Notifications:** in-app inbox, scheduled reminders, background processing.
8. **Dashboard and UX:** live analytics, charts, responsive navigation, theme settings.
9. **Security and quality:** authorization audit, validation, automated tests, logging, documentation.
10. **Deployment:** production secrets, persistent database storage, HTTPS, backups, health checks.

At each phase:
- List changed files and explain design decisions.
- Add and review migrations; preserve data.
- Run build and relevant tests.
- Test API-to-frontend integration.
- Record remaining limitations.

## 9. Acceptance criteria

A feature is complete only when:
- It persists correctly in the database.
- It has authenticated, user-scoped API operations.
- Its UI works with the real API.
- Validation and error states are handled.
- Tests cover core behavior and ownership.
- Documentation explains setup and usage.

## 10. Non-goals for the first release

Defer AI assistant/task breakdown, smart prioritization, Google Calendar integration, team collaboration, and cloud storage until the core modules are stable and tested.

## 11. First action for an implementation agent

Do not start coding from this plan alone. First inspect this repository and establish whether it contains the intended TaskMate application source. If the application source is absent, request/import the correct source repository or archive before implementing. Never invent an existing codebase or overwrite unrelated files.

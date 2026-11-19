# TaskMate V2 audit and roadmap

**Audited:** 2026-11-19  
**Branch:** `docs/taskmate-v2-development-plan`  
**Audited HEAD:** `a8f61a83b90f3ab67999b9a64f21f061350b68b3`  
**Review:** PR #3 is open; no pull request was merged during this audit.

## Architecture and verified stack

- ASP.NET Core Web API on .NET 10, C# and EF Core 10.0.13.
- SQLite persistence, with EF Core migrations applied on application startup.
- JWT bearer authentication; PBKDF2 password hashing; ASP.NET Core controllers.
- The API uses contracts/DTOs, domain entities, services, and repositories for tasks. Courses and study sessions currently access EF Core directly from controllers.
- The frontend is a same-origin HTML/CSS/vanilla JavaScript application served by ASP.NET Core. There is no frontend package manager or build step.
- Docker multi-stage image and Compose deployment use a persistent SQLite volume.

## Feature inventory

| Area | Status | Evidence and gaps |
|---|---|---|
| Registration, login, profile | Partially implemented | Register/login and `/api/auth/me`; no profile editing, password reset, or account settings. |
| Tasks | Partially implemented | CRUD, owner scoping, title search, status/category/priority and date filters, pagination, dashboard counts. Missing course filter, client-selected sort, start/completion dates, progress, subtasks, recurrence, notes distinct from description, and attachments. |
| Courses | Partially implemented | User-scoped CRUD, code/instructor/semester/color, task link, ownership validation. Missing description, credit hours, course detail/search/filter and course analytics. |
| Exams | Partially implemented | `Exam` is a task category, so its title, course and due date can be tracked. No independent exam type/location/time/CRUD, countdown, reminders, or exam dashboard data. |
| Assignments | Partially implemented | `Assignment` is a task category. This avoids duplicating CRUD. No submission workflow, submission notes/files, grading, or independent assignment views. |
| Calendar | Partially implemented | Monthly view of task deadlines. No day/week views; no separate exam or assignment records. |
| Kanban | Partially implemented | Three task status columns with status changes persisted via the task API. Status is changed with a select control; no drag-and-drop. |
| Study planner | Partially implemented | Seven-day view derives suggestions from task due dates and category. It does not persist planned blocks or goals. |
| Study sessions | Partially implemented | Latest commit adds authenticated create/list/summary endpoints and a migration. The frontend does not call these endpoints; no Pomodoro, goals, editing/deletion, or session tests yet. |
| Notifications/reminders | Missing | No notification entities, inbox, preferences, scheduler, or background worker. |
| Dashboard/analytics | Partially implemented | Real task totals, completed, due-this-week, overdue and completion percentage. No study/exam/assignment/course analytics or charts. |
| Frontend design | Partially implemented | Responsive sign-in/dashboard, CSS variables and task/course/calendar/board/planner views exist. It is a single large `app.js`; there are no exams, focus timer, analytics, notification, settings or dark-theme screens. Browser verification has not been recorded. |

## Data model and API

Current entities are `User`, `TaskItem`, `Course`, and `StudySession`. A task has an optional `CourseId`; deleting a course sets linked task course IDs to null. Study sessions optionally reference a course and task; deleting either leaves the session and clears that reference. Ownership is carried by `UserId` and checked by protected API queries.

Current routes cover `/api/auth`, `/api/tasks`, `/api/courses`, `/api/dashboard`, `/api/study/sessions`, and `/api/study/summary`. There are no `/api/exams`, `/api/assignments`, `/api/study/goals`, `/api/notifications`, or `/api/reminders` routes.

The Study API currently accepts client-provided start/end timestamps, limits sessions to 1–240 minutes, checks future times, and validates optional course/task ownership. Timestamp handling should be tightened to require unambiguous UTC instants and must not trust client-reported duration without further safeguards. The API currently has no integration tests for study sessions.

## Security, tests, CI, and deployment

- JWT issuer, audience, signature and expiry validation are enabled. Production requires a non-development signing key and explicit `AllowedHosts`.
- Auth endpoints have per-IP rate limiting. Security response headers and a restrictive same-origin CSP are configured.
- Protected task/course/study routes are user-scoped. Study reference ownership is checked at creation.
- Gaps: no upload surface exists (so upload validation is not applicable yet); no CORS policy is configured (the current app is same-origin); no backup/restore procedure or migration backup step; no automated populated-database migration test; no request logging policy audit or deployment smoke test.
- GitHub Actions on the audited HEAD passed restore, build, and 20 tests. Existing tests cover passwords, auth, task behavior/ownership and courses/course links. They do not cover study sessions, migrations over populated data, frontend behavior or browser accessibility.
- Local test execution and direct app startup were blocked in the current Windows environment by Application Control. The hosted CI result is evidence for the current backend tests, not for browser verification.
- Docker/Compose configuration supplies a required JWT secret and allowed host and uses a persistent volume. No live deployment target or external production database is configured. SQLite is suitable for local/single-instance use only.

## Documentation and repository issues

- The root README still claims the application source has not been added and lists a planned stack that conflicts with the implementation. It needs to become a current project overview.
- `app/README.md` describes task/course features but does not document the newly added study-session API and migration.
- `app/database/schema.sql` stops at the task-course migration and omits `StudySessions`; it is not current with the audited migration set.
- `docs/TASKMATE_V2_SPECIFICATION.md` explicitly describes a proposed plan, which is useful as a specification but should not be treated as implementation evidence.

## Architecture decisions

1. Keep ASP.NET Core 10, EF Core, SQLite, JWT and the existing same-origin frontend; do not replace the working application or introduce a frontend framework without a demonstrated need.
2. Continue to represent assignments as tasks while there is no submission/grading lifecycle. Add fields only for a real assignment-specific workflow; keep exams as a separate entity because type, time, location and reminders are independent of generic task status.
3. Keep calendar and Kanban as projections over persisted tasks. Persist study sessions and goals because they have history and progress independent of task due dates.
4. Additive EF migrations only. Test both a fresh database and an existing database containing user/course/task records before treating a migration as production-safe.
5. Keep API DTO validation and ownership checks server-side. Move study business rules from the controller into a testable service as the study module grows.
6. Extend the existing visual language and CSS tokens. Build missing pages into the real app, with shared layout, accessible states, responsive behavior and real API integration; do not ship static demos.

## Prioritized implementation roadmap

1. **Audit baseline and repair documentation/schema** — align both READMEs and regenerate/update fresh-database SQL through the latest migration; add migration compatibility coverage.
2. **Task model and API** — course filter, completion/start timestamps, explicit sorting and updated dashboard counts; decide subtasks/recurrence and file storage before adding tables or uploads.
3. **Exams and academic workflow** — add user-owned Exam entity/API/UI and calendar/dashboard integration. Continue using Tasks for assignments until submission-specific requirements are implemented.
4. **Study module** — test and harden StudySession API, add frontend session history and timestamp-based Pomodoro timer, persist completed focus sessions, then add StudyGoal CRUD/progress.
5. **Notifications/reminders** — user preferences, idempotent reminder records and hosted worker with restart-safe scheduling; expose inbox/read state.
6. **Dashboard and UX** — live study/exam/course analytics, complete navigation/pages, accessible responsive controls, persisted theme preference and browser workflow checks.
7. **Security and release readiness** — data backup/restore, migration upgrade rehearsal, deployment smoke test, security review, docs and CI gates.

Each phase should be implemented in a focused commit, with tests for validation and ownership before moving to the next phase. PR #3 remains open for review; do not merge it without explicit approval.


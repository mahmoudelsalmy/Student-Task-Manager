# Student Task Manager

A web-based academic productivity application that helps university students organize subjects, assignments, projects, exams, and deadlines in one place.

> **Course project:** CSE362c / CSE5531 — Fall 2026  
> **Status:** Proposal and initial planning

## Project goals

- Create and maintain academic tasks.
- Browse, search, and filter tasks.
- Track task status, priority, and deadlines.
- Organize tasks by subject.
- Review upcoming deadlines and overall progress.

## Required user journeys

| Journey | Goal | Planned behavior |
|---|---|---|
| **J1 — Browse or discover** | Find academic tasks | Browse, search, and filter the signed-in user's tasks. |
| **J2 — Create or submit** | Add a task | Submit a task form; validate it on the server; persist the record. |
| **J3 — Review or update** | Progress a task | The task owner edits task details or changes its status. |

## Planned technology stack

- **Frontend:** HTML5, CSS3, JavaScript, Bootstrap
- **Backend:** C# and ASP.NET Core MVC
- **Data access:** Entity Framework Core
- **Database:** SQL Server
- **Authentication:** ASP.NET Core Identity (planned)

The application must be server-backed. Browser-side validation alone is not sufficient, and saved data must survive an application restart.

## Planned core features

- Authentication and per-user data access
- Subject management
- Task creation, viewing, editing, status updates, and deletion
- Search and filters by title, subject, status, priority, and due date
- Dashboard summary and upcoming deadlines
- Responsive, keyboard-accessible interface

## Initial data model

- **ApplicationUser:** account and identity information
- **Subject:** name, optional description, and owner
- **Task:** title, description, subject, task type, due date, priority, status, and owner

Every task and subject must be scoped to its owner. Authorization and ownership checks must be enforced on the server.

## Getting started

Application source code has not yet been added. Tested prerequisites, database setup, migrations, and run commands will be documented when the implementation is ready.

Never commit passwords, connection-string secrets, API keys, or real student data.

## Quality and evidence plan

The team will maintain reproducible evidence for:
- Positive and negative validation tests
- Business-rule tests
- Data persistence after restart
- Authorization and cross-user access attempts
- Browser/server HTTP requests and responses
- Accessibility, compatibility, and regression checks
- Diagnosis of at least one issue where initial evidence is insufficient to identify the root cause

## Repository workflow

- `main`: stable, reviewed work
- `develop`: integrated development
- `feature/<short-name>`: individual features
- Use focused commits that describe one logical change.
- Open pull requests into `develop`; review before merging.
- Keep meaningful commit history throughout the project.

## Team

Add the confirmed team roster and university contact details before submission.

# Project Scope

## Domain

Education and academic productivity / student support.

## Problem

Students may track assignments, projects, study activities, and exam dates across paper notes, spreadsheets, messaging applications, and separate calendar tools. This makes it harder to find task details, notice approaching deadlines, and understand progress.

## Intended users

University students who want to organize their own academic workload.

## In scope

- Sign-up and sign-in
- Per-user subject management
- Task CRUD
- Task search and filtering
- Status, priority, type, and due-date tracking
- Dashboard summary and upcoming deadlines
- Basic calendar/deadline view

## Out of scope for the first version

- Native mobile and desktop-only applications
- AI-generated study plans
- Assignment submission or grading
- University portal integrations
- Shared team workspaces

## J1 — Browse or discover

**User:** Authenticated student.

**Action:** Opens the task list, searches by title, and filters by subject, status, priority, or due date.

**Server/data behavior:** Server retrieves only the authenticated user's records and returns matching tasks.

**Visible result:** Matching task records or a clear empty state.

**Evidence:** UI state, GET request and response, and proof that records are owner-scoped.

## J2 — Create or submit

**User:** Authenticated student.

**Action:** Submits a form containing title, subject, task type, due date, priority, and optional description.

**Server/data behavior:** Server validates required fields, allowed values, due-date rules, and subject ownership before saving.

**Visible result:** Success confirmation and the new task in the task list.

**Evidence:** Form, POST request, valid and invalid submissions, stored record, and record persistence after restart.

## J3 — Review or update

**User:** Authenticated owner of the task.

**Action:** Opens a task, edits permitted details, or changes its status.

**Server/data behavior:** Server checks authentication and task ownership before updating the stored record.

**Visible result:** Updated task details/status and confirmation.

**Evidence:** Read/update requests, authorization rule, changed database values, rendered result, and a negative cross-user access test.

## Security and validation

- Validate all submitted data on the server.
- Verify referenced subjects belong to the current user.
- Enforce task ownership for reads and updates.
- Use framework-supported authentication and authorization.
- Keep secrets outside source control.
- Do not use real personal data in test fixtures.

# Contributing

Thanks for contributing to Student Task Manager.

## Branching

- Keep `main` stable.
- Integrate work through `develop`.
- Create feature branches from `develop`, for example:
  - `feature/task-crud`
  - `feature/subject-management`
  - `feature/task-filters`
  - `fix/server-validation`

## Commits

Use clear, focused messages:
- `feat: add task creation endpoint`
- `fix: enforce task ownership on update`
- `test: cover invalid due dates`
- `docs: update local setup instructions`

Avoid a single final upload. Commit meaningful increments throughout development.

## Pull requests

1. Explain the problem and change.
2. Link the related issue.
3. Include test evidence.
4. Confirm no secrets or real personal data are included.
5. Request a teammate review before merging.

## Definition of done

- Acceptance criteria are met.
- Server-side validation and authorization are applied where required.
- Relevant tests pass.
- Persistent data behavior is verified when applicable.
- README/evidence documentation is updated when needed.

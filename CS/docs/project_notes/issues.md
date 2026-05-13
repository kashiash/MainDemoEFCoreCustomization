# Issues

Chronological work log for tickets, bugs, and implementation threads.

## 2026-05-13 - ISSUE-001: Analyze XAF testing articles and adapt to MainDemo

- **Context**: Reviewed three XAF/XPO testing articles about fluent builders, test data, and functional/page-object tests.
- **Actions**: Documented how the ideas apply to this EF Core XAF repository, including EasyTest's role, ObjectSpace tests, controller tests, `EmployeeBuilder`, and `DemoTaskBuilder`.
- **Outcome**: Added `docs/xaf-testing-adaptation-plan.md` and memory decision `ADR-001`.
- **Next**: Implement `MainDemo.Module.Tests` with minimal ObjectSpace fixture, then add first builders and tests.
- **Refs**: `docs/xaf-testing-adaptation-plan.md`, `docs/project_notes/decisions.md`

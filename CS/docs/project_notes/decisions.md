# Decisions

Short ADR-style record of technical and workflow decisions.

## ADR-001: XAF testing strategy with ObjectSpace builders and page objects (2026-05-13)

**Status**: Accepted

**Context**
- Reviewed Manuel Grundner's XAF/XPO testing series about builder pattern, test data builders, and functional tests with Page Object Pattern.
- This repository already has `MainDemo.E2E.Tests` using `DevExpress.EasyTest.Framework.EasyTestFixtureContext`.
- Business objects such as `Employee`, `Department`, `DemoTask`, `Position`, and `Paycheck` contain relationships, validation rules, aliases, and controller behavior that should not all be tested through slow UI tests.

**Decision**
- Keep EasyTest as low-level E2E infrastructure for Blazor and WinForms.
- Prefer ObjectSpace-based tests with domain builders for business objects and controller behavior.
- Use page objects to hide EasyTest/UI mechanics in E2E tests.
- Start planned builder work with `EmployeeBuilder` and `DemoTaskBuilder`.

**Alternatives Considered**
- Remove EasyTest completely - rejected because current tests cover both Blazor and WinForms through DevExpress infrastructure.
- Test all behavior through E2E - rejected because it is slower, more brittle, and hides business intent.
- Create builders for every class immediately - rejected; builders should be added when tests need them.

**Consequences**
- Add a future `MainDemo.Module.Tests` project for ObjectSpace-level tests.
- Build reusable fixture helpers for creating ObjectSpaces, views, controllers, and executing actions.
- Keep E2E tests focused on critical user flows and UI visibility/enabled-state behavior.

**Refs**
- `docs/xaf-testing-adaptation-plan.md`
- `MainDemo.E2E.Tests/Tests.cs`
- `MainDemo.Module/BusinessObjects/Employee.cs`
- `MainDemo.Module/BusinessObjects/DemoTask.cs`
- `MainDemo.Module/Controllers/TaskActionsController.cs`

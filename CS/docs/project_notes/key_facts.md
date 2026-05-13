# Key Facts

Stable project facts that save repeated investigation.

## Architecture
- Solution: `MainDemo.NET.EFCore.sln`.
- Main XAF module: `MainDemo.Module`.
- UI hosts: `MainDemo.Blazor.Server` and `MainDemo.Win`.
- Existing E2E tests: `MainDemo.E2E.Tests`, using DevExpress EasyTest plus Selenium.
- Existing Web API tests: `MainDemo.WebAPI.Tests`.

## Testing Notes
- Current E2E tests use `EasyTestFixtureContext` and register both Blazor and Win applications.
- Proposed next test layer: `MainDemo.Module.Tests` for ObjectSpace-level business object and controller tests.
- First proposed domain builders: `EmployeeBuilder` and `DemoTaskBuilder`.
- Detailed plan is in `docs/xaf-testing-adaptation-plan.md`.

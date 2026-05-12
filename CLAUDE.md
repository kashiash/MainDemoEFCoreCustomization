# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this repo is

`MainDemo.NET.EFCore` is the DevExpress XAF "Main Demo" sample ported to **EF Core** (instead of XPO). It is a multi-project solution that ships the same business model through three UIs:

- `MainDemo.Blazor.Server` — ASP.NET Core Blazor Server app, hosts the **Web API** (OData v4 + Swagger) inside the same process.
- `MainDemo.Win` — WinForms client (Integrated security mode talks to `MainDemo.MiddleTier`).
- `MainDemo.MiddleTier` — ASP.NET Core middle-tier server used by the WinForms client.

Two test projects: `MainDemo.WebAPI.Tests` (xunit.v3 + `Microsoft.AspNetCore.TestHost`) and `MainDemo.E2E.Tests` (Selenium).

DevExpress version is **v25.2.6** (target framework `net9.0` / `net9.0-windows`).

## Common commands

The solution file lives under `CS\`. All build/run commands assume that directory.

```powershell
# Build everything
dotnet build CS\MainDemo.NET.EFCore.sln -c Debug

# Run Blazor + Web API (default URL http://localhost:5115)
dotnet run --project CS\MainDemo.Blazor.Server\MainDemo.Blazor.Server.csproj -c Debug --urls http://localhost:5115

# Run WinForms client (requires MiddleTier first if SecurityServer mode is used)
dotnet run --project CS\MainDemo.Win\MainDemo.Win.csproj -c Debug

# Run middle tier (for WinForms client)
dotnet run --project CS\MainDemo.MiddleTier\MainDemo.MiddleTier.csproj -c Debug

# Apply database migration on startup
dotnet run --project CS\MainDemo.Blazor.Server\MainDemo.Blazor.Server.csproj -- --updateDatabase --forceUpdate --silent

# Run Web API tests (xunit.v3)
dotnet test CS\MainDemo.WebAPI.Tests\MainDemo.WebAPI.Tests.csproj -c Debug

# Run a single test by full name
dotnet test CS\MainDemo.WebAPI.Tests\MainDemo.WebAPI.Tests.csproj -c Debug --filter "FullyQualifiedName~LocalizationTests"

# E2E tests (Selenium — needs running Blazor app on the configured URL)
dotnet test CS\MainDemo.E2E.Tests\MainDemo.E2E.Tests.csproj -c Debug
```

The `EasyTest` build configuration exists alongside `Debug`/`Release` and switches the connection string to `EasyTestConnectionString` via the `EASYTEST` define.

## Architecture — what spans multiple files

### Module composition (the XAF "spine")

- `MainDemo.Module` is the **platform-agnostic** module. It owns business objects, controllers, validation rules, reports, dashboards, and the model differences (`Model.DesignedDiffs*.xafml`). Both Blazor and WinForms consume it.
- `MainDemoModuleExtensions.AddMainDemoModule()` (CS\MainDemo.Module\MainModuleExtensions.cs:11) is the single entry point that registers the module, configures `SecurityOptions` (lockout, role/user types, `PermissionsReloadMode.CacheOnFirstAccess`) and wires up the non-persistent object space extender via `OnObjectSpaceCreated`.
- Platform-specific modules sit next to the host: `MainDemo.Blazor.Server\BlazorModule.cs` and `MainDemo.Win\WinModule.cs`.

### Persistence: EF Core, not XPO

- `MainDemoDbContext` (CS\MainDemo.Module\BusinessObjects\MainDemoDbContext.cs) is the single `DbContext`. It uses XAF EF Core extensions: `UseDeferredDeletion`, `SetOneToManyAssociationDeleteBehavior(SetNull, Cascade)`, `UseOptimisticLock`, and `ChangeTrackingStrategy.ChangingAndChangedNotificationsWithOriginalValues`.
- A second context, `AuditingDbContext`, owns only audit tables (`AuditDataItemPersistent`, `AuditEFCoreWeakReference`). It is wired through `AddSecuredEFCore().WithAuditedDbContext(...)` in `Startup.ConfigureServices` (CS\MainDemo.Blazor.Server\Startup.cs:108).
- `MainDemoDesignTimeDbContextFactory` exists purely so `dotnet ef` migrations can build the model at design time without a connection string.
- When SQL Server is not reachable the app falls back to `UseInMemoryDatabase()` — see `DemoDbEngineDetectorHelper.IsSqlServerAccessible()`.

### Web API surface (Blazor host)

- The Web API is hosted in-process with Blazor. `builder.AddXafWebApi(...)` in `Startup.ConfigureServices` enumerates each exposed business object explicitly (`Department`, `Employee`, `Paycheck`, `DemoTask`, `CustomNonPersistentObject`, …) and enables `ConfigureBusinessObjectActionEndpoints`.
- OData is mounted at `api/odata` (OData V4.01) with `EnableQueryFeatures(100)`. In `DEBUG` it also registers `DefaultODataBatchHandler` for batch edit.
- JWT auth is configured against `Authentication:Jwt` in `appsettings.json` — issuer/audience/`IssuerSigningKey` are dev secrets and must be moved to secret storage in production.
- Swagger lives at `/swagger` (Development env only) using `Swashbuckle.AspNetCore` **6.9.0** (centrally pinned — see below).
- `JsonSerializerOptions.PropertyNamingPolicy = null` is intentional: it preserves CLR casing so Swagger example payloads remain valid for case-sensitive ORMs.

### Non-persistent business objects

- `MainDemo.Module/BusinessObjects/NonPersistent/` defines `CustomNonPersistentObject`, a singleton storage (`NonPersistentGlobalObjectStorage`), and `NonPersistentObjectSpaceExtender` which is attached to every newly created `NonPersistentObjectSpace`. The XAF Web API exposes `CustomNonPersistentObject` alongside persistent objects.

### Security

- Integrated security mode in Blazor + WinForms-against-MiddleTier deployments.
- Standard password auth (`AddPasswordAuthentication`) with `IsSupportChangePassword = true` and `CustomAuthenticationProvider` for additional logins.
- Default authorization policy requires both `RequireAuthenticatedUser()` AND `RequireXafAuthentication()` on the JWT scheme.

### Localization (pl-PL, en-US, de-DE)

- Languages list: `appsettings.json` → `DevExpress:ExpressApp:Languages` = `"pl-PL;en-US;de-DE"`.
- ASP.NET Core `RequestLocalizationOptions` resolves culture in this order: query string → cookie → `Accept-Language` → fallback `en-US`. **Fallback is intentionally `en-US`** — switching the default to Polish breaks CSV report tests (separator becomes `;` and date formatting changes). Polish still works through the providers above and the XAF language switcher.
- XAF model translations:
  - `MainDemo.Module/Model.DesignedDiffs.Localization.de.xafml` — German captions
  - `MainDemo.Module/Model.DesignedDiffs.Localization.pl.xafml` — Polish captions
  - Both are registered as `EmbeddedResource` `DependentUpon="Model.DesignedDiffs.xafml"` in the module `.csproj`.
- DevExpress JS localization (Reports/Dashboards widgets) is bootstrapped from `wwwroot\js\scripts.js`, **not** from `_Host.cshtml`. `resolveLocalizationCulture()` normalizes the current culture to `de-DE` or `pl-PL` and then loads the matching `dx-analytics-core.<culture>.json`, `dx-reporting.<culture>.json`, and `<culture>.json` from `wwwroot\js\localization\`. To add a new language, drop the matching DevExpress localization JSON files there (downloadable from `https://localization.devexpress.com/`) and extend `resolveLocalizationCulture`.
- `ReportTests.cs` pins requests to `Accept-Language: en-US` and formats expected dates with `CultureInfo.GetCultureInfo("en-US")` — keep that pattern when adding report tests.

Full write-up of the Polish-localization changes lives in `docs/obsluga-jezyka-polskiego-w-main-demo-blazor.md`.

### Central package management — local override required

- Root `Directory.Packages.props` enables `ManagePackageVersionsCentrally` and defines per-TFM `PackageVersion` items.
- The trailing `<PackageVersion Update="..." />` block at the bottom (lines ~294–319) is a **deliberate downgrade/pin layer** added so the repo builds standalone, without inheriting versions from `C:\Users\Programista\source\repos\Directory.Packages.props`. Notably:
  - `Swashbuckle.AspNetCore` and `Swashbuckle.AspNetCore.Annotations` are pinned to **6.9.0** for compatibility with the DevExpress Web API's `OpenApiSecurityScheme` configuration in `Startup.cs`.
  - `System.IdentityModel.Tokens.Jwt` pinned to `8.18.0`, `SkiaSharp` to `3.119.2`.
- Do **not** remove that block when bumping packages — verify Swagger/JWT compat first.

### Application entry points (all three hosts share a pattern)

Each `Program.cs` (`MainDemo.Blazor.Server`, `MainDemo.MiddleTier`, `MainDemo.Win`) parses `--help`, `--updateDatabase [--forceUpdate] [--silent]`, sets `FrameworkSettings.DefaultSettingsCompatibilityMode = Latest` and `SecurityStrategy.AutoAssociationReferencePropertyMode = AllMembers` before building/running the host. The `--updateDatabase` path resolves `IDBUpdater` from DI and exits with codes 0/1/2.

## XAF skills — use them

This Claude Code install ships an `xaf` master skill plus topic sub-skills (`xaf-blazor-ui`, `xaf-controllers`, `xaf-ef-models`, `xaf-security`, `xaf-validation`, `xaf-reports`, `xaf-web-api`, `xaf-conditional-appearance`, `xaf-multi-tenant`, `xaf-memory-leaks`, etc.). They contain version-specific guidance for XAF v24.2 / v25.1 on both Blazor and WinForms, EF Core and XPO.

**When working in this repo, invoke the relevant XAF skill first** instead of relying on generic knowledge — this project is XAF + EF Core + Blazor, and the skills cover the exact patterns used here (`AddXaf` builder, `IObjectSpace`, `ViewController`, `ObjectViewController<TView,TObject>`, `BlazorPropertyEditorBase`, `IXafEntityObject`, `PredefinedReportsUpdater`, `RuleSet`, JWT Web API, …). Start with the master `xaf` skill to pick the right sub-skill, or jump directly when the topic is obvious (e.g. `xaf-ef-models` for `MainDemoDbContext` work, `xaf-web-api` for the OData surface, `xaf-reports` for `ReportDataV2`/`PredefinedReportsUpdater` work).

## DevExpress Documentation MCP server

DevExpress publishes an official remote MCP server for its documentation (over 300k topics, v24.2 and v25.1). It exposes two tools — `devexpress_docs_search` (semantic search, top 5 hits) and `devexpress_docs_get_content` (download a specific topic by URL) — plus a `dxdocs.devexpress_docs_query_workflow` prompt. Source: <https://community.devexpress.com/Blogs/news/archive/2025/10/16/transform-your-development-experience-with-the-devexpress-mcp-server.aspx>.

Endpoints:

- `https://api.devexpress.com/mcp/docs` — current docs
- `https://api.devexpress.com/mcp/docs?v=24.2` — pinned to v24.2
- `https://api.devexpress.com/mcp/docs?v=25.1` — pinned to v25.1 (this repo runs on v25.2.6; v25.1 is the closest version-pinned endpoint)

To add it to Claude Code:

```powershell
claude mcp add --transport http dxdocs https://api.devexpress.com/mcp/docs
```

Use it for live DevExpress API lookups (XAF, XtraReports, DevExtreme, DevExpress Blazor, Office File API, Dashboards) when Context7 doesn't cover the topic. For general libraries (EF Core, ASP.NET Core, Microsoft.Identity.Web, etc.) keep using Context7.

## Conventions worth knowing

- Source files live under `CS/<Project>/` — there is no `src/`.
- `EnableDefaultItems` is `false` in every project; new `.cs` files must be added explicitly to the `.csproj` `<Compile Include="..." />` list. The Module project also wildcard-includes `BusinessObjects\**`, `Controllers\**`, `Reports\**`, etc., so dropping files inside those folders works automatically.
- `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` is set on `MainDemo.Blazor.Server.csproj`.
- Connection string key is `ConnectionStrings:ConnectionString` (not the EF Core default `DefaultConnection`). The default points to `(localdb)\mssqllocaldb` / `MainDemo.EFCore_v25.2`.
- Polish/German `.xafml` files MUST be registered as `EmbeddedResource` with `DependentUpon="Model.DesignedDiffs.xafml"` — otherwise XAF won't load them.

# XAF testing articles - translation and adaptation plan

## Sources

- https://blog.delegate.at/2019/05/17/t-is-for-testing-xaf-xpo-builder-pattern-1.html
- https://blog.delegate.at/2019/05/26/t-is-for-testing-xaf-xpo-test-data-2.html
- https://blog.delegate.at/2019/08/08/t-is-for-testing-xaf-xpo-functional-tests-3.html

The third link originally provided as `test-data-3.html` does not exist. The matching third article in the series is `functional-tests-3.html`.

## What the articles say

### 1. Builder Pattern

The first article shows how to use a fluent builder pattern for XAF tests.

The main problem is that XAF objects and applications often need a lot of setup. If this setup is hidden in separate helper classes or "directors", the test becomes hard to read. The test no longer says clearly which input data matters.

The proposed solution is to use fluent builders:

```csharp
var application = new TestApplicationBuilder()
    .WithConnectionString("Some Test Connection String")
    .WithTitle("Test Application")
    .Build();
```

The important point is not the builder itself. The important point is that test data is visible directly in the test.

The article also uses recursive generics so that builders return the concrete application type. This avoids casting in tests when testing `WinApplication`, `XafApplication`, or custom derived application types.

### 2. Test Data

The second article applies the same idea to persistent objects.

Instead of manually creating a large object graph in every test, the article proposes domain-specific test data builders, for example:

```csharp
var person = new JohnDoePersonBuilder()
    .WithObjectSpace(os)
    .WithFirstName("Jane")
    .Build();
```

The builder provides sensible defaults, while the test overrides only what matters for the scenario.

The article also introduces guards. For XPO objects, the builder requires a `Session`; for our EF Core XAF objects, the equivalent dependency is usually `IObjectSpace`. If the builder cannot create a valid object without an `ObjectSpace`, it should fail with a clear error.

The second important idea is using a fixture for expensive test infrastructure. A fixture can create an ObjectSpace provider once and reset data between tests. This gives repeatable tests without repeating infrastructure setup everywhere.

### 3. Functional Tests

The third article discusses functional tests and Page Object Pattern.

Functional tests should simulate user behavior and assert visible behavior. The article argues against recorded UI tests and against tests full of technical UI details.

Instead of writing tests directly against controls, selectors, actions, or EasyTest commands, the test should use page objects:

```csharp
new ApplicationPageObject(fixture)
    .NavigateToContact()
    .OpenRecordByFullName("Mary Tellitson")
    .Assert(c => c.Position.ShouldBe("Manager"));
```

The page object hides UI details. The test describes what the user does.

## Does this replace EasyTest?

Not completely.

The articles do not prove that EasyTest as an execution engine must be removed. They show how to replace classic EasyTest scripts, recorded tests, or low-level EasyTest command code with normal C# tests and page objects.

In this repository, `MainDemo.E2E.Tests` already uses this direction:

```csharp
EasyTestFixtureContext FixtureContext { get; } = new EasyTestFixtureContext();
```

Tests are written in C# and use DevExpress EasyTest infrastructure underneath.

So the realistic interpretation is:

- keep EasyTest as the low-level runner for XAF Blazor and WinForms;
- avoid `.ets` scripts and recorded tests;
- wrap repeated `appContext.GetAction(...)`, `GetGrid()`, `Navigate(...)`, and form operations in page objects;
- move business logic tests away from UI tests into ObjectSpace or WebAPI tests.

EasyTest should become infrastructure, not the language of our tests.

## Current project shape

This repository is an XAF EF Core demo application with these main projects:

- `MainDemo.Module` - shared module, business objects, controllers, updater, reports;
- `MainDemo.Blazor.Server` - Blazor XAF host;
- `MainDemo.Win` - WinForms XAF host;
- `MainDemo.MiddleTier` - middle-tier host;
- `MainDemo.WebAPI.Tests` - Web API tests;
- `MainDemo.E2E.Tests` - EasyTest/Selenium-based functional tests.

Important business objects found in `MainDemo.Module/BusinessObjects`:

- `Employee`
- `Department`
- `DemoTask`
- `Position`
- `Paycheck`
- `Address`
- `PhoneNumber`
- `Resume`
- `PortfolioFileData`
- `Note`
- `Location`
- `Country`
- `ApplicationUser`
- `ApplicationUserLoginInfo`
- non-persistent objects under `BusinessObjects/NonPersistent`

The central domain graph is:

- `Department` has `Employees`, `Positions`, child `Departments`, and `DepartmentHead`;
- `Employee` has `Department`, `Position`, `Manager`, `Tasks`, `PhoneNumbers`, `Address1`, `Address2`, `Resumes`, and `Paychecks`;
- `DemoTask` has `AssignedTo`, `Employees`, `Status`, `Priority`, work hours, and actions like `Postpone` and `MarkCompleted`;
- `Paycheck` has required `Employee`, pay period fields, rates, hours, and calculated aliases: `TotalTax`, `GrossPay`, `NetPay`.

These relationships make the project a good candidate for domain-specific test builders.

## How to test normal business objects

There are two levels.

### Simple unit tests

Use this only for pure C# behavior that does not need XAF lifecycle, ObjectSpace, EF Core tracking, validation, security, or persistent aliases.

Example:

```csharp
[Fact]
public void Postpone_moves_due_date_by_one_day() {
    var task = new DemoTask {
        DueDate = new DateTime(2026, 5, 13)
    };

    task.Postpone();

    Assert.Equal(new DateTime(2026, 5, 14), task.DueDate);
}
```

This is fast, but it is not enough for XAF-specific behavior.

### ObjectSpace tests

For most XAF business objects, create objects through `IObjectSpace`:

```csharp
var employee = objectSpace.CreateObject<Employee>();
employee.FirstName = "John";
employee.LastName = "Smith";
objectSpace.CommitChanges();
```

This is the safer default because it exercises:

- XAF object lifecycle;
- EF Core integration;
- `BaseObject` behavior;
- validation rules;
- relationships;
- persistent aliases;
- ObjectSpace commit behavior.

## Proposed test architecture for this repository

### 1. Add module-level test project

Create a dedicated test project:

```text
MainDemo.Module.Tests
```

Its purpose should be testing business objects and module controllers without launching Blazor or WinForms UI.

This project should reference:

- `MainDemo.Module`;
- DevExpress XAF test/runtime packages already used by the solution;
- xUnit, matching existing test style.

### 2. Add ObjectSpace fixture

Create a reusable fixture for ObjectSpace-based tests.

Responsibilities:

- create test service provider and XAF ObjectSpace infrastructure;
- use isolated test database or in-memory SQLite where possible;
- reset data between tests;
- expose helper methods:

```csharp
IObjectSpace CreateObjectSpace();
T Create<T>() where T : class;
void Commit();
```

The fixture should be the equivalent of the article's `TimeEntryFixture`, adapted to EF Core/XAF instead of XPO `Session`.

### 3. Add domain builders

Start small. Do not generate builders for every class immediately.

Recommended first builders:

- `EmployeeBuilder`
- `DepartmentBuilder`
- `PositionBuilder`
- `DemoTaskBuilder`
- `PaycheckBuilder`

Example style:

```csharp
var department = new DepartmentBuilder(os)
    .WithTitle("Development Department")
    .WithPosition("Manager")
    .WithEmployee(new EmployeeBuilder(os).AsManager())
    .Build();
```

Rules for builders:

- they should use `objectSpace.CreateObject<T>()`;
- they should provide valid defaults;
- they should expose only scenario-relevant methods;
- they should not create unnecessary object graphs;
- they should keep test data visible in the test;
- they should represent domain meaning, not only technical fields.

#### Proposed `EmployeeBuilder`

`Employee` is a good first builder because it is central in the model and appears in many relationships:

- `Department`
- `Position`
- `Manager`
- `Tasks`
- `PhoneNumbers`
- `Address1` / `Address2`
- `Resumes`
- `Paychecks`

The builder should use `IObjectSpace` and create the employee through XAF:

```csharp
using DevExpress.ExpressApp;
using MainDemo.Module.BusinessObjects;

namespace MainDemo.Module.Tests.Builders;

public sealed class EmployeeBuilder {
    private readonly IObjectSpace objectSpace;
    private string firstName = "John";
    private string lastName = "Smith";
    private string middleName = "";
    private string email = "john.smith@example.test";
    private TitleOfCourtesy titleOfCourtesy = TitleOfCourtesy.Mr;
    private Department department;
    private Position position;
    private Employee manager;
    private readonly List<(string Number, string Type)> phoneNumbers = new();

    public EmployeeBuilder(IObjectSpace objectSpace) {
        this.objectSpace = objectSpace ?? throw new ArgumentNullException(nameof(objectSpace));
    }

    public EmployeeBuilder WithName(string firstName, string lastName, string middleName = "") {
        this.firstName = firstName;
        this.lastName = lastName;
        this.middleName = middleName;
        return this;
    }

    public EmployeeBuilder WithEmail(string email) {
        this.email = email;
        return this;
    }

    public EmployeeBuilder WithTitle(TitleOfCourtesy titleOfCourtesy) {
        this.titleOfCourtesy = titleOfCourtesy;
        return this;
    }

    public EmployeeBuilder InDepartment(Department department) {
        this.department = department;
        return this;
    }

    public EmployeeBuilder WithPosition(Position position) {
        this.position = position;
        return this;
    }

    public EmployeeBuilder WithManager(Employee manager) {
        this.manager = manager;
        return this;
    }

    public EmployeeBuilder WithPhone(string number, string type = "Work") {
        phoneNumbers.Add((number, type));
        return this;
    }

    public EmployeeBuilder AsManager() {
        titleOfCourtesy = TitleOfCourtesy.Mr;
        return this;
    }

    public Employee Build() {
        var employee = objectSpace.CreateObject<Employee>();
        employee.FirstName = firstName;
        employee.MiddleName = middleName;
        employee.LastName = lastName;
        employee.Email = email;
        employee.TitleOfCourtesy = titleOfCourtesy;

        if(department != null) {
            employee.Department = department;
            if(!department.Employees.Contains(employee)) {
                department.Employees.Add(employee);
            }
        }

        if(position != null) {
            employee.Position = position;
            if(!position.Employees.Contains(employee)) {
                position.Employees.Add(employee);
            }
        }

        if(manager != null) {
            employee.Manager = manager;
        }

        foreach(var phone in phoneNumbers) {
            var phoneNumber = objectSpace.CreateObject<PhoneNumber>();
            phoneNumber.Number = phone.Number;
            phoneNumber.PhoneType = phone.Type;
            phoneNumber.Employee = employee;
            employee.PhoneNumbers.Add(phoneNumber);
        }

        return employee;
    }
}
```

Example usage for a simple employee:

```csharp
var employee = new EmployeeBuilder(os)
    .WithName("Mary", "Tellitson")
    .WithEmail("mary.tellitson@example.test")
    .Build();
```

Example usage with department and position:

```csharp
var department = os.CreateObject<Department>();
department.Title = "Development Department";

var position = os.CreateObject<Position>();
position.Title = "Manager";
department.Positions.Add(position);

var employee = new EmployeeBuilder(os)
    .WithName("Mary", "Tellitson")
    .InDepartment(department)
    .WithPosition(position)
    .AsManager()
    .Build();

department.DepartmentHead = employee;
```

Example test for `FullName`:

```csharp
[Fact]
public void Employee_full_name_is_calculated_from_name_parts() {
    using var os = fixture.CreateObjectSpace();

    var employee = new EmployeeBuilder(os)
        .WithName("John", "Smith", "A.")
        .Build();

    os.CommitChanges();

    Assert.Equal("John A. Smith", employee.FullName);
}
```

Example test for `Department` setter behavior:

```csharp
[Fact]
public void Changing_department_clears_position_when_position_belongs_to_previous_department() {
    using var os = fixture.CreateObjectSpace();

    var oldDepartment = os.CreateObject<Department>();
    oldDepartment.Title = "Development";

    var newDepartment = os.CreateObject<Department>();
    newDepartment.Title = "Sales";

    var developer = os.CreateObject<Position>();
    developer.Title = "Developer";
    oldDepartment.Positions.Add(developer);

    var employee = new EmployeeBuilder(os)
        .InDepartment(oldDepartment)
        .WithPosition(developer)
        .Build();

    employee.Department = newDepartment;

    Assert.Null(employee.Position);
}
```

This builder should stay focused on `Employee`. It should not silently create a complete department graph, tasks, paychecks, resumes, and security users unless a specific test asks for them. Those scenarios should be modeled by separate builders or explicit method calls.

#### Proposed `DemoTaskBuilder` for testing task actions

`DemoTask` is the best second builder because it has real behavior and is used by `TaskActionsController`.

Relevant behavior:

- `Postpone()` moves `DueDate`;
- `MarkCompleted()` sets `Status = Completed`;
- setting `PercentCompleted` changes `Status`;
- setting `Status` may change `PercentCompleted` and `DateCompleted`;
- `TaskActionsController` has a `Set Task` action that changes `Priority` or `Status` for selected tasks.

The builder should create valid tasks and optionally attach an employee when a status requires an assignee.

```csharp
using DevExpress.ExpressApp;
using MainDemo.Module.BusinessObjects;

namespace MainDemo.Module.Tests.Builders;

public sealed class DemoTaskBuilder {
    private readonly IObjectSpace objectSpace;
    private string subject = "Test task";
    private string description = "Task created by test builder";
    private DateTime? startDate = new DateTime(2026, 5, 13);
    private DateTime? dueDate = new DateTime(2026, 5, 14);
    private Priority priority = Priority.Normal;
    private MainDemo.Module.BusinessObjects.TaskStatus status = MainDemo.Module.BusinessObjects.TaskStatus.NotStarted;
    private int estimatedWorkHours = 1;
    private int actualWorkHours = 0;
    private Employee assignedTo;
    private readonly List<Employee> employees = new();

    public DemoTaskBuilder(IObjectSpace objectSpace) {
        this.objectSpace = objectSpace ?? throw new ArgumentNullException(nameof(objectSpace));
    }

    public DemoTaskBuilder WithSubject(string subject) {
        this.subject = subject;
        return this;
    }

    public DemoTaskBuilder WithDescription(string description) {
        this.description = description;
        return this;
    }

    public DemoTaskBuilder WithDates(DateTime? startDate, DateTime? dueDate) {
        this.startDate = startDate;
        this.dueDate = dueDate;
        return this;
    }

    public DemoTaskBuilder WithPriority(Priority priority) {
        this.priority = priority;
        return this;
    }

    public DemoTaskBuilder WithStatus(MainDemo.Module.BusinessObjects.TaskStatus status) {
        this.status = status;
        return this;
    }

    public DemoTaskBuilder AssignedTo(Employee employee) {
        assignedTo = employee;
        return this;
    }

    public DemoTaskBuilder WithEmployee(Employee employee) {
        employees.Add(employee);
        return this;
    }

    public DemoTaskBuilder WithWorkHours(int estimated, int actual = 0) {
        estimatedWorkHours = estimated;
        actualWorkHours = actual;
        return this;
    }

    public DemoTaskBuilder InProgress(Employee assignee) {
        assignedTo = assignee;
        status = MainDemo.Module.BusinessObjects.TaskStatus.InProgress;
        return this;
    }

    public DemoTaskBuilder Completed(Employee assignee) {
        assignedTo = assignee;
        status = MainDemo.Module.BusinessObjects.TaskStatus.Completed;
        return this;
    }

    public DemoTask Build() {
        var task = objectSpace.CreateObject<DemoTask>();
        task.Subject = subject;
        task.Description = description;
        task.StartDate = startDate;
        task.DueDate = dueDate;
        task.Priority = priority;
        task.EstimatedWorkHours = estimatedWorkHours;
        task.ActualWorkHours = actualWorkHours;
        task.AssignedTo = assignedTo;

        foreach(var employee in employees) {
            task.Employees.Add(employee);
        }

        task.Status = status;
        return task;
    }
}
```

Example object-level test:

```csharp
[Fact]
public void Mark_completed_sets_task_status_to_completed() {
    using var os = fixture.CreateObjectSpace();

    var employee = new EmployeeBuilder(os)
        .WithName("Mary", "Tellitson")
        .Build();

    var task = new DemoTaskBuilder(os)
        .WithSubject("Review reports")
        .InProgress(employee)
        .Build();

    task.MarkCompleted();

    Assert.Equal(MainDemo.Module.BusinessObjects.TaskStatus.Completed, task.Status);
    Assert.Equal(100, task.PercentCompleted);
    Assert.NotNull(task.DateCompleted);
}
```

Example controller-level test idea for `TaskActionsController`:

```csharp
[Fact]
public void Set_task_action_changes_priority_for_selected_tasks() {
    using var os = fixture.CreateObjectSpace();

    var task1 = new DemoTaskBuilder(os)
        .WithSubject("Task 1")
        .WithPriority(Priority.Normal)
        .Build();

    var task2 = new DemoTaskBuilder(os)
        .WithSubject("Task 2")
        .WithPriority(Priority.Normal)
        .Build();

    os.CommitChanges();

    var listView = fixture.CreateListView<DemoTask>(os, task1, task2);
    var controller = fixture.ActivateController<TaskActionsController>(listView);

    fixture.SelectObjects(listView, task1, task2);
    fixture.ExecuteSingleChoiceAction(
        controller,
        actionId: "SetTaskAction",
        itemPath: "Priority.High");

    Assert.Equal(Priority.High, task1.Priority);
    Assert.Equal(Priority.High, task2.Priority);
}
```

The exact helper names above are proposed API for the future test fixture, not existing code yet. The point is that a controller test should not know how to construct `SingleChoiceActionExecuteEventArgs` or wire `View.SelectionChanged`; this belongs in reusable test infrastructure.

For `TaskActionsController`, useful test scenarios are:

- selected tasks can be changed to `Priority.High`;
- selected tasks can be changed to `Status.InProgress`;
- action is disabled when security denies write access to `Priority` or `Status`;
- list view execution commits changes and refreshes the original ObjectSpace;
- detail view in view mode commits changes;
- new unsaved tasks are handled without creating a second ObjectSpace copy.

### 4. Move business behavior tests below UI

Use ObjectSpace tests for:

- `DemoTask.Status` and `PercentCompleted` behavior;
- `DemoTask.MarkCompleted`;
- `DemoTask.Postpone`;
- validation rule requiring an assignee for active/completed tasks;
- `Department` validation requiring employees and positions;
- `Paycheck` validation and calculated values;
- `Employee.Department` setter resetting invalid `Position` and `Manager`;
- `Employee.FullName` alias behavior.

This reduces the need to test all business rules through slow UI tests.

### 5. Wrap E2E tests in page objects

Keep `MainDemo.E2E.Tests` and EasyTest, but reduce direct low-level calls.

Current style:

```csharp
appContext.Navigate("Tasks");
appContext.GetGrid().ProcessRow(("Subject", "Task1"));
appContext.GetAction("Set Task").Execute("Status.In progress");
```

Target style:

```csharp
new ApplicationPage(appContext)
    .LoginAs("Sam")
    .Tasks()
    .Open("Task1")
    .SetStatus("In progress")
    .AssertStatus("In progress");
```

Possible page objects:

- `ApplicationPage`
- `LoginPage`
- `TasksListPage`
- `TaskDetailPage`
- `EmployeesListPage`
- `EmployeeDetailPage`
- `RolesListPage`
- `RoleDetailPage`

Page objects should hide EasyTest details, action captions, grid operations, and platform differences between Blazor and WinForms.

## Recommended implementation order

1. Add `MainDemo.Module.Tests`.
2. Build the smallest possible ObjectSpace test fixture.
3. Add `DemoTaskBuilder` and `EmployeeBuilder`.
4. Write first tests for `DemoTask.Postpone`, `MarkCompleted`, status/percent behavior, and assignee validation.
5. Add `DepartmentBuilder` and `PositionBuilder`.
6. Add tests for `Department` and `Employee.Department` behavior.
7. Add `PaycheckBuilder`.
8. Add tests for paycheck validation and calculated aliases.
9. Refactor one existing E2E test into page object style.
10. Continue only if the pattern improves readability.

## What not to do

- Do not replace every test with UI/E2E tests.
- Do not remove EasyTest immediately.
- Do not write builders for every class before there is a test that needs them.
- Do not hide important scenario data in large fixture seed methods.
- Do not make page objects mirror every XAF control one-to-one.
- Do not test DevExpress framework behavior unless our code depends on a specific integration detail.

## Practical conclusion

The most useful adaptation is not "replace EasyTest".

The useful adaptation is:

- test business rules with ObjectSpace and domain builders;
- test API contracts with WebAPI tests;
- test only critical UI flows with EasyTest;
- hide UI mechanics behind page objects;
- keep test setup readable and domain-oriented.

For this repository, the first valuable target is a new `MainDemo.Module.Tests` project with builders for `Employee`, `Department`, `Position`, `DemoTask`, and `Paycheck`.

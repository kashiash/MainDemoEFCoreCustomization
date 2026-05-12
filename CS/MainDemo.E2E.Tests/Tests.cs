using System.Diagnostics;
using DevExpress.EasyTest.Framework;
using OpenQA.Selenium;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

// To run functional tests for ASP.NET Web Forms and ASP.NET Core Blazor XAF Applications,
// install browser drivers: https://www.selenium.dev/documentation/getting_started/installing_browser_drivers/.
//
// -For Google Chrome: download "chromedriver.exe" from https://chromedriver.chromium.org/downloads.
// -For Microsoft Edge: download "msedgedriver.exe" from https://developer.microsoft.com/en-us/microsoft-edge/tools/webdriver/.
//
// Selenium requires a path to the downloaded driver. Add a folder with the driver to the system's PATH variable.
//
// Refer to the following article for more information: https://docs.devexpress.com/eXpressAppFramework/403852/

namespace MainDemo.E2E.Tests;
public class MainDemoTests : IDisposable {
    const string BlazorAppName = "MainDemoBlazor";
    const string WinAppName = "MainDemoWin";
    const string MainDemoDBName = "MainDemoDemo";
    EasyTestFixtureContext FixtureContext { get; } = new EasyTestFixtureContext();

    public MainDemoTests() {
        FixtureContext.RegisterApplications(
            new BlazorApplicationOptions(BlazorAppName, @$"{Environment.CurrentDirectory}\..\..\..\..\MainDemo.Blazor.Server", browser: "Chrome"),
            new WinApplicationOptions(WinAppName, @$"{Environment.CurrentDirectory}\..\..\..\..\MainDemo.Win\bin\EasyTest\MainDemo.Win.exe")
        );
        FixtureContext.RegisterDatabases(new DatabaseOptions(MainDemoDBName, "MainDemo.EFCore_v25.2", server: "(localdb)\\mssqllocaldb"));
    }
    public void Dispose() {
        FixtureContext.CloseRunningApplications();
        // Temporary fix for VS2022 17.14 because it doesn't execute Application.ApplicationExited event after process.CloseMainWindow()
        // and so we cannot stop the MiddleTier server after the test. The problem is reproduced only with xUnit.v3 and Microsoft.Testing.Platform.
        Process existProc = Process.GetProcessesByName("MainDemo.MiddleTier").FirstOrDefault();
        if(existProc != null) {
            existProc.Kill();
            existProc.WaitForExit();
        }
    }

    [Theory]
    [InlineData(BlazorAppName)]
    [InlineData(WinAppName)]
    public void CreateNewRole(string applicationName) {
        FixtureContext.DropDB(MainDemoDBName);
        var appContext = FixtureContext.CreateApplicationContext(applicationName);
        appContext.RunApplication();
        appContext.GetForm().FillForm(("User Name", "Sam"));
        appContext.GetAction("Log In").Execute();
        appContext.Navigate("Roles");

        appContext.GetAction("New").Execute();
        appContext.GetAction("Save").Execute();

        Assert.Equal("\"Name\" must not be empty.", appContext.GetValidation().GetValidationMessages().First());

        if(appContext.IsWin()) {
            appContext.GetAction("Close").Execute();
        }
        appContext.GetForm().FillForm(("Name", "TechWriter"));
        appContext.GetAction("Save and Close").Execute();

        Assert.True(appContext.GetGrid().RowExists(("Name", "TechWriter")));
    }

    [Fact]
    public void GetAboutInfoText_WebDriver_Blazor() {
        FixtureContext.DropDB(MainDemoDBName);
        var appContext = FixtureContext.CreateApplicationContext(BlazorAppName);
        appContext.RunApplication();
        appContext.GetForm().FillForm(("User Name", "Sam"));
        appContext.GetAction("Log In").Execute();
        appContext.Navigate("Employees");
        var aboutInfoElement = appContext.AsBlazor().WebDriver.FindElement(By.CssSelector(".about-info"));
        Assert.Contains("Developer Express Inc. All Rights Reserved", aboutInfoElement.Text);
    }

    [Theory]
    [InlineData(BlazorAppName)]
    [InlineData(WinAppName)]
    public void CheckSaveActionState(string applicationName) {
        FixtureContext.DropDB(MainDemoDBName);
        var appContext = FixtureContext.CreateApplicationContext(applicationName);
        appContext.RunApplication();
        appContext.GetForm().FillForm(("User Name", "Sam"));
        appContext.GetAction("Log In").Execute();
        appContext.Navigate("Tasks");
        appContext.GetGrid().ProcessRow(("Subject", "Task1"));

        Assert.False(appContext.GetAction("Save").Enabled);
        var expectedTooltip = appContext.IsBlazor() ? "Save (Ctrl+Shift+S)" : "Save";
        Assert.Equal(expectedTooltip, appContext.GetAction("Save").Hint);
        Assert.False(appContext.GetAction("Save").Execute());

        appContext.GetForm().FillForm(("Estimated Work Hours", "20"), ("Actual Work Hours", "20"));

        Assert.True(appContext.GetAction("Save").Enabled);
        Assert.True(appContext.GetAction("Save").Execute());
    }

    [Theory]
    [InlineData(BlazorAppName)]
    [InlineData(WinAppName)]
    public void SetTaskAction(string applicationName) {
        FixtureContext.DropDB(MainDemoDBName);
        var appContext = FixtureContext.CreateApplicationContext(applicationName);
        appContext.RunApplication();
        appContext.GetForm().FillForm(("User Name", "Sam"));
        appContext.GetAction("Log In").Execute();
        appContext.Navigate("Tasks");
        appContext.GetGrid().ProcessRow(("Subject", "Task1"));

        Assert.Equal("Set Task Low", appContext.GetAction("Set Task").Hint);
        Assert.True(appContext.GetAction("Set Task").Enabled);
        Assert.True(appContext.GetAction("Set Task").IsItemEnabled("Priority"));
        Assert.True(appContext.GetAction("Set Task").IsItemVisible("Priority"));
        Assert.True(appContext.GetAction("Set Task").IsItemEnabled("Status.In progress"));
        Assert.True(appContext.GetAction("Set Task").IsItemVisible("Status.In progress"));

        Assert.True(appContext.GetAction("Set Task").Execute("Status.In progress"));
        Assert.Equal("In progress", appContext.GetForm().GetPropertyValue("Status"));

        Assert.True(appContext.GetAction("Set Task").Execute("Priority.Low"));
        Assert.Equal("Low", appContext.GetForm().GetPropertyValue("Priority"));
    }

    [Fact]
    public void CheckDepartmentValue() {
        FixtureContext.DropDB(MainDemoDBName);
        var appContext = FixtureContext.CreateApplicationContext(BlazorAppName).AsBlazor();
        appContext.RunApplication();
        appContext.GetForm().FillForm(("User Name", "Sam"));
        appContext.GetAction("Log In").Execute();
        appContext.Navigate("Employees");
        appContext.GetGrid().ProcessRow(("Last Name", "Jablonski"));
        Assert.Equal("Development Department (205, Building 2)", appContext.GetForm().GetPropertyValue("Department"));
    }

    [Theory]
    [InlineData(BlazorAppName)]
    [InlineData(WinAppName)]
    public void FilterTasks(string applicationName) {
        FixtureContext.DropDB(MainDemoDBName);
        var appContext = FixtureContext.CreateApplicationContext(applicationName);
        appContext.RunApplication();
        appContext.GetForm().FillForm(("User Name", "Sam"));
        appContext.GetAction("Log In").Execute();
        appContext.Navigate("Tasks");

        Assert.Equal("", appContext.GetAction("Filter by Text").Value);
        Assert.True(appContext.GetAction("Filter by Text").Enabled);
        Assert.Equal("Filter records by text", appContext.GetAction("Filter by Text").Hint);
        Assert.Equal(4, appContext.GetGrid().GetRowCount());

        Assert.True(appContext.GetAction("Filter by Text").Execute("Task1"));
        Assert.Equal("Task1", appContext.GetAction("Filter by Text").Value);
        Assert.Equal(1, appContext.GetGrid().GetRowCount());
    }

    [Theory]
    [InlineData(BlazorAppName)]
    [InlineData(WinAppName)]
    public void DeleteTask(string applicationName) {
        FixtureContext.DropDB(MainDemoDBName);
        var appContext = FixtureContext.CreateApplicationContext(applicationName);
        appContext.RunApplication();
        appContext.GetForm().FillForm(("User Name", "Sam"));
        appContext.GetAction("Log In").Execute();
        appContext.Navigate("Tasks");
        Assert.Equal(4, appContext.GetGrid().GetRowCount());
        appContext.GetGrid().ClearSelection();
        appContext.GetGrid().SelectRows("Subject", "Task1");

        appContext.GetAction("Delete").Execute();
        Assert.Equal("You are about to delete the selected record(s). Do you want to proceed?", appContext.GetDialog().Message);
        appContext.GetDialog().Click("No");

        appContext.GetGrid().ClearSelection();
        appContext.GetGrid().SelectRows("Subject", "Task2");
        appContext.GetAction("Delete").Execute();
        appContext.GetDialog().Click("Yes");
        Assert.Equal(3, appContext.GetGrid().GetRowCount());
    }
}

using Xunit;
using MainDemo.WebAPI.TestInfrastructure;
using System.Net;
using MainDemo.Module.BusinessObjects;
using DevExpress.ExpressApp.Security;
using Microsoft.Extensions.DependencyInjection;
using DevExpress.ExpressApp;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp.DC;
using DevExpress.Data.Helpers;
using System.Text;
using System.Net.Http.Headers;
using System.Net.Http;

namespace MainDemo.WebAPI.Tests;

public class CRUDTests : BaseWebApiTest {
    public CRUDTests(SharedTestHostHolder fixture) : base(fixture) { }

    [Fact]
    public async System.Threading.Tasks.Task CheckUnauthorizedAccess() {
        try {
            await WebApiClient.GetAllAsync(typeof(Resume), true);
            Assert.Fail("The HttpRequestException is expected : Unauthorized");
        }
        catch(HttpRequestException e) {
            Assert.Equal(HttpStatusCode.Unauthorized, e.StatusCode);
        }
    }

    [Theory]
    [InlineData(typeof(Employee))]
    [InlineData(typeof(Resume))]
    public async System.Threading.Tasks.Task GetBasic(Type boType) {
        await WebApiClient.AuthenticateAsync("John", "");
        var items = await WebApiClient.GetAllAsync(boType);
        Assert.True(items.Length > 0);
    }


    [Fact]
    public async System.Threading.Tasks.Task CreateApplicationUser() {
        await WebApiClient.AuthenticateAsync("Sam", "");
        ApplicationUser newUser = null;

        try {
            newUser = await WebApiClient.PostAsync(new ApplicationUser() {
                UserName = "<TestUser>"
            });

            Assert.NotNull(newUser);
            Assert.Equal("<TestUser>", newUser.UserName);

            var newUser_Loaded = await WebApiClient.GetByKeyAsync<ApplicationUser>(newUser.ID.ToString());
            Assert.Equal(newUser.UserName, newUser_Loaded.UserName);
            Assert.Equal(newUser.ID, newUser_Loaded.ID);
        }
        finally {
            if(newUser != null) {
                var deletedObjectKey = (await WebApiClient.DeleteAsync<ApplicationUser>(newUser.ID.ToString())).ID;
                Assert.Equal(newUser.ID, deletedObjectKey);
            }
        }
    }
    [Fact]
    public async System.Threading.Tasks.Task GetApplicationUserWithRef() {
        await WebApiClient.AuthenticateAsync("Sam", "");

        var results = await WebApiClient.GetAllAsync<ApplicationUser>(nameof(ApplicationUser.UserLogins));
        var admin = results.First(x => x.UserName == "Sam");
        Assert.Equal(SecurityDefaults.PasswordAuthentication, admin.UserLogins[0].LoginProviderName);
    }

    [Fact]
    public async System.Threading.Tasks.Task Assign_an_object_to_a_reference_property() {
        await WebApiClient.AuthenticateAsync("Sam", "");

        Employee _newEmployee = null;
        try {
            _newEmployee = await WebApiClient.PostAsync(new Employee() {
                FirstName = "Test",
                LastName = "Test",
                Email = "Test@com.com",
            });

            var newEmployee = await WebApiClient.GetByKeyAsync<Employee>(_newEmployee.ID.ToString(), nameof(Employee.Department));
            Assert.NotNull(newEmployee);
            Assert.Null(newEmployee.Department);


            var departments = await WebApiClient.GetAllAsync<Department>();
            var department = departments.First();
            Assert.NotNull(department);

            await WebApiClient.CreateRefAsync<Employee, Department>(newEmployee.ID.ToString(), nameof(Employee.Department), department.ID.ToString());

            newEmployee = await WebApiClient.GetByKeyAsync<Employee>(newEmployee.ID.ToString(), nameof(Employee.Department));
            Assert.NotNull(newEmployee);
            Assert.NotNull(newEmployee.Department);
            Assert.Equal(newEmployee.Department.ID, department.ID);
        }
        finally {
            if(_newEmployee != null) {
                var deletedObjectKey = (await WebApiClient.DeleteAsync<Employee>(_newEmployee.ID.ToString())).ID;
                Assert.Equal(_newEmployee.ID, deletedObjectKey);
            }
        }
    }

    [Fact]
    public async System.Threading.Tasks.Task Add_an_object_to_a_collection() {
        await WebApiClient.AuthenticateAsync("Sam", "");

        DemoTask _newTask = null;
        try {
            _newTask = await WebApiClient.PostAsync(new DemoTask() {
                Subject = "123",
                Status = Module.BusinessObjects.TaskStatus.NotStarted
            });
            ;

            var newTask = await WebApiClient.GetByKeyAsync<DemoTask>(_newTask.ID.ToString(), nameof(DemoTask.Employees));
            Assert.NotNull(newTask);
            Assert.Empty(newTask.Employees);


            var employees = await WebApiClient.GetAllAsync<Employee>();
            var employee = employees.First();
            Assert.NotNull(employee);
            if(employee.Tasks != null) {
                Assert.Null(employee.Tasks.FirstOrDefault(r => r.ID == newTask.ID));
            }

            await WebApiClient.CreateRefAsync<Employee, DemoTask>(employee.ID.ToString(), nameof(Employee.Tasks), newTask.ID.ToString());

            employee = await WebApiClient.GetByKeyAsync<Employee>(employee.ID.ToString(), nameof(employee.Tasks));
            Assert.NotNull(employee);
            Assert.NotNull(employee.Tasks.FirstOrDefault(r => r.ID == newTask.ID));

            newTask = await WebApiClient.GetByKeyAsync<DemoTask>(newTask.ID.ToString(), nameof(DemoTask.Employees));
            Assert.NotNull(newTask.Employees.FirstOrDefault(r => r.ID == employee.ID));
        }
        finally {
            if(_newTask != null) {
                var deletedObjectKey = (await WebApiClient.DeleteAsync<DemoTask>(_newTask.ID.ToString())).ID;
                Assert.Equal(_newTask.ID, deletedObjectKey);
            }
        }
    }

    [Fact]
    public async System.Threading.Tasks.Task GetObjectWithComputedProperties() {
        await WebApiClient.AuthenticateAsync("John", "");
        var items = await WebApiClient.GetAllAsync<Paycheck>();
        Assert.True(items.Length > 0);

        var paycheck = items[0];
       
        var expectedGrossPay = (paycheck.PayRate * paycheck.Hours) + (paycheck.OvertimePayRate * paycheck.OvertimeHours);
        var actualGrossPay =  paycheck.GrossPay;
        Assert.Equal(expectedGrossPay, actualGrossPay);

        var expectedTotalTax = Convert.ToDecimal(Convert.ToDouble(paycheck.GrossPay) * paycheck.TaxRate);
        var actualTotalTax = paycheck.TotalTax;
        Assert.Equal(expectedTotalTax, actualTotalTax);

        var expectedNetPay = paycheck.GrossPay - paycheck.TotalTax;
        var actualNetPay = paycheck.NetPay;
        Assert.Equal(expectedNetPay, actualNetPay);
    }

    [Fact]
    public async System.Threading.Tasks.Task FilterObjectWithComputedProperties() {
        await WebApiClient.AuthenticateAsync("John", "");
        var items = await WebApiClient.GetAllAsync<Paycheck>();
        Assert.True(items.Length > 0);

        var filteredGrossPay = await WebApiClient.GetFilteredAsync<Paycheck>("GrossPay gt 700");
        Assert.Equal(items.Count(t => t.GrossPay > 700), filteredGrossPay.Length);

        var filteredTotalTax = await WebApiClient.GetFilteredAsync<Paycheck>("TotalTax gt 200");
        Assert.Equal(items.Count(t => t.TotalTax > 200), filteredTotalTax.Length);

        var filteredNetPay = await WebApiClient.GetFilteredAsync<Paycheck>("NetPay gt 500");
        Assert.Equal(items.Count(t => t.NetPay > 500), filteredNetPay.Length);
    }

    public override ValueTask InitializeAsync() {
        ClearTestData();
        return base.InitializeAsync();
    }
    public override ValueTask DisposeAsync() {
        ClearTestData();
        return base.DisposeAsync();
    }
    private void ClearTestData() {
        using var scope = fixture.Host.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var osFactory = scope.ServiceProvider.GetRequiredService<INonSecuredObjectSpaceFactory>();
        using var os = osFactory.CreateNonSecuredObjectSpace<Employee>();
 
        var usersToDelete = os.GetObjectsQuery<ApplicationUser>().Where(e => e.UserName == "New User").ToArray();
        foreach(var toDelete in usersToDelete) {
            os.Delete(toDelete);
        }
        os.CommitChanges();

        var employeesToDelete = os.GetObjectsQuery<Employee>().Where(e => e.Email == "Test@com.com").ToArray();
        foreach(var toDelete in employeesToDelete) {
            os.Delete(toDelete);
        }
        os.CommitChanges();
    }
}

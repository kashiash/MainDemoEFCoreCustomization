using System.Net;
using DevExpress.ExpressApp;
using MainDemo.Module.BusinessObjects;
using MainDemo.WebAPI.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MainDemo.WebAPI.Tests;

public class CustomValidationTests : BaseWebApiTest {
    public CustomValidationTests(SharedTestHostHolder fixture) : base(fixture) { }

    [Fact]
    public async System.Threading.Tasks.Task CreateApplicationUser_ValidateUserNameIsNotEmpty() {
        await WebApiClient.AuthenticateAsync("Sam", "");
        var errorResult = await Assert.ThrowsAnyAsync<HttpRequestException>(() => WebApiClient.PostAsync(new ApplicationUser()));

        string expectedErrorMessage =
            $"Bad Request : Data Validation Error: Please review and correct the data validation error(s) listed below to proceed.{Environment.NewLine}" +
            $" - The user name must not be empty";
        Assert.Equal(expectedErrorMessage, errorResult.Message);
        Assert.Equal(HttpStatusCode.BadRequest, errorResult.StatusCode);


        var newUser = _ = await WebApiClient.PostAsync(new ApplicationUser() {
            UserName = "CreateApplicationUser_ValidateUserNameIsNotEmpty"
        });
        Assert.NotNull(newUser);
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
        using var os = osFactory.CreateNonSecuredObjectSpace<ApplicationUser>();

        var usersToDelete = os.GetObjectsQuery<ApplicationUser>().Where(e => e.UserName == "CreateApplicationUser_ValidateUserNameIsNotEmpty").ToArray();
        foreach(var toDelete in usersToDelete) {
            os.Delete(toDelete);
        }
        os.CommitChanges();
    }
}


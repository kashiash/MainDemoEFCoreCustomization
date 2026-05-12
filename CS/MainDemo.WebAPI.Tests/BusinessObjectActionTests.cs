using System.Net;
using MainDemo.Module.BusinessObjects;
using MainDemo.WebAPI.TestInfrastructure;
using Xunit;

namespace MainDemo.WebAPI.Tests;
public class BusinessObjectActionTests : BaseWebApiTest {
    public BusinessObjectActionTests(SharedTestHostHolder fixture) : base(fixture) { }

    [Fact]
    public async System.Threading.Tasks.Task Invoke_business_object_action() {
        await WebApiClient.AuthenticateAsync("Sam", "");

        var today = DateTime.Now.Date;
        var tomorrow = today.AddDays(1);
        var afterTomorrow = today.AddDays(2);

        DemoTask demoTask = new DemoTask() {
            Description = "Test demo task",
            Subject = "Do something",
            ActualWorkHours = 1,
            EstimatedWorkHours = 2,
            StartDate = today,
            DueDate = tomorrow,
            Status = Module.BusinessObjects.TaskStatus.NotStarted
        };

        try {
            demoTask = await WebApiClient.PostAsync(demoTask);
            Assert.NotNull(demoTask);

            var uri = WebApiClient.GetTypeUri<DemoTask>(demoTask.ID.ToString()) + "/" + nameof(DemoTask.Postpone);            
            using var response = await WebApiClient.SendAsync(new HttpRequestMessage(HttpMethod.Post, uri));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            demoTask = await WebApiClient.GetByKeyAsync<DemoTask>(demoTask.ID.ToString());
            Assert.NotNull(demoTask);
            Assert.Equal(afterTomorrow, demoTask.DueDate);
        }
        finally {
            _ = await WebApiClient.DeleteAsync<DemoTask>(demoTask.ID.ToString());
        }
    }
}

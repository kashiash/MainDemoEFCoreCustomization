using System.Net.Http.Headers;
using DevExpress.ExpressApp;
using MainDemo.Module.BusinessObjects;
using MainDemo.WebAPI.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MainDemo.WebAPI.Tests;

public class MediaFileTests : BaseWebApiTest {
    public MediaFileTests(SharedTestHostHolder fixture) : base(fixture) { }

    [Fact]
    public async System.Threading.Tasks.Task LoadApplicationUserPhotoTest() {
        await WebApiClient.AuthenticateAsync("Sam", "");
        var userSamId = (await WebApiClient.GetAllAsync<ApplicationUser>()).First(u => u.UserName == "Sam").ID;

        var photo = await WebApiClient.DownloadStream<ApplicationUser>(userSamId.ToString(), nameof(ApplicationUser.Photo));
        Assert.True(photo.Length > 1000);
    }

    [Fact]
    public async System.Threading.Tasks.Task Upload_ApplicationUser_Photo() {
        await WebApiClient.AuthenticateAsync("Sam", "");

        var newUser = await WebApiClient.PostAsync(new ApplicationUser() {
            UserName = "<TestUser>"
        });

        using var formDataContent = new MultipartFormDataContent();

        var fileContent = new ByteArrayContent(Convert.FromBase64String(Utils.SamPhoto_String64));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        formDataContent.Add(fileContent, "file", "file");
        formDataContent.Add(new StringContent(typeof(ApplicationUser).Name), "objectType");
        formDataContent.Add(new StringContent(newUser.ID.ToString()), "objectKey");
        formDataContent.Add(new StringContent(nameof(ApplicationUser.Photo)), "propertyName");

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/MediaFile/UploadStream");
        request.Content = formDataContent;
        var httpResponse = await WebApiClient.SendAsync(request);

        httpResponse.EnsureSuccessStatusCode();

        var photo = await WebApiClient.DownloadStream<ApplicationUser>(newUser.ID.ToString(), nameof(ApplicationUser.Photo));
        Assert.Equal(Convert.FromBase64String(Utils.SamPhoto_String64), photo);
    }

    [Fact]
    public async System.Threading.Tasks.Task Upload_FileData() {
        await WebApiClient.AuthenticateAsync("Sam", "");

        var _newEmployee = await WebApiClient.PostAsync(new Employee() {
            FirstName = "Test",
            LastName = "Test",
            Email = "MediaFileTests@com.com",
        });
        var resume = await WebApiClient.PostAsync(new Resume() {
            Employee = _newEmployee
        });

        using var formDataContent = new MultipartFormDataContent();

        var fileContent = new ByteArrayContent(Convert.FromBase64String(Utils.JohnPhoto_String64));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        formDataContent.Add(fileContent, "file", "JohnPhoto.jpg");
        formDataContent.Add(new StringContent(typeof(Resume).Name), "objectType");
        formDataContent.Add(new StringContent(resume.ID.ToString()), "objectKey");
        formDataContent.Add(new StringContent(nameof(Resume.File)), "propertyName");

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/MediaFile/UploadStream");
        request.Content = formDataContent;
        var httpResponse = await WebApiClient.SendAsync(request);

        httpResponse.EnsureSuccessStatusCode();

        var fileData = await WebApiClient.DownloadStream<Resume>(resume.ID.ToString(), nameof(Resume.File));
        Assert.Equal(Convert.FromBase64String(Utils.JohnPhoto_String64), fileData);
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

        var usersToDelete = os.GetObjectsQuery<ApplicationUser>().Where(e => e.UserName == "<TestUser>" || e.UserName == "New User").ToArray();
        foreach(var toDelete in usersToDelete) {
            os.Delete(toDelete);
        }
        os.CommitChanges();

        var employeesToDelete = os.GetObjectsQuery<Employee>().Where(e => e.Email == "MediaFileTests@com.com").ToArray();
        foreach(var toDelete in employeesToDelete) {
            os.Delete(toDelete);
        }
        os.CommitChanges();
    }
}


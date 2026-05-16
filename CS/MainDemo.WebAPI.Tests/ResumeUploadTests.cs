using System.Net.Http.Headers;
using DevExpress.ExpressApp;
using MainDemo.Module.BusinessObjects;
using MainDemo.WebAPI.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MainDemo.WebAPI.Tests;

public class ResumeUploadTests : BaseWebApiTest {
    private const string ResumeUploadEmployeeEmail = "resume-upload-tests@maindemo.local";

    public ResumeUploadTests(SharedTestHostHolder fixture) : base(fixture) {
    }

    [Fact]
    public async Task Can_add_multiple_resume_pdfs_to_employee_via_upload_endpoint() {
        await WebApiClient.AuthenticateAsync("Sam", "");

        Guid employeeId;
        using (var scope = fixture.Host.Services.GetRequiredService<IServiceScopeFactory>().CreateScope()) {
            var objectSpaceFactory = scope.ServiceProvider.GetRequiredService<INonSecuredObjectSpaceFactory>();
            using var objectSpace = objectSpaceFactory.CreateNonSecuredObjectSpace(typeof(Employee));

            var testEmployee = objectSpace.CreateObject<Employee>();
            testEmployee.FirstName = "Resume";
            testEmployee.LastName = "Upload";
            testEmployee.Email = ResumeUploadEmployeeEmail;
            objectSpace.CommitChanges();
            employeeId = testEmployee.ID;
        }

        using var formDataContent = new MultipartFormDataContent();
        AddFile(formDataContent, "cv-1.pdf", Utils.JohnPhoto_String64);
        AddFile(formDataContent, "cv-2.pdf", Utils.SamPhoto_String64);
        formDataContent.Add(new StringContent(employeeId.ToString()), "employeeId");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/resumes/upload") {
            Content = formDataContent
        };

        var response = await WebApiClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var verificationScope = fixture.Host.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var verificationFactory = verificationScope.ServiceProvider.GetRequiredService<INonSecuredObjectSpaceFactory>();
        using var verificationObjectSpace = verificationFactory.CreateNonSecuredObjectSpace(typeof(Employee));

        var loadedEmployee = verificationObjectSpace.GetObjectsQuery<Employee>()
            .Where(item => item.Email == ResumeUploadEmployeeEmail)
            .Single();

        var resumes = loadedEmployee.Resumes.ToList();
        Assert.Equal(2, resumes.Count);
        Assert.All(resumes, item => Assert.NotNull(item.File));
        Assert.All(resumes, item => Assert.True(item.File.Size > 0));
    }

    public override ValueTask InitializeAsync() {
        ClearTestData();
        return base.InitializeAsync();
    }

    public override ValueTask DisposeAsync() {
        ClearTestData();
        return base.DisposeAsync();
    }

    private static void AddFile(MultipartFormDataContent formDataContent, string fileName, string base64Content) {
        var fileContent = new ByteArrayContent(Convert.FromBase64String(base64Content));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        formDataContent.Add(fileContent, "files", fileName);
    }

    private void ClearTestData() {
        using var scope = fixture.Host.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var objectSpaceFactory = scope.ServiceProvider.GetRequiredService<INonSecuredObjectSpaceFactory>();
        using var objectSpace = objectSpaceFactory.CreateNonSecuredObjectSpace(typeof(Employee));

        var employees = objectSpace.GetObjectsQuery<Employee>()
            .Where(item => item.Email == ResumeUploadEmployeeEmail)
            .ToArray();
        foreach (var employee in employees) {
            objectSpace.Delete(employee);
        }

        objectSpace.CommitChanges();
    }
}

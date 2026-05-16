using System.Net.Http.Headers;
using DevExpress.ExpressApp;
using MainDemo.Module.BusinessObjects;
using MainDemo.WebAPI.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MainDemo.WebAPI.Tests;

public class DocumentFileUploadTests : BaseWebApiTest {
    private const string TestEmployeeEmail = "document-upload-tests@maindemo.local";

    public DocumentFileUploadTests(SharedTestHostHolder fixture) : base(fixture) {
    }

    [Fact]
    public async Task Can_attach_multiple_files_to_employee_via_upload_endpoint() {
        await WebApiClient.AuthenticateAsync("Sam", "");

        Guid employeeId;
        Guid invoiceTypeId;
        using (var scope = fixture.Host.Services.GetRequiredService<IServiceScopeFactory>().CreateScope()) {
            var objectSpaceFactory = scope.ServiceProvider.GetRequiredService<INonSecuredObjectSpaceFactory>();
            using var objectSpace = objectSpaceFactory.CreateNonSecuredObjectSpace(typeof(Employee));

            var testEmployee = objectSpace.CreateObject<Employee>();
            testEmployee.FirstName = "Document";
            testEmployee.LastName = "Upload";
            testEmployee.Email = TestEmployeeEmail;
            objectSpace.CommitChanges();
            employeeId = testEmployee.ID;

            invoiceTypeId = objectSpace.FirstOrDefault<DocumentFileType>(item => item.Code == "INVOICE").ID;
        }

        using var formDataContent = new MultipartFormDataContent();
        AddFile(formDataContent, "faktura-1.pdf", Utils.JohnPhoto_String64);
        AddFile(formDataContent, "faktura-2.pdf", Utils.SamPhoto_String64);
        AddFile(formDataContent, "scan-1.png", Utils.JohnPhoto_String64);
        formDataContent.Add(new StringContent(nameof(Employee)), "ownerObjectType");
        formDataContent.Add(new StringContent(employeeId.ToString()), "ownerObjectId");
        formDataContent.Add(new StringContent(invoiceTypeId.ToString()), "typeId");
        formDataContent.Add(new StringContent("Pakiet testowych załączników"), "description");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/document-files/upload") {
            Content = formDataContent
        };

        var response = await WebApiClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var verificationScope = fixture.Host.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var verificationFactory = verificationScope.ServiceProvider.GetRequiredService<INonSecuredObjectSpaceFactory>();
        using var verificationObjectSpace = verificationFactory.CreateNonSecuredObjectSpace(typeof(Employee));

        var loadedEmployee = verificationObjectSpace.GetObjectsQuery<Employee>()
            .Where(item => item.Email == TestEmployeeEmail)
            .Single();
        var documents = loadedEmployee.DocumentFiles.ToList();

        Assert.Equal(3, documents.Count);
        Assert.All(documents, item => Assert.Equal("INVOICE", item.Type.Code));
        Assert.All(documents, item => Assert.True(item.File.Size > 0));
    }

    [Fact]
    public async Task Upload_endpoint_uses_OTHER_type_when_type_is_not_provided() {
        await WebApiClient.AuthenticateAsync("Sam", "");

        Guid employeeId;
        using (var scope = fixture.Host.Services.GetRequiredService<IServiceScopeFactory>().CreateScope()) {
            var objectSpaceFactory = scope.ServiceProvider.GetRequiredService<INonSecuredObjectSpaceFactory>();
            using var objectSpace = objectSpaceFactory.CreateNonSecuredObjectSpace(typeof(Employee));

            var testEmployee = objectSpace.CreateObject<Employee>();
            testEmployee.FirstName = "Document";
            testEmployee.LastName = "Fallback";
            testEmployee.Email = "document-upload-tests-fallback@maindemo.local";
            objectSpace.CommitChanges();
            employeeId = testEmployee.ID;
        }

        using var formDataContent = new MultipartFormDataContent();
        AddFile(formDataContent, "bez-typu.pdf", Utils.JohnPhoto_String64);
        formDataContent.Add(new StringContent(nameof(Employee)), "ownerObjectType");
        formDataContent.Add(new StringContent(employeeId.ToString()), "ownerObjectId");
        formDataContent.Add(new StringContent(string.Empty), "typeId");
        formDataContent.Add(new StringContent("Domyślny typ dokumentu"), "description");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/document-files/upload") {
            Content = formDataContent
        };

        var response = await WebApiClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var verificationScope = fixture.Host.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var verificationFactory = verificationScope.ServiceProvider.GetRequiredService<INonSecuredObjectSpaceFactory>();
        using var verificationObjectSpace = verificationFactory.CreateNonSecuredObjectSpace(typeof(Employee));

        var loadedEmployee = verificationObjectSpace.GetObjectsQuery<Employee>()
            .Where(item => item.Email == "document-upload-tests-fallback@maindemo.local")
            .Single();
        var document = loadedEmployee.DocumentFiles.Single();

        Assert.Equal("OTHER", document.Type.Code);
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
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        formDataContent.Add(fileContent, "files", fileName);
    }

    private void ClearTestData() {
        using var scope = fixture.Host.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var objectSpaceFactory = scope.ServiceProvider.GetRequiredService<INonSecuredObjectSpaceFactory>();
        using var objectSpace = objectSpaceFactory.CreateNonSecuredObjectSpace(typeof(Employee));

        var employees = objectSpace.GetObjectsQuery<Employee>()
            .Where(item => item.Email == TestEmployeeEmail || item.Email == "document-upload-tests-fallback@maindemo.local")
            .ToArray();
        foreach (var employee in employees) {
            objectSpace.Delete(employee);
        }

        objectSpace.CommitChanges();
    }
}

using System.Text;
using DevExpress.ExpressApp;
using MainDemo.Module.BusinessObjects;
using MainDemo.WebAPI.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MainDemo.WebAPI.Tests;

public class MediaFile_DeepUpdate_Tests : BaseWebApiTest {
    public MediaFile_DeepUpdate_Tests(SharedTestHostHolder fixture) : base(fixture) { }

    [Fact]
    public async System.Threading.Tasks.Task DeepCreate_with_FileData() {
        await WebApiClient.AuthenticateAsync("Sam", "");

        var _newEmployee = await WebApiClient.PostAsync(new Employee() {
            FirstName = "Test2",
            LastName = "Test2",
            Email = "MediaFileTests@com.com"
        });

        byte[] bytes = Encoding.UTF8.GetBytes("FILE DATA");
        string base64 = Convert.ToBase64String(bytes);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/odata/Resume");
        request.Content = new StringContent(
            $@"{{
                    ""Employee"": {{
                        ""ID"": ""{_newEmployee.ID}""
                    }},
                    ""File"": {{
                        ""Content"": ""{base64}"",
                        ""FileName"": ""DeepCreate_with_FileData_test.txt""
                    }}
                }}
                ", Encoding.UTF8, "application/json");

        var httpResponse = await WebApiClient.SendAsync(request);
        httpResponse.EnsureSuccessStatusCode();
        var result = await httpResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        var keys = Utils.ExtractGuids(result);
        var fileData = await WebApiClient.DownloadStream<Resume>(keys[0], nameof(Resume.File));
        Assert.Equal(bytes, fileData);
    }

    [Fact]
    public async Task DeepCreate_ApplicationUser_Photo() {
        await WebApiClient.AuthenticateAsync("Sam", "");
        if(HttpClient.DefaultRequestHeaders.Authorization == null) {
            HttpClient.DefaultRequestHeaders.Authorization = WebApiClient.AuthorizationToken;
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            HttpClient.DefaultRequestHeaders.Add("Prefer", "return=representation");
            HttpClient.DefaultRequestHeaders.Add("OData-Version", "4.01");
        }

        string userPassword = DevExpress.Persistent.Base.PasswordCryptographer.HashPasswordDelegate(Guid.NewGuid().ToString());
        using var createUserResponse = await HttpClient.PostAsync("/api/odata/ApplicationUser?$expand=Photo($expand=MediaResource)", new StringContent(
            $@"{{
                    ""UserName"": ""New User"",
                    ""StoredPassword"":""{userPassword}"",
                    ""Photo"": {{
                        ""MediaDataKey"": ""{Guid.NewGuid().ToString("N")}"",
                        ""MediaResource"": {{
                            ""MediaData"": ""{Utils.SamPhoto_String64}""
                        }}
                    }}
                }}
                ", Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        createUserResponse.EnsureSuccessStatusCode();
        var result = await createUserResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        #region response content
        /*
        {
            "@context":"http://localhost/api/odata/$metadata#ApplicationUser(Photo(MediaResource()))/$entity",
            "ID":"a33f42ca-cb35-4b47-4f18-08dd8f10e013",
            "UserName":"New User",
            "IsActive":true,
            "ChangePasswordOnFirstLogon":false
            "AccessFailedCount":0,
            "LockoutEnd":"0001-01-01T00:00:00Z",
            "Photo":{
                "ID":"5e52c21a-d52a-4c21-782b-08dd8f10e015",
                "MediaDataKey":"b056c67159a44a20bb859d0df9877fcb",
                "MediaResource":{
                    "ID":"5e52c21a-d52a-4c21-782b-08dd8f10e015",
                    "MediaData":"{Utils.SamPhoto_String64}"
                }
            }
        }
         */
        #endregion

        #region  Validate response
        var keys = Utils.ExtractGuids(result);
        int startIndex = result.IndexOf("\"MediaDataKey\":\"") + "\"MediaDataKey\":\"".Length;
        string mediaDataKey = result.Substring(startIndex, 32);
        string expectedResponse = $"" +
            $"{{" +
                $"\"@context\":\"http://localhost/api/odata/$metadata#ApplicationUser(Photo(MediaResource()))/$entity\"," +
                $"\"ID\":\"{keys[0]}\"," +
                $"\"UserName\":\"New User\"," +
                $"\"IsActive\":true," +
                $"\"ChangePasswordOnFirstLogon\":false," +
                $"\"AccessFailedCount\":0," +
                $"\"LockoutEnd\":\"0001-01-01T00:00:00Z\"," +
                $"\"Photo\":{{" +
                    $"\"ID\":\"{keys[1]}\"," +
                    $"\"MediaDataKey\":\"{mediaDataKey}\"," +
                    $"\"MediaResource\":{{" +
                        $"\"ID\":\"{keys[2]}\"," +
                        $"\"MediaData\":\"{Utils.SamPhoto_String64}\"" +
                    $"}}" +
                $"}}" +
            $"}}";

        Assert.Equal(expectedResponse, result);
        #endregion
    }

    [Fact]
    public async Task DeepUpdate_ApplicationUser_Photo() {
        await DeepCreate_ApplicationUser_Photo();

        using var getUserResponse = await HttpClient.GetAsync("/api/odata/ApplicationUser?$filter=UserName eq 'New User'&$expand=Photo($expand=MediaResource)", TestContext.Current.CancellationToken);

        getUserResponse.EnsureSuccessStatusCode();
        var result = await getUserResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);


        var keys = Utils.ExtractGuids(result);
        using var updateDepartmentResponse = await HttpClient.PatchAsync(
            $"/api/odata/ApplicationUser/{keys[0]}?$expand=Photo($expand=MediaResource)",
            new StringContent(
            $@"{{
                    ""Photo"": {{
                        ""MediaDataKey"": ""{Guid.NewGuid().ToString("N")}"",
                        ""MediaResource"": {{
                            ""MediaData"": ""{Utils.JohnPhoto_String64}""
                        }}
                    }}
                }}", Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);


        updateDepartmentResponse.EnsureSuccessStatusCode();
        var response = await updateDepartmentResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        #region response content
        /*
        {
            "@context":"http://localhost/api/odata/$metadata#ApplicationUser(Photo(MediaResource()))/$entity",
            "ID":"a33f42ca-cb35-4b47-4f18-08dd8f10e013",
            "UserName":"New User",
            "IsActive":true,
            "ChangePasswordOnFirstLogon":false,
            "AccessFailedCount":0,
            "LockoutEnd":"0001-01-01T00:00:00Z",
            "Photo":{
                "ID":"5e52c21a-d52a-4c21-782b-08dd8f10e015",
                "MediaDataKey":"b056c67159a44a20bb859d0df9877fcb",
                "MediaResource":{
                    "ID":"5e52c21a-d52a-4c21-782b-08dd8f10e015",
                    "MediaData":"{Utils.JohnPhoto_String64}"
                }
            }
        }
         */
        #endregion

        #region  Validate response
        int startIndex = response.IndexOf("\"MediaDataKey\":\"") + "\"MediaDataKey\":\"".Length;
        string mediaDataKey = response.Substring(startIndex, 32);
        string expectedResponse = $"" +
            $"{{" +
                $"\"@context\":\"http://localhost/api/odata/$metadata#ApplicationUser(Photo(MediaResource()))/$entity\"," +
                $"\"ID\":\"{keys[0]}\"," +
                $"\"UserName\":\"New User\"," +
                $"\"IsActive\":true," +
                $"\"ChangePasswordOnFirstLogon\":false," +
                $"\"AccessFailedCount\":0," +
                $"\"LockoutEnd\":\"0001-01-01T00:00:00Z\"," +
                $"\"Photo\":{{" +
                    $"\"ID\":\"{keys[1]}\"," +
                    $"\"MediaDataKey\":\"{mediaDataKey}\"," +
                    $"\"MediaResource\":{{" +
                        $"\"ID\":\"{keys[2]}\"," +
                        $"\"MediaData\":\"{Utils.JohnPhoto_String64}\"" +
                    $"}}" +
                $"}}" +
            $"}}";

        Assert.Equal(expectedResponse, response);
        #endregion
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


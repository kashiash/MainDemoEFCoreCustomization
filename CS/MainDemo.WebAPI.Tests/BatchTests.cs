using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DevExpress.ExpressApp;
using MainDemo.Module.BusinessObjects;
using MainDemo.WebAPI.TestInfrastructure;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MainDemo.WebAPI.Tests;

[Collection("SharedTestHost")]
public class BatchTests(SharedTestHostHolder fixture) : IAsyncLifetime, IDisposable {
    private readonly JsonSerializerOptions jsonSerializerOptions = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault };
    private HttpClient httpClient = fixture.Host.GetTestClient();

    [Fact]
    public async Task BatchModificationTest() {
        #region Preparing test data
        var employee = await CreateAsync(httpClient, new Employee {
            FirstName = "First Name",
            LastName = "Johnson",
            Email = "BatchTests@example.com",
        });

        var department = await CreateAsync(httpClient, new Department {
            Title = "New department",
            Positions = [new() { Title = "New position in new department" }],
            DepartmentHead = employee,
            Employees = [employee]
        });
        #endregion
        var batchRequestContent =
            @$"{{
                ""requests"": [
                    {{
                      ""method"": ""{HttpMethod.Patch}"",
                      ""url"": ""{$"/api/odata/{typeof(Employee).Name}/{employee.ID}"}"",
                      ""headers"": {{
                        ""content-type"": ""application/json; odata.metadata=minimal; odata.streaming=true"",
                        ""odata-version"": ""4.01"",
                        ""Prefer"": ""return=representation""
                      }},
                      ""id"": ""0"",
                      ""body"": {{
                        ""FirstName"": ""Stefan"",
                        ""Department@delta"": {{
                            ""Title"": ""Department new Title""
                        }}
                      }}
                    }},
                    {{
                      ""method"": ""{HttpMethod.Patch}"",
                      ""url"": ""{$"/api/odata/{typeof(Department).Name}/{department.ID}"}?$expand=Positions"",
                      ""headers"": {{
                        ""content-type"": ""application/json; odata.metadata=minimal; odata.streaming=true"",
                        ""odata-version"": ""4.01"",
                        ""Prefer"": ""return=representation""
                      }},
                      ""id"": ""1"",
                      ""body"": {{
                          ""Office"": ""New office on the top floor""
                      }}
                    }}
                ]
            }}";

        var httpResponse = await httpClient.PostAsync("/api/odata/$batch", new StringContent(batchRequestContent, Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        httpResponse.EnsureSuccessStatusCode();
        string result = await httpResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        #region
        /*
        {"responses":[
            {
                "id":"0",
                "status":200,
                "headers":{"content-type":"application/json; odata.metadata=minimal; odata.streaming=true; charset=utf-8","odata-version":"4.01"},
                "body" :{
                    "@context":"http://localhost/api/odata/$metadata#Employee/$entity",
                    "ID":"4b304322-5e9c-4b59-5196-08dd8cd06915",
                    "FullName":"Stefan Johnson",
                    "FirstName":"Stefan",
                    "LastName":"Johnson",
                    "Email":"BatchTests@example.com",
                    "TitleOfCourtesy":"Dr"
                }
            },
            {
                "id":"1",
                "status":200,
                "headers":{"content-type":"application/json; odata.metadata=minimal; odata.streaming=true; charset=utf-8","odata-version":"4.01"},
                "body" :{
                    "@context":"http://localhost/api/odata/$metadata#Department(Positions())/$entity",
                    "ID":"a3c6d14a-95ad-4bd9-2786-08dd8cd06a53",
                    "Title":"Department new Title",
                    "Office":"New office on the top floor",
                    "Positions":[
                        {
                            "ID":"4b304322-5e9c-4b59-5196-08dd8cd06915",
                            "Title":"New position in new department"
                        }
                    ]
                }
            }
        ]}
        */
        #endregion

        #region  Validate response
        var objectKeys = Utils.ExtractGuids(result);
        string expectedResponse =
            $"{{\"responses\":[" +
                $"{{" +
                    $"\"id\":\"0\"," +
                    $"\"status\":200," +
                    $"\"headers\":{{\"content-type\":\"application/json; odata.metadata=minimal; odata.streaming=true; charset=utf-8\",\"odata-version\":\"4.01\"}}," +
                    $" \"body\" :{{" +
                        $"\"@context\":\"http://localhost/api/odata/$metadata#Employee/$entity\"," +
                        $"\"ID\":\"{employee.ID}\"," +
                        $"\"FullName\":\"Stefan Johnson\"," +
                        $"\"FirstName\":\"Stefan\"," +
                        $"\"LastName\":\"Johnson\"," +
                        $"\"MiddleName\":null," +
                        $"\"Birthday\":null," +
                        $"\"Email\":\"BatchTests@example.com\"," +
                        $"\"Photo\":null," +
                        $"\"WebPageAddress\":null," +
                        $"\"NickName\":null," +
                        $"\"SpouseName\":null," +
                        $"\"Anniversary\":null," +
                        $"\"Notes\":null," +
                        $"\"TitleOfCourtesy\":\"Dr\"" +
                    $"}}" +
                $"}}," +
                $"{{" +
                    $"\"id\":\"1\"," +
                    $"\"status\":200," +
                    $"\"headers\":{{\"content-type\":\"application/json; odata.metadata=minimal; odata.streaming=true; charset=utf-8\",\"odata-version\":\"4.01\"}}," +
                    $" \"body\" :{{" +
                        $"\"@context\":\"http://localhost/api/odata/$metadata#Department(Positions())/$entity\"," +
                        $"\"ID\":\"{department.ID}\"," +
                        $"\"Title\":\"Department new Title\"," +
                        $"\"Office\":\"New office on the top floor\"," +
                        $"\"Location\":null," +
                        $"\"Description\":null," +
                        $"\"Positions\":[" +
                            $"{{" +
                                $"\"ID\":\"{objectKeys[2]}\"," +
                                $"\"Title\":\"New position in new department\"" +
                            $"}}" +
                        $"]" +
                     $"}}" +
                $"}}" +
            $"]}}";


        Assert.Equal(expectedResponse, result);
        #endregion

        #region  Validate response
        department = await GetByKeyAsync<Department>(httpClient, department.ID);
        Assert.Equal("Department new Title", department.Title);
        Assert.Equal("New office on the top floor", department.Office);
        #endregion
    }

    [Fact]
    public async Task BatchQueryTest() {
        #region Preparing test data
        var employee = await CreateAsync(httpClient, new Employee {
            FirstName = "Stefan",
            LastName = "Johnson",
            Email = "BatchTests@example.com",
        });

        var department = await CreateAsync(httpClient, new Department {
            Title = "New department",
            Positions = [new() { Title = "New position in new department" }],
            DepartmentHead = employee,
            Employees = [employee]
        });
        #endregion

        var batchRequestContent =
            @$"{{
                ""requests"": [
                    {{
                      ""method"": ""{HttpMethod.Get}"",
                      ""url"": ""{$"/api/odata/{typeof(Employee).Name}/{employee.ID}"}"",
                      ""headers"": {{
                        ""content-type"": ""application/json; odata.metadata=minimal; odata.streaming=true"",
                        ""odata-version"": ""4.01""
                      }},
                      ""id"": ""0""
                    }},
                    {{
                      ""method"": ""{HttpMethod.Get}"",
                      ""url"": ""{$"/api/odata/{typeof(Department).Name}/{department.ID}"}?$expand=Positions"",
                      ""headers"": {{
                        ""content-type"": ""application/json; odata.metadata=minimal; odata.streaming=true"",
                        ""odata-version"": ""4.01""
                      }},
                      ""id"": ""1""
                    }}
                ]
            }}";

        var httpResponse = await httpClient.PostAsync("/api/odata/$batch", new StringContent(batchRequestContent, Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        httpResponse.EnsureSuccessStatusCode();
        string result = await httpResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        #region
        /*
        {"responses":[
            {
                "id":"0",
                "status":200,
                "headers":{"content-type":"application/json; odata.metadata=minimal; odata.streaming=true; charset=utf-8","odata-version":"4.01"},
                "body" :{
                    "@context":"http://localhost/api/odata/$metadata#Employee/$entity",
                    "ID":"e1574d97-fafd-46be-21cd-08dd8d554255",
                    "FullName":"Stefan Johnson",
                    "FirstName":"Stefan",
                    "LastName":"Johnson",
                    "Email":"BatchTests@example.com",
                    "TitleOfCourtesy":"Dr"
                }
            },
            {
                "id":"1",
                "status":200,
                "headers":{"content-type":"application/json; odata.metadata=minimal; odata.streaming=true; charset=utf-8","odata-version":"4.01"},
                "body" :{
                    "@context":"http://localhost/api/odata/$metadata#Department(Positions())/$entity",
                    "ID":"c666e9e6-bd06-4b17-04a5-08dd8d554376",
                    "Title":"New department",
                    "Positions":[
                        {
                            "ID":"2462f989-969d-415e-e9ab-08dd8d55437e",
                            "Title":"New position in new department"
                        }
                    ]
                }
            }
        ]}
        */
        #endregion

        #region  Validate response
        var objectKeys = Utils.ExtractGuids(result);
        string expectedResponse =
            $"{{\"responses\":[" +
                $"{{" +
                    $"\"id\":\"0\"," +
                    $"\"status\":200," +
                    $"\"headers\":{{\"content-type\":\"application/json; odata.metadata=minimal; odata.streaming=true; charset=utf-8\",\"odata-version\":\"4.01\"}}," +
                    $" \"body\" :{{" +
                        $"\"@context\":\"http://localhost/api/odata/$metadata#Employee/$entity\"," +
                        $"\"ID\":\"{employee.ID}\"," +
                        $"\"FullName\":\"Stefan Johnson\"," +
                        $"\"FirstName\":\"Stefan\"," +
                        $"\"LastName\":\"Johnson\"," +
                        $"\"MiddleName\":null," +
                        $"\"Birthday\":null," +
                        $"\"Email\":\"BatchTests@example.com\"," +
                        $"\"Photo\":null," +
                        $"\"WebPageAddress\":null," +
                        $"\"NickName\":null," +
                        $"\"SpouseName\":null," +
                        $"\"Anniversary\":null," +
                        $"\"Notes\":null," +
                        $"\"TitleOfCourtesy\":\"Dr\"" +
                    $"}}" +
                $"}}," +
                $"{{" +
                    $"\"id\":\"1\"," +
                    $"\"status\":200," +
                    $"\"headers\":{{\"content-type\":\"application/json; odata.metadata=minimal; odata.streaming=true; charset=utf-8\",\"odata-version\":\"4.01\"}}," +
                    $" \"body\" :{{" +
                        $"\"@context\":\"http://localhost/api/odata/$metadata#Department(Positions())/$entity\"," +
                        $"\"ID\":\"{department.ID}\"," +
                        $"\"Title\":\"New department\"," +
                        $"\"Office\":null," +
                        $"\"Location\":null," +
                        $"\"Description\":null," +
                        $"\"Positions\":[" +
                            $"{{" +
                                $"\"ID\":\"{objectKeys[2]}\"," +
                                $"\"Title\":\"New position in new department\"" +
                            $"}}" +
                        $"]" +
                    $"}}" +
                $"}}" +
            $"]}}";


        Assert.Equal(expectedResponse, result);
        #endregion
    }

    private async Task<T> CreateAsync<T>(HttpClient httpClient, T value) {
        using var httpResponse = await httpClient.PostAsJsonAsync($"/api/odata/{typeof(T).Name}", value, jsonSerializerOptions);

        Assert.Null(Record.Exception(httpResponse.EnsureSuccessStatusCode));
        var jsonElement = await httpResponse.Content.ReadFromJsonAsync<JsonElement>();

        var created = jsonElement.Deserialize<T>(jsonSerializerOptions);
        Assert.NotNull(created);
        return created;
    }

    private async Task<T> GetByKeyAsync<T>(HttpClient httpClient, Guid key) {
        using var httpResponse = await httpClient.GetAsync($"/api/odata/{typeof(T).Name}/{key}");
        if(httpResponse.StatusCode is HttpStatusCode.NotFound) {
            return default;
        }

        Assert.Null(Record.Exception(httpResponse.EnsureSuccessStatusCode));
        var jsonElement = await httpResponse.Content.ReadFromJsonAsync<JsonElement>();

        var result = jsonElement.Deserialize<T>(jsonSerializerOptions);
        Assert.NotNull(result);
        return result;
    }


    private async Task<string> GetAuthenticationTokenAsync() {
        StringContent httpContent = new(@"{ ""userName"": ""Sam"", ""password"": """" }", Encoding.UTF8, "application/json");
        var tokenResponse = await httpClient.PostAsync("/api/Authentication/Authenticate", httpContent);
        return await tokenResponse.Content.ReadAsStringAsync();
    }
    public async ValueTask InitializeAsync() {
        ClearTestData();
        if(httpClient.DefaultRequestHeaders.Authorization == null) {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetAuthenticationTokenAsync());
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            httpClient.DefaultRequestHeaders.Add("OData-Version", "4.01");
        }
    }
    public ValueTask DisposeAsync() {
        ClearTestData();
        return ValueTask.CompletedTask;
    }
    public void Dispose() {
        httpClient.Dispose();
    }
    private void ClearTestData() {
        using var scope = fixture.Host.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var osFactory = scope.ServiceProvider.GetRequiredService<INonSecuredObjectSpaceFactory>();
        using var os = osFactory.CreateNonSecuredObjectSpace<Employee>();
        var employeesToDelete = os.GetObjectsQuery<Employee>().Where(e => e.Email == "BatchTests@example.com").ToArray();
        foreach(var toDelete in employeesToDelete) {
            os.Delete(toDelete);
        }
        os.CommitChanges();

        var positionsToDelete = os.GetObjectsQuery<Position>().Where(e => e.Title.StartsWith("New position in new department")).ToArray();
        foreach(var toDelete in positionsToDelete) {
            os.Delete(toDelete);
        }
        os.CommitChanges();

        var departmentsToDelete = os.GetObjectsQuery<Department>().Where(e => e.Title.StartsWith("Department new Title")).ToArray();
        foreach(var toDelete in departmentsToDelete) {
            os.Delete(toDelete);
        }
        os.CommitChanges();
    }
}

using System.Net.Http.Headers;
using System.Text;
using DevExpress.ExpressApp;
using MainDemo.Module.BusinessObjects;
using MainDemo.WebAPI.TestInfrastructure;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MainDemo.WebAPI.Tests.HelpTopicTestExamples {
    [Collection("SharedTestHost")]
    public sealed class CRUD_DeepUpdate_Tests(SharedTestHostHolder fixture) : IAsyncLifetime, IDisposable {
        private HttpClient httpClient = fixture.Host.GetTestClient();

        [Fact]
        public async Task DeepCreateObjectAsync() {
            var data = @"{
                ""FirstName"": ""Mary"",
                ""LastName"":""Gordon"",
                ""Email"":""CRUDTests@example.com"",
                ""Tasks"": [
                    { ""Description"":""Foo"",""Subject"":""Bar"" }
                ]
            }";

            using var response = await httpClient.PostAsync("/api/odata/Employee?$expand=Tasks", new StringContent(data, Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);
            response.EnsureSuccessStatusCode();
            string result = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            #region
            // Status Code: Created
            //{
            //    "@odata.context": "http://localhost/api/odata/$metadata#Employee(Tasks())/$entity",
            //    "ID": "b40bcbad-0c66-4688-60c5-08dd895281b7",
            //    "FullName": "Mary Gordon",
            //    "FirstName": "Mary",
            //    "LastName": "Gordon",
            //    "Email": "CRUDTests@example.com",
            //    "TitleOfCourtesy": "Dr",
            //    "Tasks": [
            //        {
            //            "ID": "5ca52dbf-c812-4e24-b057-08dd895281c3",
            //            "Subject": "Bar",
            //            "Description": "Foo",
            //            "PercentCompleted": 0,
            //            "Status": "NotStarted",
            //            "Priority": "Low",
            //            "ActualWorkHours": 0,
            //            "EstimatedWorkHours": 0
            //        }
            //    ]
            //}
            #endregion
            #region  Validate response
            var objectKeys = Utils.ExtractGuids(result);

            string expectedResponse =
                $"{{\"@context\":\"http://localhost/api/odata/$metadata#Employee(Tasks())/$entity\"," +
                    $"\"ID\":\"{objectKeys[0]}\"," +
                    $"\"FullName\":\"Mary Gordon\"," +
                    $"\"FirstName\":\"Mary\"," +
                    $"\"LastName\":\"Gordon\"," +
                    $"\"MiddleName\":null," +
                    $"\"Birthday\":null," +
                    $"\"Email\":\"CRUDTests@example.com\"," +
                    $"\"Photo\":null," +
                    $"\"WebPageAddress\":null," +
                    $"\"NickName\":null," +
                    $"\"SpouseName\":null," +
                    $"\"Anniversary\":null," +
                    $"\"Notes\":null," +
                    $"\"TitleOfCourtesy\":\"Dr\"," +
                    $"\"Tasks\":[" +
                        $"{{" +
                            $"\"ID\":\"{objectKeys[1]}\"," +
                            $"\"DateCompleted\":null," +
                            $"\"Subject\":\"Bar\"," +
                            $"\"Description\":\"Foo\"," +
                            $"\"DueDate\":null," +
                            $"\"StartDate\":null," +
                            $"\"PercentCompleted\":0," +
                            $"\"Status\":\"NotStarted\"," +
                            $"\"Priority\":\"Low\"," + // see--> DemoTask.OnCreated { Priority = Priority.Normal; } Must be Normal, but without delta format we drop all values
                            $"\"ActualWorkHours\":0," +
                            $"\"EstimatedWorkHours\":0" +
                        $"}}" +
                    $"]" +
                $"}}";
            Assert.Equal(expectedResponse, result);
            #endregion
        }
        [Fact]
        public async Task DeepUpdate_CreateReferenceObject() {
            await DeepUpdate_CreateReferenceObject_Core();
        }
        async Task<List<string>> DeepUpdate_CreateReferenceObject_Core() {
            using var createEmployeeResponse = await httpClient.PostAsync("/api/odata/Employee", new StringContent(
                $@"{{
                    ""FirstName"": ""John"",
                    ""LastName"":""Jones"",
                    ""Email"":""CRUDTests@example.com""
                }}
                ", Encoding.UTF8, "application/json"));

            createEmployeeResponse.EnsureSuccessStatusCode();
            var result = await createEmployeeResponse.Content.ReadAsStringAsync();
            var newEmployee_ID = Utils.ExtractGuids(result)[0];

            using var createDepartmentResponse = await httpClient.PatchAsync(
                $"/api/odata/Employee/{newEmployee_ID}?$expand=Department($expand=Positions)",
                new StringContent(
                    $@"{{
                        ""Department"": {{
                            ""Title"": ""Logistics"",
                            ""DepartmentHead"": {{ ""ID"": ""{newEmployee_ID}"" }},
                            ""Positions@delta"": [
                                {{ ""Title"": ""Logistics Head"" }},
                                {{ ""Title"": ""Logistics Head Assistant"" }}
                            ]
                        }}
                    }}",
                Encoding.UTF8, "application/json"));

            createDepartmentResponse.EnsureSuccessStatusCode();
            result = await createDepartmentResponse.Content.ReadAsStringAsync();

            #region
            // Status Code: OK
            //{
            //    "@context": "http://localhost/api/odata/$metadata#Employee(Department(Positions()))/$entity",
            //    "ID": "4011fcb7-d8bf-4c95-2e7a-08dd8661082a",
            //    "FullName": "Mary Gordon",
            //    "FirstName": "Mary",
            //    "LastName": "Gordon",
            //    "Email": "CRUDTests@example.com",
            //    "TitleOfCourtesy": "Dr",
            //    "Department": {
            //        "ID": "2f4c580f-1d17-4be7-bda8-08dd8662f8d7",
            //        "Title": "Logistics",
            //        "Positions":[
            //              {
            //                "ID":"5561593f-5f55-4f92-9f74-08dd8cb0f592",
            //                "Title":"Logistics Head"
            //              },
            //              {
            //                "ID":"6afc0a3e-392f-483b-9f75-08dd8cb0f592",
            //                "Title":"Logistics Head Assistant"
            //              }
            //        ]
            //    }
            //}
            #endregion

            var objectKeys = Utils.ExtractGuids(result);
            #region  Validate response
            Assert.Equal(newEmployee_ID.ToString(), objectKeys[0]);

            string expectedResponse =
                $"{{\"@context\":\"http://localhost/api/odata/$metadata#Employee(Department(Positions()))/$entity\"," +
                    $"\"ID\":\"{objectKeys[0]}\"," +
                    $"\"FullName\":\"John Jones\"," +
                    $"\"FirstName\":\"John\"," +
                    $"\"LastName\":\"Jones\"," +
                    $"\"MiddleName\":null," +
                    $"\"Birthday\":null," +
                    $"\"Email\":\"CRUDTests@example.com\"," +
                    $"\"Photo\":null," +
                    $"\"WebPageAddress\":null," +
                    $"\"NickName\":null," +
                    $"\"SpouseName\":null," +
                    $"\"Anniversary\":null," +
                    $"\"Notes\":null," +
                    $"\"TitleOfCourtesy\":\"Dr\"," +
                    $"\"Department\":{{" +
                        $"\"ID\":\"{objectKeys[1]}\"," +
                        $"\"Title\":\"Logistics\"," +
                        $"\"Office\":null," +
                        $"\"Location\":null," +
                        $"\"Description\":null," +
                        $"\"Positions\":[" +
                            $"{{" +
                                $"\"ID\":\"{objectKeys[2]}\"," +
                                $"\"Title\":\"Logistics Head\"" +
                            $"}}," +
                            $"{{" +
                                $"\"ID\":\"{objectKeys[3]}\"," +
                                $"\"Title\":\"Logistics Head Assistant\"" +
                            $"}}" +
                        $"]" +
                    $"}}" +
                $"}}";
            Assert.Equal(expectedResponse, result);
            #endregion
            return objectKeys;
        }
        [Fact]
        public async Task DeepUpdate_ModifyReferenceObject() {
            var objectKeys = await DeepUpdate_CreateReferenceObject_Core();
            string existing_Employee_With_Department_ID = objectKeys[0];

            using var updateDepartmentResponse = await httpClient.PatchAsync(
                $"/api/odata/Employee/{existing_Employee_With_Department_ID}?$expand=Department",
                new StringContent(
                @"{
                    ""Department"": {
                        ""Title"": ""Logistics And Warehouse""
                    }
                }", Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

            updateDepartmentResponse.EnsureSuccessStatusCode();
            var result = await updateDepartmentResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            #region
            // Status Code: OK
            //{
            //    "@context": "http://localhost/api/odata/$metadata#Employee(Department())/$entity",
            //    "ID": "4011fcb7-d8bf-4c95-2e7a-08dd8661082a",
            //    "FullName": "John Jones",
            //    "FirstName": "John",
            //    "LastName": "Jones",
            //    "Email": "CRUDTests@example.com",
            //    "TitleOfCourtesy": "Dr",
            //    "Department": {
            //        "ID": "2f4c580f-1d17-4be7-bda8-08dd8662f8d7",
            //        "Title": "Logistics And Warehouse"
            //    }
            //}
            #endregion

            #region  Validate response
            string expectedResponse =
                $"{{\"@context\":\"http://localhost/api/odata/$metadata#Employee(Department())/$entity\"," +
                    $"\"ID\":\"{objectKeys[0]}\"," +
                    $"\"FullName\":\"John Jones\"," +
                    $"\"FirstName\":\"John\"," +
                    $"\"LastName\":\"Jones\"," +
                    $"\"MiddleName\":null," +
                    $"\"Birthday\":null," +
                    $"\"Email\":\"CRUDTests@example.com\"," +
                    $"\"Photo\":null," +
                    $"\"WebPageAddress\":null," +
                    $"\"NickName\":null," +
                    $"\"SpouseName\":null," +
                    $"\"Anniversary\":null," +
                    $"\"Notes\":null," +
                    $"\"TitleOfCourtesy\":\"Dr\"," +
                    $"\"Department\":{{" +
                        $"\"ID\":\"{objectKeys[1]}\"," +
                        $"\"Title\":\"Logistics And Warehouse\"," +
                        $"\"Office\":null," +
                        $"\"Location\":null," +
                        $"\"Description\":null" +
                    $"}}" +
                $"}}";
            Assert.Equal(expectedResponse, result);
            #endregion
        }
        [Fact]
        public async Task DeepUpdate_ReassignReferenceObject() {
            var objectKeys = await DeepUpdate_CreateReferenceObject_Core();
            string existing_Employee_With_Department_ID = objectKeys[0];

            using var newDepartmentLinkResponse = await httpClient.PatchAsync(
                $"/api/odata/Employee/{existing_Employee_With_Department_ID}?$expand=Department($expand=Positions)",
                new StringContent(
                $@"{{
                    ""Department"": {{
                        ""ID"": ""00000000-0000-0000-0000-000000000000"",
                        ""Title"": ""Sales"",
                        ""DepartmentHead"": {{ ""ID"": ""{existing_Employee_With_Department_ID}"" }},
                        ""Positions@delta"": [ {{ ""Title"": ""Sales Head Assistant"" }} ]
                    }}
                }}", Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

            newDepartmentLinkResponse.EnsureSuccessStatusCode();
            var result = await newDepartmentLinkResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            #region
            // Status Code: OK
            //{
            //    "@context": "http://localhost/api/odata/$metadata#Employee(Department(Positions()))/$entity",
            //    "ID": "4011fcb7-d8bf-4c95-2e7a-08dd8661082a",
            //    "FullName": "Mary Gordon",
            //    "FirstName": "Mary",
            //    "LastName": "Gordon",
            //    "Email": "CRUDTests@example.com",
            //    "TitleOfCourtesy": "Dr",
            //    "Department": {
            //        "ID": "4349F735-19B8-4DFE-8C18-88117B855EDD",
            //        "Title": "Sales",
            //        "Positions":[
            //              {
            //                "ID":"5561593f-5f55-4f92-9f74-08dd8cb0f592",
            //                "Title":"Sales Head Assistant"
            //              }
            //        ]
            //    }
            //}
            #endregion
            #region  Validate response
            var objectKeys_new = Utils.ExtractGuids(result);
            Assert.Equal(objectKeys_new[0], objectKeys[0]);
            Assert.NotEqual(objectKeys_new[1], objectKeys[1]);

            string expectedResponse =
                $"{{\"@context\":\"http://localhost/api/odata/$metadata#Employee(Department(Positions()))/$entity\"," +
                    $"\"ID\":\"{objectKeys_new[0]}\"," +
                    $"\"FullName\":\"John Jones\"," +
                    $"\"FirstName\":\"John\"," +
                    $"\"LastName\":\"Jones\"," +
                    $"\"MiddleName\":null," +
                    $"\"Birthday\":null," +
                    $"\"Email\":\"CRUDTests@example.com\"," +
                    $"\"Photo\":null," +
                    $"\"WebPageAddress\":null," +
                    $"\"NickName\":null," +
                    $"\"SpouseName\":null," +
                    $"\"Anniversary\":null," +
                    $"\"Notes\":null," +
                    $"\"TitleOfCourtesy\":\"Dr\"," +
                    $"\"Department\":{{" +
                        $"\"ID\":\"{objectKeys_new[1]}\"," +
                        $"\"Title\":\"Sales\"," +
                        $"\"Office\":null," +
                        $"\"Location\":null," +
                        $"\"Description\":null," +
                        $"\"Positions\":[" +
                            $"{{" +
                                $"\"ID\":\"{objectKeys_new[2]}\"," +
                                $"\"Title\":\"Sales Head Assistant\"" +
                            $"}}" +
                        $"]" +
                    $"}}" +
                $"}}";
            Assert.Equal(expectedResponse, result);
            #endregion
        }
        [Fact]
        public async Task Create_several_objects() {
            using var createTaskResponse = await httpClient.PatchAsync("/api/odata/DemoTask", new StringContent($@"
            {{
                ""@context"": ""http://localhost/api/odata/$metadata#DemoTask/$delta"",
                ""value"": [
                    {{ ""Subject"": ""Test task 1"" }},
                    {{ ""Subject"": ""Test task 2"" }}
                ]
            }}", Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

            createTaskResponse.EnsureSuccessStatusCode();
            var result = await createTaskResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            #region
            /*
            {"@context":"http://localhost/api/odata/$metadata#DemoTask/$delta",
                "value":[
                    {
                        "ID":"a82866a5-9793-4e4c-a17c-23eb0736c5e7",
                        "Subject":"Test task 1"
                    },
                    {
                        "ID":"59b76c2b-0539-48db-b7e5-49563db7a5e5",
                        "Subject":"Test task 2"
                    }
                ]
            }
            */
            #endregion
            #region  Validate response
            var taskKeys = Utils.ExtractGuids(result);
            string expectedResponse =
                $"{{\"@context\":\"http://localhost/api/odata/$metadata#DemoTask/$delta\"," +
                    $"\"value\":[" +
                        $"{{" +
                            $"\"ID\":\"{taskKeys[0]}\"," +
                            $"\"Subject\":\"Test task 1\"" +
                        $"}}," +
                        $"{{" +
                            $"\"ID\":\"{taskKeys[1]}\"," +
                            $"\"Subject\":\"Test task 2\"" +
                        $"}}" +
                    $"]" +
                $"}}";

            Assert.Equal(expectedResponse, result);
            #endregion
        }
        [Fact]
        public async Task Collection_modifications_with_delta() {
            using var createTaskResponse = await httpClient.PatchAsync("/api/odata/DemoTask", new StringContent($@"
            {{
                ""@context"": ""http://localhost/api/odata/$metadata#DemoTask/$delta"",
                ""value"": [
                    {{ ""Subject"": ""Test task 1"" }},
                    {{ ""Subject"": ""Test task 2"" }},
                    {{ ""Subject"": ""Test task 3"" }},
                    {{ ""Subject"": ""Test task 4"" }}
                ]
            }}", Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

            createTaskResponse.EnsureSuccessStatusCode();
            var result = await createTaskResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var taskKeys = Utils.ExtractGuids(result);

            using var createEmployeeResponse = await httpClient.PostAsync("/api/odata/Employee", new StringContent(
                $@"{{
                    ""FirstName"": ""Anita"",
                    ""LastName"":""Deville"",
                    ""Email"":""CRUDTests@example.com"",
                    ""Tasks@delta"": [
                        {{ ""ID"": ""{taskKeys[1]}"" }},
                        {{ ""ID"": ""{taskKeys[2]}"" }},
                        {{ ""ID"": ""{taskKeys[3]}"" }}
                    ]
                }}
                ", Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

            createEmployeeResponse.EnsureSuccessStatusCode();
            result = await createEmployeeResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            var employeeKey = Utils.ExtractGuids(result)[0];

            using var updateTasksResponse = await httpClient.PatchAsync(
                "/api/odata/Employee",
                new StringContent(
                    $@"{{
                        ""@context"": ""http://localhost/api/odata/$metadata#Employee/$delta"",
                        ""value"": [
                            {{
                                ""ID"": ""{employeeKey}"",
                                ""Tasks@delta"": [
                                    {{
                                        ""Subject"": ""New test task 100""
                                    }},
                                    {{
                                        ""ID"": ""{taskKeys[0]}""
                                    }},
                                    {{
                                        ""ID"": ""{taskKeys[1]}"",
                                        ""EstimatedWorkHours"": 15
                                    }},
                                    {{
                                        ""@removed"": {{ ""reason"": ""changed"" }},
                                        ""ID"": ""{taskKeys[2]}""
                                    }},
                                    {{
                                        ""@removed"": {{ ""reason"": ""deleted"" }},
                                        ""ID"": ""{taskKeys[3]}""
                                    }}
                                ]
                            }}
                        ]
                    }}", Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

            updateTasksResponse.EnsureSuccessStatusCode();
            result = await updateTasksResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            #region
            // Status Code: OK
            //{
            //    "@context": "http://localhost/api/odata/$metadata#Employee/$delta",
            //    "value": [
            //        {
            //            "ID": "4011fcb7-d8bf-4c95-2e7a-08dd8661082a",
            //            "Tasks@delta": [
            //                {
            //                    "ID": "13632d9d-9875-411a-dd93-08dd87f9079c",
            //                    "Subject": "New test task 100"
            //                },
            //                {
            //                    "ID": "3f5e43dd-7b1a-45b1-ce31-08dd7db05450"
            //                },
            //                {
            //                    "ID": "e847e4a3-d151-41e2-5a5f-08dd8723f585",
            //                    "EstimatedWorkHours": 15
            //                },
            //                {
            //                    "@removed": {
            //                      "reason": "changed"
            //                    },
            //                    "@id": "http://localhost/api/odata/DemoTask(70461055-5c93-48c8-6dfd-08dd87f5b018)",
            //                    "ID": "70461055-5c93-48c8-6dfd-08dd87f5b018"
            //                },
            //                {
            //                    "@removed": {
            //                      "reason": "deleted"
            //                    },
            //                    "@id": "http://localhost/api/odata/DemoTask(a74366e4-b391-4e01-3234-08dd87f7d4fa)",
            //                    "ID": "a74366e4-b391-4e01-3234-08dd87f7d4fa"
            //                }
            //            ]
            //        }
            //    ]
            //}
            #endregion

            #region  Validate response
            var newTaskKey = Utils.ExtractGuids(result)[1];
            string expectedResponse =
                $"{{\"@context\":\"http://localhost/api/odata/$metadata#Employee/$delta\"," +
                    $"\"value\":[" +
                        $"{{" +
                            $"\"ID\":\"{employeeKey}\"," +
                            $"\"Tasks@delta\":[" +
                                $"{{" +
                                    $"\"ID\":\"{newTaskKey}\"," +
                                    $"\"Subject\":\"New test task 100\"" +
                                $"}}," +
                                $"{{" +
                                    $"\"ID\":\"{taskKeys[0]}\"" +
                                $"}}," +
                                $"{{" +
                                    $"\"ID\":\"{taskKeys[1]}\"," +
                                    $"\"EstimatedWorkHours\":15" +
                                $"}}," +
                                $"{{" +
                                    $"\"@removed\":{{" +
                                    $"\"reason\":\"changed\"" +
                                    $"}}," +
                                    $"\"@id\":\"http://localhost/api/odata/DemoTask({taskKeys[2]})\"," +
                                    $"\"ID\":\"{taskKeys[2]}\"" +
                                $"}}," +
                                $"{{" +
                                    $"\"@removed\":{{" +
                                    $"\"reason\":\"deleted\"" +
                                    $"}}," +
                                    $"\"@id\":\"http://localhost/api/odata/DemoTask({taskKeys[3]})\"," +
                                    $"\"ID\":\"{taskKeys[3]}\"" +
                                $"}}" +
                            $"]" +
                        $"}}" +
                    $"]" +
                $"}}";
            Assert.Equal(expectedResponse, result);
            #endregion
        }

        public async ValueTask InitializeAsync() {
            ClearTestData();
            if(httpClient.DefaultRequestHeaders.Authorization == null) {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetAuthenticationTokenAsync());
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
                httpClient.DefaultRequestHeaders.Add("Prefer", "return=representation");
                httpClient.DefaultRequestHeaders.Add("OData-Version", "4.01");
            }
        }
        private async Task<string> GetAuthenticationTokenAsync() {
            StringContent httpContent = new(@"{ ""userName"": ""Sam"", ""password"": """" }", Encoding.UTF8, "application/json");
            var tokenResponse = await httpClient.PostAsync("/api/Authentication/Authenticate", httpContent);
            return await tokenResponse.Content.ReadAsStringAsync();
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
            var employeesToDelete = os.GetObjectsQuery<Employee>().Where(e => e.Email == "CRUDTests@example.com").ToArray();
            foreach(var toDelete in employeesToDelete) {
                os.Delete(toDelete);
            }
            os.CommitChanges();

            var positionsToDelete = os.GetObjectsQuery<Position>().Where(e => e.Title.StartsWith("Logistics Head")).ToArray();
            foreach(var toDelete in positionsToDelete) {
                os.Delete(toDelete);
            }
            os.CommitChanges();

            var departmentsToDelete = os.GetObjectsQuery<Department>().Where(e => e.Title.StartsWith("Logistics")).ToArray();
            foreach(var toDelete in departmentsToDelete) {
                os.Delete(toDelete);
            }
            os.CommitChanges();

            var tasksToDelete = os.GetObjectsQuery<DemoTask>().Where(e => e.Subject.StartsWith("Test task") || e.Subject.StartsWith("New test task")).ToArray();
            foreach(var toDelete in tasksToDelete) {
                os.Delete(toDelete);
            }
            os.CommitChanges();

            var usersToDelete = os.GetObjectsQuery<ApplicationUser>().Where(e => e.UserName == "New User").ToArray();
            foreach(var toDelete in usersToDelete) {
                os.Delete(toDelete);
            }
            os.CommitChanges();
        }
    }
}


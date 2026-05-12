using Xunit;
using MainDemo.WebAPI.TestInfrastructure;

namespace MainDemo.WebAPI.Tests;

public class LocalizationTests : BaseWebApiTest {
    const string ApiUrl = "/api/Localization/";

    public LocalizationTests(SharedTestHostHolder fixture) : base(fixture) { }


    [Fact]
    public async System.Threading.Tasks.Task GetClassCaption() {
        string url = "ClassCaption?classFullName=DevExpress.Persistent.BaseImpl.EF.PermissionPolicy.PermissionPolicyUser";

        string result = await SendRequestAsync("de-DE", url);
        Assert.Equal("Benutzer", result);

        result = await SendRequestAsync("en-US", url);
        Assert.Equal("Base User", result);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetMemberCaption() {
        string url = "MemberCaption?classFullName=MainDemo.Module.BusinessObjects.Employee&memberName=Birthday";

        string result = await SendRequestAsync("de-DE", url);
        Assert.Equal("Geburtstag", result);

        result = await SendRequestAsync("en-US", url);
        Assert.Equal("Birth Date", result);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetActionCaption() {
        string url = "ActionCaption?actionName=SetTaskAction";

        string result = await SendRequestAsync("de-DE", url);
        Assert.Equal("Setze für Aufgabe...", result);

        result = await SendRequestAsync("en-US", url);
        Assert.Equal("Set Task", result);
    }

    protected async System.Threading.Tasks.Task<string> SendRequestAsync(string locale, string url) {
        var request = new HttpRequestMessage(HttpMethod.Get, ApiUrl + url);
        request.Headers.Add("Accept-Language", locale);

        var httpResponse = await WebApiClient.SendAsync(request);
        if(!httpResponse.IsSuccessStatusCode) {
            throw new InvalidOperationException($"Caption request failed! Code {(int)httpResponse.StatusCode}, '{httpResponse.ReasonPhrase}', error: {await httpResponse.Content.ReadAsStringAsync() ?? "<null>"}");
        }
        var result = await httpResponse.Content.ReadAsStringAsync();
        return result;
    }
}

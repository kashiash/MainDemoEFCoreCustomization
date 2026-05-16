# Dodanie języka polskiego do MainDemo.Blazor.Server

Ten dokument pokazuje dokładnie, co zmieniłem w repo `MainDemo.NET.EFCore`, żeby dodać `pl-PL`.

## Zakres zmiany

Zmiana objęła:

1. listę języków w `appsettings.json`,
2. wybór kultury w `Startup.cs`,
3. ładowanie plików lokalizacyjnych DevExpress w `scripts.js`,
4. osadzenie `Model.DesignedDiffs.Localization.pl.xafml`,
5. testy HTTP dla lokalizacji,
6. ustabilizowanie testów raportów po dodaniu obsługi kultur.

## Plik 1. `CS/MainDemo.Blazor.Server/appsettings.json`

To jest dokładny fragment z repo:

```json
"DevExpress": {
  "ExpressApp": {
    "Languages": "pl-PL;en-US;de-DE",
    "ShowLanguageSwitcher": true,
    "Security": {
      "UrlSigningKey": "669BC10469B34252A2EF1BA1BAFEDEAF"
    },
    "ThemeSwitcher": {
      "DefaultItemName": "Office White",
      "ShowSizeModeSwitcher": true,
      "Groups": [
        {
          "IsFluent": true,
          "Caption": "DevExpress Fluent",
          "Items": [
            { "Caption": "Blue", "Color": "Blue" },
            { "Caption": "Cool Blue", "Color": "CoolBlue" },
            { "Caption": "Desert", "Color": "Desert" },
            { "Caption": "Mint", "Color": "Mint" },
            { "Caption": "Moss", "Color": "Moss" },
            { "Caption": "Orchid", "Color": "Orchid" },
            { "Caption": "Purple", "Color": "Purple" },
            { "Caption": "Rose", "Color": "Rose" },
            { "Caption": "Rust", "Color": "Rust" },
            { "Caption": "Steel", "Color": "Steel" },
            { "Caption": "Storm", "Color": "Storm" }
          ]
        },
        {
          "Caption": "DevExpress Classic",
          "Items": [
            {
              "Caption": "Blazing Berry",
              "Url": "_content/DevExpress.Blazor.Themes/blazing-berry.bs5.min.css",
              "Color": "#5c2d91"
            },
            {
              "Caption": "Blazing Dark",
              "Url": "_content/DevExpress.Blazor.Themes/blazing-dark.bs5.min.css",
              "Color": "#46444a"
            },
            {
              "Caption": "Office White",
              "Url": "_content/DevExpress.Blazor.Themes/office-white.bs5.min.css",
              "Color": "#fe7109"
            },
            {
              "Caption": "Purple",
              "Url": "_content/DevExpress.Blazor.Themes/purple.bs5.min.css",
              "Color": "#7989ff"
            }
          ]
        }
      ]
    }
  }
}
```

Najważniejsza zmiana to:

```json
"Languages": "pl-PL;en-US;de-DE"
```

W tym repo fallback został na `en-US`. Nie zmieniałem go na `pl-PL`, bo testy raportów CSV zakładają angielski separator i format daty.

## Plik 2. `CS/MainDemo.Blazor.Server/Startup.cs`

To jest dokładna konfiguracja z repo:

```csharp
using System.Globalization;
using Microsoft.AspNetCore.Localization;

// ...

services.Configure<RequestLocalizationOptions>(options => {
    var supportedCultures = new[] {
        new CultureInfo("pl-PL"),
        new CultureInfo("en-US"),
        new CultureInfo("de-DE")
    };

    options.DefaultRequestCulture = new RequestCulture("en-US");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders = new List<IRequestCultureProvider> {
        new QueryStringRequestCultureProvider(),
        new CookieRequestCultureProvider(),
        new AcceptLanguageHeaderRequestCultureProvider()
    };
});
```

To ustawienie robi trzy rzeczy:

1. rejestruje `pl-PL` jako wspieraną kulturę,
2. pozwala wybrać kulturę z query stringa, cookie i `Accept-Language`,
3. zostawia `en-US` jako domyślne zachowanie dla żądań bez kultury.

## Plik 3. `CS/MainDemo.Blazor.Server/wwwroot/js/scripts.js`

Tu siedzi dokładna obsługa lokalizacji reportingu i widgetów DevExpress:

```javascript
window.ReportingLocalization = {
    currentCulture: null,
    loadMergedMessages: function (baseUrl, overrideUrl) {
        return $.get(baseUrl).then(baseMessages => {
            return $.get(overrideUrl)
                .then(overrideMessages => $.extend(true, {}, baseMessages, overrideMessages))
                .catch(() => baseMessages);
        });
    },
    resolveLocalizationCulture: function (culture) {
        if (!culture) {
            return null;
        }

        const normalizedCulture = culture.toLowerCase();
        if (normalizedCulture.startsWith("de")) {
            return "de-DE";
        }
        if (normalizedCulture.startsWith("pl")) {
            return "pl-PL";
        }

        return null;
    },
    setCurrentCulture: function (culture) {
        window.ReportingLocalization.currentCulture = culture;
    },
    onCustomizeLocalization: function (_, e) {
        const currentCulture = window.ReportingLocalization.resolveLocalizationCulture(window.ReportingLocalization.currentCulture);
        if (currentCulture) {
            const analyticsMessages = window.ReportingLocalization.loadMergedMessages(
                "js/localization/dx-analytics-core." + currentCulture + ".json",
                "js/localization/overrides/dx-analytics-core." + currentCulture + ".json"
            );
            const reportingMessages = window.ReportingLocalization.loadMergedMessages(
                "js/localization/dx-reporting." + currentCulture + ".json",
                "js/localization/overrides/dx-reporting." + currentCulture + ".json"
            );
            const widgetMessages = window.ReportingLocalization.loadMergedMessages(
                "js/localization/" + currentCulture + ".json",
                "js/localization/overrides/" + currentCulture + ".json"
            );

            e.LoadMessages(analyticsMessages);
            e.LoadMessages(reportingMessages);
            widgetMessages.done(result => {
                e.WidgetLocalization.loadMessages(result);
            }).always(() => {
                e.WidgetLocalization.locale(currentCulture);
            });
        }
    }
};
```

Ta wersja:

1. rozpoznaje `pl-PL` i `de-DE`,
2. ładuje bazowe pliki DevExpress,
3. próbuje dołożyć lokalne override'y,
4. ustawia locale widgetów po stronie przeglądarki.

## Plik 4. `CS/MainDemo.Module/MainDemo.Module.csproj`

Polski model lokalizacji został osadzony jako `EmbeddedResource`:

```xml
<EmbeddedResource Include="Model.DesignedDiffs.Localization.de.xafml">
  <DependentUpon>Model.DesignedDiffs.xafml</DependentUpon>
</EmbeddedResource>
<EmbeddedResource Include="Model.DesignedDiffs.Localization.pl.xafml">
  <DependentUpon>Model.DesignedDiffs.xafml</DependentUpon>
</EmbeddedResource>
```

Bez tego sam plik `.xafml` leżałby w repo, ale XAF by go nie wczytał.

## Plik 5. `CS/MainDemo.WebAPI.Tests/LocalizationTests.cs`

To jest pełny test lokalizacji z repo:

```csharp
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

        result = await SendRequestAsync("pl-PL", url);
        Assert.Equal("Użytkownik", result);

        result = await SendRequestAsync("en-US", url);
        Assert.Equal("Base User", result);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetAdditionalPolishClassCaptions() {
        var result = await SendRequestAsync("pl-PL", "ClassCaption?classFullName=MainDemo.Module.BusinessObjects.Position");
        Assert.Equal("Stanowisko", result);

        result = await SendRequestAsync("pl-PL", "ClassCaption?classFullName=MainDemo.Module.BusinessObjects.Resume");
        Assert.Equal("CV", result);

        result = await SendRequestAsync("pl-PL", "ClassCaption?classFullName=DevExpress.Persistent.BaseImpl.EF.ReportDataV2");
        Assert.Equal("Raporty", result);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetMemberCaption() {
        string url = "MemberCaption?classFullName=MainDemo.Module.BusinessObjects.Employee&memberName=Birthday";

        string result = await SendRequestAsync("de-DE", url);
        Assert.Equal("Geburtstag", result);

        result = await SendRequestAsync("pl-PL", url);
        Assert.Equal("Data urodzenia", result);

        result = await SendRequestAsync("en-US", url);
        Assert.Equal("Birth Date", result);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetActionCaption() {
        string url = "ActionCaption?actionName=SetTaskAction";

        string result = await SendRequestAsync("de-DE", url);
        Assert.Equal("Setze für Aufgabe...", result);

        result = await SendRequestAsync("pl-PL", url);
        Assert.Equal("Ustaw zadanie...", result);

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
```

Ten test sprawdza realny endpoint:

```text
/api/Localization/
```

Czyli nie zgadujemy, czy lokalizacja działa. Sprawdzamy to przez HTTP.

## Plik 6. `CS/MainDemo.WebAPI.Tests/ReportTests.cs`

Po dodaniu kultur trzeba było ustabilizować testy raportów. To jest dokładny fragment z repo:

```csharp
using System.Globalization;

// ...

string currentData = DateTime.Now.ToString("d", CultureInfo.GetCultureInfo("en-US"));

// ...

private async System.Threading.Tasks.Task LoadReportAndCompare(string userName, string url, string expectedResult) {
    var request = new HttpRequestMessage(HttpMethod.Get, url);
    request.Headers.Add("Accept-Language", "en-US");
    var response = await WebApiClient.SendAsync(request);
    Assert.True(response.IsSuccessStatusCode, $"Request failed for {userName} @ {url} ");

    string loadedReport = await response.Content.ReadAsStringAsync();
    Assert.Equal(expectedResult, loadedReport);
}
```

To zabezpiecza test przed przypadkową zmianą separatora CSV i formatu daty po stronie hosta testowego.

## Pliki z tłumaczeniami JavaScript

Do repo doszły pliki:

```text
CS/MainDemo.Blazor.Server/wwwroot/js/localization/pl-PL.json
CS/MainDemo.Blazor.Server/wwwroot/js/localization/dx-analytics-core.pl-PL.json
CS/MainDemo.Blazor.Server/wwwroot/js/localization/dx-reporting.pl-PL.json
```

Serwer publikuje je automatycznie, bo projekt ma już:

```xml
<Content Include="wwwroot\**\*.*" CopyToPublishDirectory="PreserveNewest" />
```

## Jak uruchomić po zmianie

```powershell
cd C:\Users\Programista\source\repos\MainDemo.NET.EFCore\CS
dotnet build .\MainDemo.Blazor.Server\MainDemo.Blazor.Server.csproj -c Debug
dotnet test .\MainDemo.WebAPI.Tests\MainDemo.WebAPI.Tests.csproj -c Debug
dotnet run --project .\MainDemo.Blazor.Server\MainDemo.Blazor.Server.csproj -c Debug --no-build --urls http://localhost:5115
Invoke-WebRequest -Uri 'http://localhost:5115'
```

## Zmienione pliki

```text
Directory.Packages.props
CS/MainDemo.Blazor.Server/appsettings.json
CS/MainDemo.Blazor.Server/Startup.cs
CS/MainDemo.Blazor.Server/wwwroot/js/scripts.js
CS/MainDemo.Blazor.Server/wwwroot/js/localization/pl-PL.json
CS/MainDemo.Blazor.Server/wwwroot/js/localization/dx-analytics-core.pl-PL.json
CS/MainDemo.Blazor.Server/wwwroot/js/localization/dx-reporting.pl-PL.json
CS/MainDemo.Module/MainDemo.Module.csproj
CS/MainDemo.Module/Model.DesignedDiffs.Localization.pl.xafml
CS/MainDemo.WebAPI.Tests/LocalizationTests.cs
CS/MainDemo.WebAPI.Tests/ReportTests.cs
```

## Wynik

Po tej zmianie aplikacja:

1. pokazuje `pl-PL` na liście języków,
2. wybiera polski z `Accept-Language`,
3. ładuje polskie komunikaty DevExpress dla reportingu,
4. ma polskie captiony w modelu XAF,
5. przechodzi testy HTTP dla lokalizacji.

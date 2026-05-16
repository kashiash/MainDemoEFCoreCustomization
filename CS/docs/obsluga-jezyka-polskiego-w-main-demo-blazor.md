# Dodanie języka polskiego do MainDemo.Blazor.Server

Ten dokument pokazuje, jak dodałem język polski do projektu `MainDemo.NET.EFCore`. To zapis zmian wykonanych w tym repo.

## Co było celem

Chodziło o trzy rzeczy:

1. dodać `pl-PL` do dostępnych języków w aplikacji Blazor/XAF,
2. dołożyć polskie tłumaczenia dla UI, raportów i modelu XAF,
3. zrobić to tak, żeby projekt dało się zbudować i uruchomić jako samodzielne publiczne repo.

## Ważna decyzja projektowa

W artykule bazowym fallback był ustawiony na `pl-PL`. W tym repo zostawiłem fallback na `en-US`.

Powód jest praktyczny: po ustawieniu domyślnej kultury na polską eksport CSV w raportach zaczął używać separatora `;`, a istniejące testy i dotychczasowe zachowanie projektu zakładały format angielski z przecinkami. Polski działa więc normalnie przez:

- `Accept-Language`,
- query string,
- cookie,
- language switcher w XAF.

Fallback `en-US` nie blokuje polskiego. Po prostu nie zmienia domyślnego zachowania użytkownikom, którzy nie podali żadnej kultury.

## Krok 0 - uniezależnienie repo od globalnego `Directory.Packages.props`

### Problem

Po opublikowaniu repo okazało się, że projekt odziedziczał centralne wersje pakietów z katalogu nadrzędnego poza repo:

- `C:\Users\Programista\source\repos\Directory.Packages.props`

To znaczyło, że świeży klon publicznego repo nie był samowystarczalny.

### Zmiana

Dodałem do repo własny plik:

- `Directory.Packages.props`

Na końcu pliku dopisałem lokalne override'y dla wersji, które blokowały restore i build tego projektu, między innymi:

```xml
<PackageVersion Update="Swashbuckle.AspNetCore" Version="6.9.0" />
<PackageVersion Update="Swashbuckle.AspNetCore.Annotations" Version="6.9.0" />
<PackageVersion Update="System.IdentityModel.Tokens.Jwt" Version="8.18.0" />
<PackageVersion Update="SkiaSharp" Version="3.119.2" />
<PackageVersion Update="SkiaSharp.NativeAssets.Win32" Version="3.119.2" />
```

To nie jest sama lokalizacja, ale bez tego repo nie dawało się poprawnie zbudować w izolacji.

## Krok 1 - dodanie polskiego do listy języków

### Plik

- `CS\MainDemo.Blazor.Server\appsettings.json`

### Było

```json
"Languages": "en-US;de-DE",
"ShowLanguageSwitcher": true
```

### Jest

```json
"Languages": "pl-PL;en-US;de-DE",
"ShowLanguageSwitcher": true
```

### Efekt

Polski pojawia się na liście dostępnych języków w UI.

## Krok 2 - konfiguracja wyboru kultury w ASP.NET Core

### Plik

- `CS\MainDemo.Blazor.Server\Startup.cs`

### Dodałem namespace

```csharp
using System.Globalization;
using Microsoft.AspNetCore.Localization;
```

### Nowa konfiguracja

```csharp
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

### Co to daje

Kolejność wyboru języka jest teraz taka:

1. query string,
2. cookie,
3. nagłówek `Accept-Language`,
4. fallback `en-US`.

To oznacza, że jeśli przeglądarka użytkownika wysyła `pl-PL`, aplikacja wejdzie po polsku bez ręcznego przełączania.

## Krok 3 - polskie pliki lokalizacyjne dla raportów i widgetów DevExpress

### Plik sterujący

- `CS\MainDemo.Blazor.Server\wwwroot\js\scripts.js`

W tym projekcie lokalizacja raportów nie siedzi w `_Host.cshtml`, tylko właśnie tutaj.

### Było

Kod obsługiwał tylko `de-DE`:

```javascript
const currentCulture = window.ReportingLocalization.currentCulture;
if (currentCulture == "de-DE") {
    e.LoadMessages($.get("js/localization/dx-analytics-core." + currentCulture + ".json"));
    e.LoadMessages($.get("js/localization/dx-reporting." + currentCulture + ".json"));
    $.get("js/localization/" + currentCulture + ".json").done(result => {
        e.WidgetLocalization.loadMessages(result);
    }).always(() => {
        e.WidgetLocalization.locale(currentCulture);
    })
}
```

### Jest

Dodałem normalizację kultury i obsługę `pl-PL`:

```javascript
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
```

oraz:

```javascript
const currentCulture = window.ReportingLocalization.resolveLocalizationCulture(window.ReportingLocalization.currentCulture);
if (currentCulture) {
    e.LoadMessages($.get("js/localization/dx-analytics-core." + currentCulture + ".json"));
    e.LoadMessages($.get("js/localization/dx-reporting." + currentCulture + ".json"));
    $.get("js/localization/" + currentCulture + ".json").done(result => {
        e.WidgetLocalization.loadMessages(result);
    }).always(() => {
        e.WidgetLocalization.locale(currentCulture);
    });
}
```

### Nowe pliki

Do katalogu:

- `CS\MainDemo.Blazor.Server\wwwroot\js\localization\`

dodałem:

- `pl-PL.json`
- `dx-analytics-core.pl-PL.json`
- `dx-reporting.pl-PL.json`

### Skąd pochodzą pliki

- `pl-PL.json` został pobrany z paczki `devextreme`,
- `dx-analytics-core.pl-PL.json` i `dx-reporting.pl-PL.json` zostały skopiowane z działającego projektu Fleetman, który już używa tych tłumaczeń.

Jeżeli robisz to w innym projekcie i nie masz pod ręką gotowych plików z działającej aplikacji, najpierw zajrzyj na:

- `https://localization.devexpress.com/`

To jest oficjalne miejsce DevExpress do przeglądania i pobierania tłumaczeń. W praktyce warto tam sprawdzić, czy potrzebny pakiet językowy już istnieje, zanim zaczniesz wyciągać pliki z innych repozytoriów albo tłumaczyć coś ręcznie.

W praktyce nie chodzi tylko o samo wejście na stronę. Trzeba też faktycznie dograć pliki z pobranej paczki do projektu. Dla polskiego DevExpress daje to zwykle w katalogu podobnym do:

- `DevExpressLocalizedResources_2025.2_pl\json resources\`

I to właśnie te pliki trzeba skopiować do:

- `CS\MainDemo.Blazor.Server\wwwroot\js\localization\`

bo dopiero wtedy aplikacja ma z czego załadować tłumaczenia dla reportingu, dashboardów, rich editora czy arkuszy.

### Dlaczego nie trzeba było zmieniać `.csproj`

W tym projekcie `MainDemo.Blazor.Server.csproj` ma już:

```xml
<Content Include="wwwroot\**\*.*" CopyToPublishDirectory="PreserveNewest" />
```

więc nowe pliki z `wwwroot` są automatycznie brane do outputu i publikacji.

## Krok 4 - polska warstwa modelu XAF

### Nowy plik

- `CS\MainDemo.Module\Model.DesignedDiffs.Localization.pl.xafml`

### Rejestracja pliku

### Plik

- `CS\MainDemo.Module\MainDemo.Module.csproj`

### Dodałem

```xml
<EmbeddedResource Include="Model.DesignedDiffs.Localization.pl.xafml">
  <DependentUpon>Model.DesignedDiffs.xafml</DependentUpon>
</EmbeddedResource>
```

### Co jest w polskim modelu

Ten plik zawiera polskie captiony dla:

- akcji XAF, np. `Save`, `Refresh`, `SetTaskAction`,
- klas i pól biznesowych, np. `Employee`, `Birthday`, `Department`,
- widoków list,
- lokalizacji grupy `Paycheck`.

Przykład:

```xml
<Class Name="DevExpress.Persistent.BaseImpl.EF.PermissionPolicy.PermissionPolicyUser" Caption="Użytkownik">
  <OwnMembers>
    <Member Name="UserName" Caption="Nazwa użytkownika" />
    <Member Name="Roles" Caption="Role" />
  </OwnMembers>
</Class>
```

oraz:

```xml
<Action Id="SetTaskAction" Caption="Ustaw zadanie..." ToolTip="Ustaw zadanie..." />
```

## Krok 5 - testy lokalizacji dla `pl-PL`

### Plik

- `CS\MainDemo.WebAPI.Tests\LocalizationTests.cs`

### Dodałem asercje dla polskiego

#### Klasa

```csharp
result = await SendRequestAsync("pl-PL", url);
Assert.Equal("Użytkownik", result);
```

#### Pole

```csharp
result = await SendRequestAsync("pl-PL", url);
Assert.Equal("Data urodzenia", result);
```

#### Akcja

```csharp
result = await SendRequestAsync("pl-PL", url);
Assert.Equal("Ustaw zadanie...", result);
```

Dzięki temu testy nie sprawdzają już tylko `de-DE` i `en-US`, ale też konkretnie naszą nową obsługę polskiego.

## Krok 6 - ustabilizowanie testów raportów po dodaniu lokalizacji

### Plik

- `CS\MainDemo.WebAPI.Tests\ReportTests.cs`

### Problem

Po dodaniu obsługi kultur raport CSV zaczął zależeć od aktywnego języka:

- separator kolumn,
- format daty.

Same testy nie były wcześniej jawnie przypięte do konkretnej kultury.

### Zmiana

Dodałem:

```csharp
using System.Globalization;
```

oraz wymusiłem `en-US` w requestach raportowych:

```csharp
var request = new HttpRequestMessage(HttpMethod.Get, url);
request.Headers.Add("Accept-Language", "en-US");
var response = await WebApiClient.SendAsync(request);
```

i ujednoliciłem datę w oczekiwanym wyniku:

```csharp
string currentData = DateTime.Now.ToString("d", CultureInfo.GetCultureInfo("en-US"));
```

### Dlaczego to jest poprawne

Te testy mają sprawdzać zawartość raportu, a nie to, czy maszyna buildowa aktualnie pracuje na kulturze polskiej, angielskiej czy niemieckiej.

## Krok 7 - zgodność Swagger / OpenAPI z DevExpress Web API

### Problem

Po uniezależnieniu repo od zewnętrznego `Directory.Packages.props` wyszło na jaw, że projekt potrzebuje wersji Swaggera zgodnej z DevExpress Web API.

### Zmiana

W lokalnym `Directory.Packages.props` przypiąłem:

```xml
<PackageVersion Update="Swashbuckle.AspNetCore" Version="6.9.0" />
<PackageVersion Update="Swashbuckle.AspNetCore.Annotations" Version="6.9.0" />
```

To przywróciło zgodność z istniejącym kodem:

```csharp
using Microsoft.OpenApi.Models;
```

oraz z konfiguracją:

```csharp
c.AddSecurityRequirement(new OpenApiSecurityRequirement()
{
    {
        new OpenApiSecurityScheme() {
            Reference = new OpenApiReference() {
                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                Id = "JWT"
            }
        },
        new string[0]
    },
});
```

To nie było sednem lokalizacji, ale bez tego test host nie startował poprawnie.

## Krok 8 - jak uruchomić projekt po tych zmianach

### Build

```powershell
cd C:\Users\Programista\source\repos\MainDemo.NET.EFCore\CS
dotnet build .\MainDemo.Blazor.Server\MainDemo.Blazor.Server.csproj -c Debug
```

### Testy Web API

```powershell
dotnet test .\MainDemo.WebAPI.Tests\MainDemo.WebAPI.Tests.csproj -c Debug
```

### Lokalny start Blazora

```powershell
dotnet run --project .\MainDemo.Blazor.Server\MainDemo.Blazor.Server.csproj -c Debug --no-build --urls http://localhost:5115
```

### Smoke test

```powershell
Invoke-WebRequest -Uri 'http://localhost:5115'
```

Przy tej zmianie aplikacja wystartowała poprawnie i odpowiadała HTTP `200`.

## Lista zmienionych plików

- `Directory.Packages.props`
- `CS\MainDemo.Blazor.Server\appsettings.json`
- `CS\MainDemo.Blazor.Server\Startup.cs`
- `CS\MainDemo.Blazor.Server\wwwroot\js\scripts.js`
- `CS\MainDemo.Blazor.Server\wwwroot\js\localization\pl-PL.json`
- `CS\MainDemo.Blazor.Server\wwwroot\js\localization\dx-analytics-core.pl-PL.json`
- `CS\MainDemo.Blazor.Server\wwwroot\js\localization\dx-reporting.pl-PL.json`
- `CS\MainDemo.Module\MainDemo.Module.csproj`
- `CS\MainDemo.Module\Model.DesignedDiffs.Localization.pl.xafml`
- `CS\MainDemo.WebAPI.Tests\LocalizationTests.cs`
- `CS\MainDemo.WebAPI.Tests\ReportTests.cs`

## Efekt końcowy

Po tych zmianach projekt:

- ma język polski na liście języków,
- potrafi wybrać polski z `Accept-Language`,
- ładuje polskie komunikaty DevExpress w raportach,
- ma polskie captiony w modelu XAF,
- przechodzi build i testy,
- uruchamia się lokalnie jako publiczne repo bez zależności od zewnętrznego `Directory.Packages.props`.

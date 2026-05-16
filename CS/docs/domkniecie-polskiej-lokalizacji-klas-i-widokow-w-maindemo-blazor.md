# Domknięcie polskiej lokalizacji klas i widoków w MainDemo Blazor

Ten dokument pokazuje dokładnie, co dopisałem do `Model.DesignedDiffs.Localization.pl.xafml`, żeby interfejs po polsku nie mieszał polskich i angielskich nazw.

## Zmienione pliki

```text
CS/MainDemo.Module/Model.DesignedDiffs.Localization.pl.xafml
CS/MainDemo.WebAPI.Tests/LocalizationTests.cs
```

## Plik 1. `CS/MainDemo.Module/Model.DesignedDiffs.Localization.pl.xafml`

### Klasy frameworkowe widoczne w UI

To jest dokładny fragment z repo:

```xml
<Class Name="DevExpress.Persistent.BaseImpl.EF.ReportDataV2" Caption="Raporty">
  <OwnMembers>
    <Member Name="DataTypeCaption" Caption="Typ danych" />
    <Member Name="DisplayName" Caption="Nazwa wyświetlana" />
    <Member Name="IsInplaceReport" Caption="Raport osadzony" />
    <Member Name="IsPredefined" Caption="Tylko do odczytu" />
    <Member Name="ParametersObjectType" Caption="Typ danych parametrow" />
  </OwnMembers>
</Class>
<Class Name="DevExpress.Persistent.BaseImpl.EFCore.AuditTrail.AuditDataItemPersistent" Caption="Historia zmian">
  <OwnMembers>
    <Member Name="AuditedDefaultString" Caption="Obiekt audytowany" />
    <Member Name="AuditOperationType" Caption="Typ operacji" />
    <Member Name="ModifiedOn" Caption="Zmodyfikowano" />
    <Member Name="NewValue" Caption="Nowa wartość" />
    <Member Name="ObjectType" Caption="Typ obiektu" />
    <Member Name="OldValue" Caption="Stara wartość" />
    <Member Name="PropertyName" Caption="Nazwa pola" />
    <Member Name="UserName" Caption="Uzytkownik" />
  </OwnMembers>
</Class>
```

### Brakujące klasy biznesowe

To jest dokładny fragment z repo:

```xml
<Class Name="MainDemo.Module.BusinessObjects.PortfolioFileData" Caption="Portfolio">
  <OwnMembers>
    <Member Name="DocumentType" Caption="Typ dokumentu" />
    <Member Name="File" Caption="Plik" />
  </OwnMembers>
</Class>
<Class Name="MainDemo.Module.BusinessObjects.Position" Caption="Stanowisko">
  <OwnMembers>
    <Member Name="Departments" Caption="Działy" />
    <Member Name="Employees" Caption="Pracownicy" />
    <Member Name="Title" Caption="Nazwa" />
  </OwnMembers>
</Class>
<Class Name="MainDemo.Module.BusinessObjects.Resume" Caption="CV">
  <OwnMembers>
    <Member Name="Employee" Caption="Pracownik" />
    <Member Name="File" Caption="Plik" />
    <Member Name="Portfolio" Caption="Portfolio" />
  </OwnMembers>
</Class>
```

### Enumy i komunikaty

To jest dokładny fragment z repo:

```xml
<LocalizationGroup Name="Enums">
  <LocalizationGroup Name="DevExpress.Persistent.Base.SecurityPermissionPolicy" Value="Polityka uprawnień">
    <LocalizationItem Name="AllowAllByDefault" Value="Domyślnie zezwalaj na wszystko" />
    <LocalizationItem Name="DenyAllByDefault" Value="Domyślnie odmawiaj wszystkiego" />
    <LocalizationItem Name="ReadOnlyAllByDefault" Value="Domyślnie tylko odczyt" />
  </LocalizationGroup>
  <LocalizationGroup Name="DevExpress.Persistent.Base.SecurityPermissionState" Value="Nawigacja">
    <LocalizationItem Name="Allow" Value="Zezwól" />
    <LocalizationItem Name="Deny" Value="Odmów" />
  </LocalizationGroup>
  <LocalizationGroup Name="DevExpress.Persistent.BaseImpl.EFCore.AuditTrail.AuditOperationType">
    <LocalizationItem Name="AddedToCollection" Value="Dodano do kolekcji" />
    <LocalizationItem Name="CustomData" Value="Dane niestandardowe" />
    <LocalizationItem Name="InitialValueAssigned" Value="Przypisano wartość początkową" />
    <LocalizationItem Name="ObjectChanged" Value="Zmieniono obiekt" />
    <LocalizationItem Name="ObjectCreated" Value="Utworzono obiekt" />
    <LocalizationItem Name="ObjectDeleted" Value="Usunięto obiekt" />
    <LocalizationItem Name="RemovedFromCollection" Value="Usunięto z kolekcji" />
  </LocalizationGroup>
  <LocalizationGroup Name="MainDemo.Module.BusinessObjects.DocumentType">
    <LocalizationItem Name="Diagrams" Value="Diagramy" />
    <LocalizationItem Name="Documentation" Value="Dokumentacja" />
    <LocalizationItem Name="Screenshots" Value="Zrzuty ekranu" />
    <LocalizationItem Name="SourceCode" Value="Kod źródłowy" />
    <LocalizationItem Name="Tests" Value="Testy" />
    <LocalizationItem Name="Unknown" Value="Nieznany" />
  </LocalizationGroup>
  <LocalizationGroup Name="MainDemo.Module.BusinessObjects.Priority">
    <LocalizationItem Name="High" Value="Wysoki" />
    <LocalizationItem Name="Low" Value="Niski" />
    <LocalizationItem Name="Normal" Value="Normalny" />
  </LocalizationGroup>
  <LocalizationGroup Name="MainDemo.Module.BusinessObjects.TaskStatus">
    <LocalizationItem Name="NotStarted" Value="Nie rozpoczęto" />
    <LocalizationItem Name="InProgress" Value="W toku" />
    <LocalizationItem Name="WaitingForSomeoneElse" Value="Oczekuje na inną osobę" />
    <LocalizationItem Name="Deferred" Value="Odroczone" />
    <LocalizationItem Name="Completed" Value="Zakończone" />
  </LocalizationGroup>
</LocalizationGroup>
<LocalizationGroup Name="Messages">
  <LocalizationItem Name="CannotUploadFile" Value="Nie można przesłać pliku {0}, gdy trwa przesyłanie innego pliku." />
</LocalizationGroup>
```

### Nawigacja i widoki

To jest dokładny fragment z repo:

```xml
<NavigationItems>
  <Items>
    <Item Id="Default" Caption="Domyślne">
      <Items>
        <Item Id="Employee_ListView" Caption="Pracownicy" />
        <Item Id="ApplicationUser_ListView" Caption="Użytkownicy" />
        <Item Id="DemoTask_ListView" Caption="Zadania" />
        <Item Id="Department_ListView" Caption="Działy" />
        <Item Id="DocumentFileType_ListView" Caption="Typy dokumentów" />
        <Item Id="Event_ListView" Caption="Kalendarz" />
        <Item Id="Note" Caption="Notatka" />
        <Item Id="Paycheck_ListView" Caption="Wypłaty" />
        <Item Id="PermissionPolicyRole_ListView" Caption="Role" />
        <Item Id="Position_ListView" Caption="Stanowiska" />
        <Item Id="Resume_ListView" Caption="CV" />
      </Items>
    </Item>
    <Item Id="Reports" Caption="Raporty">
      <Items>
        <Item Id="ReportsV2" Caption="Raporty" />
      </Items>
    </Item>
  </Items>
</NavigationItems>
<Views>
  <ListView Id="ApplicationUser_ListView" Caption="Użytkownicy" />
  <ListView Id="AuditDataItemPersistent_ListView" Caption="Historia zmian" />
  <DetailView Id="AuthenticationStandardLogonParameters_DetailView_Demo" Caption="Logowanie">
    <Items>
      <StaticText Id="LogonText" Text="Wpisz nazwę użytkownika i hasło, aby kontynuować." />
      <StaticText Id="PasswordHint" Text="Ta aplikacja demonstracyjna nie wymaga hasła do logowania." />
    </Items>
  </DetailView>
```

I końcówka widoków list:

```xml
  <ListView Id="ReportDataV2_ListView" Caption="Raporty" />
  <ListView Id="Resume_ListView" Caption="CV" />
  <ListView Id="Resume_Portfolio_ListView">
    <Columns>
      <ColumnInfo Id="File" Caption="Plik" />
    </Columns>
  </ListView>
</Views>
```

## Plik 2. `CS/MainDemo.WebAPI.Tests/LocalizationTests.cs`

To jest pełny test z repo:

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

## Co ta zmiana domyka

Po tej zmianie po polsku są już:

1. klasy biznesowe `Position`, `Resume`, `PortfolioFileData`,
2. typy frameworkowe `ReportDataV2` i `AuditDataItemPersistent`,
3. enumy `DocumentType`, `Priority`, `TaskStatus`,
4. nawigacja, logowanie i listy widoczne w UI.

## Jak sprawdzić zmianę

```powershell
dotnet test CS/MainDemo.WebAPI.Tests/MainDemo.WebAPI.Tests.csproj -c Debug --filter LocalizationTests
```

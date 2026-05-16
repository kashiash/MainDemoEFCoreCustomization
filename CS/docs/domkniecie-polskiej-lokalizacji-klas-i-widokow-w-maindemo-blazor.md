# Domknięcie polskiej lokalizacji klas i widoków w MainDemo Blazor

Ten dokument jest kolejnym krokiem po artykule `obsluga-jezyka-polskiego-w-main-demo-blazor.md`. Tam opisałem włączenie `pl-PL`. Tutaj pokazuję, jak uzupełniłem model lokalizacji XAF, żeby interfejs po polsku nie mieszał polskich i angielskich nazw.

## Co było nie domknięte

Po pierwszym wdrożeniu polskiego w projekcie część modelu dalej wracała do angielskiego, bo `CS\MainDemo.Module\Model.DesignedDiffs.Localization.pl.xafml` obejmował tylko fragment obiektów i widoków.

Najbardziej widoczne braki:

- brak polskich nazw wyświetlanych dla `Position`, `Resume` i `PortfolioFileData`,
- brak polskich nazw wyświetlanych dla klas systemowych używanych w aplikacji, np. `Event`, `ReportDataV2` i `AuditDataItemPersistent`,
- brak lokalizacji enumów typu `Priority`, `TaskStatus` i `DocumentType`,
- brak polskich nazw wyświetlanych dla części nawigacji, list i logowania.

Efekt był taki, że użytkownik widział polski interfejs aplikacji, ale w kilku miejscach dalej pojawiały się angielskie nazwy z modelu bazowego.

## Zmienione pliki

- `CS\MainDemo.Module\Model.DesignedDiffs.Localization.pl.xafml`
- `CS\MainDemo.WebAPI.Tests\LocalizationTests.cs`

## Co dopisałem do modelu lokalizacji

### 1. Brakujące klasy biznesowe

Dodałem pełne polskie nazwy wyświetlane dla:

- `MainDemo.Module.BusinessObjects.Position` -> `Stanowisko`,
- `MainDemo.Module.BusinessObjects.Resume` -> `CV`,
- `MainDemo.Module.BusinessObjects.PortfolioFileData` -> `Portfolio`.

Razem z nimi doszły też tłumaczenia brakujących pól, na przykład:

- `Departments`, `Employees`, `Title` dla `Position`,
- `Employee`, `File`, `Portfolio` dla `Resume`,
- `DocumentType`, `File` dla `PortfolioFileData`.

### 2. Klasy systemowe XAF / DevExpress widoczne w UI

Polskie nazwy wyświetlane dostały także typy, które użytkownik realnie widzi w aplikacji:

- `DevExpress.Persistent.BaseImpl.EF.Event`,
- `DevExpress.Persistent.BaseImpl.EF.FileData`,
- `DevExpress.Persistent.BaseImpl.EF.ReportDataV2`,
- `DevExpress.Persistent.BaseImpl.EFCore.AuditTrail.AuditDataItemPersistent`,
- klasy uprawnień `PermissionPolicy*`,
- wyniki walidacji `ValidationResults`.

To jest ważne, bo sama lokalizacja klas biznesowych nie wystarcza. W XAF termin `Business Object` jest nazwą własną używaną dla takich klas, ale część ekranów składa się też z typów frameworkowych i jeśli ich nie obejmiesz modelem lokalizacji, interfejs dalej będzie częściowo po angielsku.

### 3. Enumy i komunikaty modelowe

Dodałem polskie wartości enumów:

- `DocumentType`,
- `Priority`,
- `TaskStatus`,
- `SecurityPermissionPolicy`,
- `SecurityPermissionState`,
- `AuditOperationType`.

Do tego doszły komunikaty modelowe, na przykład `CannotUploadFile`.

### 4. Nawigacja, listy, logowanie

Uzupełniłem także warstwę `Views` i `NavigationItems`, żeby polska lokalizacja była spójna poza samymi nazwami klas:

- `ApplicationUser_ListView` -> `Użytkownicy`,
- `PermissionPolicyRole_ListView` -> `Role`,
- `Position_ListView` -> `Stanowiska`,
- `Resume_ListView` -> `CV`,
- `ReportDataV2_ListView` -> `Raporty`,
- `AuditDataItemPersistent_ListView` -> `Historia zmian`,
- teksty logowania i nazwy grup layoutu w detail view.

## Test regresyjny

W `CS\MainDemo.WebAPI.Tests\LocalizationTests.cs` dopisałem test, który pilnuje polskich nazw wyświetlanych dla typów, które wcześniej nie były przykryte:

- `Position` -> `Stanowisko`,
- `Resume` -> `CV`,
- `ReportDataV2` -> `Raporty`.

To nie testuje całego pliku `.xafml`, ale daje szybki sygnał, jeśli ktoś przypadkiem usunie albo nadpisze kluczowe wpisy modelu lokalizacji.

Przykład z tego repo wygląda tak:

```csharp
public class LocalizationTests : BaseWebApiTest {
    const string ApiUrl = "/api/Localization/";

    [Fact]
    public async Task GetAdditionalPolishClassCaptions() {
        var result = await SendRequestAsync("pl-PL", "ClassCaption?classFullName=MainDemo.Module.BusinessObjects.Position");
        Assert.Equal("Stanowisko", result);

        result = await SendRequestAsync("pl-PL", "ClassCaption?classFullName=MainDemo.Module.BusinessObjects.Resume");
        Assert.Equal("CV", result);

        result = await SendRequestAsync("pl-PL", "ClassCaption?classFullName=DevExpress.Persistent.BaseImpl.EF.ReportDataV2");
        Assert.Equal("Raporty", result);
    }

    protected async Task<string> SendRequestAsync(string locale, string url) {
        var request = new HttpRequestMessage(HttpMethod.Get, ApiUrl + url);
        request.Headers.Add("Accept-Language", locale);

        var httpResponse = await WebApiClient.SendAsync(request);
        return await httpResponse.Content.ReadAsStringAsync();
    }
}
```

Tu nie ma żadnej magii po stronie Blazora. Test woła zwykły endpoint HTTP `api/Localization`, a aplikacja zwraca caption wyliczony z bieżącej lokalizacji i modelu XAF.

## Co to daje praktycznie

Po przełączeniu aplikacji na `pl-PL` użytkownik nie powinien już widzieć mieszanki:

- polskiej nawigacji,
- angielskich nazw list,
- angielskich typów raportów,
- i nieprzetłumaczonych enumów w formularzach.

To jest właśnie ten etap, który zwykle wychodzi dopiero po pierwszym normalnym użyciu systemu. Mechanizm języków działał już wcześniej, ale dopiero ta zmiana domyka polską warstwę modelu XAF.

## Co trzeba zrobić przy dodawaniu kolejnego języka

Jeżeli do działającej aplikacji dodajesz następny język, na przykład polski albo niemiecki, to warto przejść przez ten zestaw kroków:

1. Dodać nowy język do konfiguracji aplikacji.
2. Dodać ten język do `RequestLocalizationOptions`, żeby aplikacja umiała przełączyć kulturę żądania.
3. Dołożyć pliki lokalizacyjne DevExpress dla warstwy JavaScript, jeżeli dany język ma być widoczny także w komponentach klienckich.
4. Uzupełnić tłumaczenia własnych klas biznesowych i ich pól w modelu XAF.
5. Sprawdzić klasy frameworkowe widoczne w UI, na przykład raporty, role, audyt albo kalendarz, i także dodać im tłumaczenia.
6. Uzupełnić wartości enumów, nazwy widoków, pozycje nawigacji i teksty logowania.
7. Dodać przynajmniej jeden test regresyjny, który wysyła żądanie z nagłówkiem `Accept-Language` i sprawdza, czy aplikacja zwraca właściwe captiony.

Jeżeli zrobisz tylko punkty 1-4, to język będzie formalnie dodany, ale użytkownik nadal zobaczy w różnych miejscach mieszankę nowego języka i angielskiego.

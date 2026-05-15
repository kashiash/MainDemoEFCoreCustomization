# Domknięcie polskiej lokalizacji klas i widoków w MainDemo Blazor

Ten dokument jest kolejnym krokiem po artykule `obsluga-jezyka-polskiego-w-main-demo-blazor.md`. Tam został opisany mechanizm włączenia `pl-PL` w aplikacji. Tutaj domykam warstwę modelu XAF tak, żeby po przełączeniu języka na polski interfejs nie mieszał polskich nazw wyświetlanych z angielskimi nazwami klas, enumów i widoków.

## Co było nie domknięte

Po pierwszym wdrożeniu polskiego w projekcie część modelu dalej wracała do angielskiego, bo `CS\MainDemo.Module\Model.DesignedDiffs.Localization.pl.xafml` obejmował tylko fragment obiektów i widoków.

Najbardziej widoczne braki:

- brak polskich nazw wyświetlanych dla `Position`, `Resume` i `PortfolioFileData`,
- brak polskich nazw wyświetlanych dla klas systemowych używanych w aplikacji, np. `Event`, `ReportDataV2` i `AuditDataItemPersistent`,
- brak lokalizacji enumów typu `Priority`, `TaskStatus` i `DocumentType`,
- brak polskich nazw wyświetlanych dla części nawigacji, list i logowania.

Efekt był taki, że użytkownik widział polski shell aplikacji, ale w kilku miejscach dalej pojawiały się angielskie nazwy z modelu bazowego.

## Zmienione pliki

- `CS\MainDemo.Module\Model.DesignedDiffs.Localization.pl.xafml`
- `CS\MainDemo.WebAPI.Tests\LocalizationTests.cs`

## Co dopisałem do modelu lokalizacji

### 1. Brakujące klasy biznesowe

Dodałem pełne polskie nazwy wyświetlane dla:

- `MainDemo.Module.BusinessObjects.Position` -> `Stanowisko`,
- `MainDemo.Module.BusinessObjects.Resume` -> `CV`,
- `MainDemo.Module.BusinessObjects.PortfolioFileData` -> `Portfolio`.

Razem z nimi doszły też brakujące pola, np.:

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

To jest ważne, bo sama lokalizacja business objectów nie wystarcza. W XAF część ekranów składa się z typów frameworkowych i jeśli ich nie przykryjesz modelem językowym, UI dalej będzie częściowo po angielsku.

### 3. Enumy i komunikaty modelowe

Dodałem polskie wartości enumów:

- `DocumentType`,
- `Priority`,
- `TaskStatus`,
- `SecurityPermissionPolicy`,
- `SecurityPermissionState`,
- `AuditOperationType`.

Do tego doszły komunikaty modelowe, np. `CannotUploadFile`.

### 4. Nawigacja, listy, logowanie

Uzupełniłem także warstwę `Views` i `NavigationItems`, żeby polski był spójny poza samymi nazwami klas:

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

## Co to daje praktycznie

Po przełączeniu aplikacji na `pl-PL` użytkownik nie powinien już widzieć mieszanki:

- polskiej nawigacji,
- angielskich nazw list,
- angielskich typów raportów,
- i nieprzetłumaczonych enumów w formularzach.

To jest właśnie ten etap, który zwykle wychodzi dopiero po pierwszym prawdziwym klikaniu po systemie. Mechanizm języków działał już wcześniej, ale dopiero ta zmiana domyka polską warstwę modelu XAF.

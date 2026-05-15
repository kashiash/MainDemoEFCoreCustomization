# Obsługa skanów i podglądu PDF w MainDemo Blazor

Ten dokument opisuje rzeczywiste wdrożenie wzorca dokumentów i skanów w repo `MainDemo.NET.EFCore`. Nie jest to już ogólny HOWTO, tylko zapis tego, **co dokładnie zostało dodane do tej aplikacji**, jakie decyzje zostały podjęte oraz jakie błędy wyszły przy kompilacji i jak zostały poprawione.

Punktem wyjścia był dostarczony opis `HOWTO: dodanie obsługi skanów i podglądu PDF w aplikacji XAF (Blazor + EF Core)` zapisany w:

- [CS/docs/howto-dodanie-obslugi-skanow-i-podgladu-pdf-w-aplikacji-xaf.md](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/docs/howto-dodanie-obslugi-skanow-i-podgladu-pdf-w-aplikacji-xaf.md)

## Cel wdrożenia

W `MainDemo.NET.EFCore` były już dwa osobne motywy związane z plikami:

- `Resume.File` z wbudowanym `PdfViewerPropertyEditor`,
- `PortfolioFileData` jako prosty załącznik w CV.

Brakowało jednak jednego, spójnego wzorca do:

- przypinania **wielu dokumentów** do więcej niż jednego typu obiektu,
- rozróżniania dokumentów słownikiem typów,
- wgrywania wielu plików przez drag-drop,
- podglądu PDF i obrazów inline w Blazorze,
- pozostawienia WinForms bez ryzykownej ingerencji.

W tej iteracji mechanizm został wdrożony dla dwóch realnych obiektów z demówki:

- `Employee`,
- `DemoTask`.

To celowy kompromis. Wzorzec jest już ogólny, ale wdrożenie zostało ograniczone do dwóch istniejących obiektów, żeby nie rozlewać zmian po całym repo bez potrzeby.

## Co zostało dodane

### 1. Model danych w `MainDemo.Module`

Doszły nowe klasy:

- [CS/MainDemo.Module/BusinessObjects/DocumentFileType.cs](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Module/BusinessObjects/DocumentFileType.cs)
- [CS/MainDemo.Module/BusinessObjects/DocumentFile.cs](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Module/BusinessObjects/DocumentFile.cs)
- [CS/MainDemo.Module/BusinessObjects/IHasDocumentFiles.cs](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Module/BusinessObjects/IHasDocumentFiles.cs)
- [CS/MainDemo.Module/BusinessObjects/DocumentFilePreview.cs](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Module/BusinessObjects/DocumentFilePreview.cs)
- [CS/MainDemo.Module/BusinessObjects/DocumentUploadArea.cs](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Module/BusinessObjects/DocumentUploadArea.cs)
- [CS/MainDemo.Module/BusinessObjects/DocumentFileUploadParameters.cs](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Module/BusinessObjects/DocumentFileUploadParameters.cs)

Najważniejsze decyzje:

- `DocumentFileType` jest słownikiem typu dokumentu i ma `DefaultClassOptions`, bo ma być zarządzany z UI.
- `DocumentFile` jest osobną encją, a nie próbą rozszerzenia istniejącego `PortfolioFileData`.
- relacje do właścicieli są jawne: `Employee` i `DemoTask`,
- `DocumentFile` nie ma własnej pozycji nawigacyjnej, bo w tym wariancie ma żyć głównie jako kolekcja podrzędna,
- podgląd jest oddzielony od pola `File` przez właściwość `PreviewFile`, dzięki czemu nie trzeba podmieniać standardowego edytora `FileData` globalnie.

### 2. Powiązanie z obiektami biznesowymi

Zmienione zostały:

- [CS/MainDemo.Module/BusinessObjects/Employee.cs](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Module/BusinessObjects/Employee.cs)
- [CS/MainDemo.Module/BusinessObjects/DemoTask.cs](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Module/BusinessObjects/DemoTask.cs)

Oba typy:

- implementują `IHasDocumentFiles`,
- mają `[Aggregated] IList<DocumentFile> DocumentFiles`.

To daje dwa efekty:

- nested list view może być obsłużony jednym kontrolerem,
- usunięcie właściciela usuwa jego dokumenty razem z obiektem nadrzędnym.

### 3. `DbContext` i konfiguracja EF Core

Zmiany trafiły do:

- [CS/MainDemo.Module/BusinessObjects/MainDemoDbContext.cs](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Module/BusinessObjects/MainDemoDbContext.cs)

Doszły:

- `DbSet<DocumentFile> DocumentFiles`,
- `DbSet<DocumentFileType> DocumentFileTypes`,
- relacje `DocumentFile -> Employee`,
- relacje `DocumentFile -> DemoTask`,
- kolumna `UploadedAtUtc` mapowana jako `datetime2`.

W tym repo nie była potrzebna osobna migracja do samego builda i testów, bo aplikacja i tak działa w modelu aktualizacji schematu przy starcie. Gdyby celem było wdrożenie do środowiska, w którym migracje są utrzymywane ręcznie, trzeba byłoby dodać standardowe `dotnet ef migrations add`.

### 4. Seed typów dokumentów i danych demonstracyjnych

Rozszerzony został:

- [CS/MainDemo.Module/DatabaseUpdate/Updater.cs](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Module/DatabaseUpdate/Updater.cs)

Doszły dwie rzeczy:

- seed słownika `DocumentFileType`,
- przykładowe dokumenty przypięte do `Employee` i `DemoTask`.

Seed typów obejmuje:

- `INVOICE`,
- `CONTRACT`,
- `LETTER`,
- `ID_SCAN`,
- `PROTOCOL`,
- `OTHER`.

To ma dwie zalety:

- testy nie muszą ręcznie budować słownika od zera,
- po uruchomieniu demówki od razu widać, że mechanizm działa.

### 5. Model XAF i polska lokalizacja

Zmienione zostały:

- [CS/MainDemo.Module/Model.DesignedDiffs.xafml](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Module/Model.DesignedDiffs.xafml)
- [CS/MainDemo.Module/Model.DesignedDiffs.Localization.pl.xafml](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Module/Model.DesignedDiffs.Localization.pl.xafml)
- [CS/MainDemo.Blazor.Server/Model.xafml](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Blazor.Server/Model.xafml)

Dodane zostały:

- zakładka `Załączniki` w `Employee_DetailView`,
- sekcja `Załączniki` w `DemoTask_DetailView`,
- widoki `DocumentFileType`,
- popup `DocumentFileUploadParameters_DetailView`,
- Blazor-only `PreviewFile` w `DocumentFile_DetailView`.

Najważniejsze jest tu rozdzielenie odpowiedzialności:

- model modułowy opisuje obiekt i układ wspólny,
- model Blazor dokłada podgląd, który zależy od Blazorowego edytora.

To zmniejsza ryzyko, że WinForms zacznie wymagać edytora, którego nie ma.

## Część Blazor

### 1. Podgląd PDF i obrazów inline

Nowe pliki:

- [CS/MainDemo.Blazor.Server/Editors/Documents/DocumentPreviewPropertyEditor.cs](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Blazor.Server/Editors/Documents/DocumentPreviewPropertyEditor.cs)
- [CS/MainDemo.Blazor.Server/Editors/Documents/DocumentPreviewRenderer.razor](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Blazor.Server/Editors/Documents/DocumentPreviewRenderer.razor)
- [CS/MainDemo.Blazor.Server/wwwroot/js/file-download.js](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Blazor.Server/wwwroot/js/file-download.js)

Mechanizm działa tak:

- `PreviewFile` nie zapisuje nic do bazy,
- property editor renderuje własny komponent Razor,
- komponent zamienia `FileData` na `data:` URL,
- dla `pdf` renderowany jest `<object>`,
- dla obrazów renderowany jest `<img>`,
- dla pozostałych rozszerzeń użytkownik dostaje jasny komunikat i przycisk pobrania.

W tej wersji:

- **PDF i obrazy mają podgląd inline**,
- `DOCX` i `XLSX` są akceptowane na uploadzie, ale kończą się informacją „pobierz plik”.

To było świadome cięcie zakresu. Pełna konwersja Office -> PDF wymagałaby dołożenia kolejnej warstwy zależności i osobnej decyzji wdrożeniowej.

### 2. Upload wielu plików w popupie

Nowe pliki:

- [CS/MainDemo.Blazor.Server/Editors/Documents/DocumentUploadAreaPropertyEditor.cs](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Blazor.Server/Editors/Documents/DocumentUploadAreaPropertyEditor.cs)
- [CS/MainDemo.Blazor.Server/Editors/Documents/DocumentUploadAreaRenderer.razor](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Blazor.Server/Editors/Documents/DocumentUploadAreaRenderer.razor)
- [CS/MainDemo.Blazor.Server/Controllers/DocumentFileNestedListViewController.cs](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Blazor.Server/Controllers/DocumentFileNestedListViewController.cs)
- [CS/MainDemo.Blazor.Server/API/Documents/DocumentFileUploadController.cs](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Blazor.Server/API/Documents/DocumentFileUploadController.cs)

Przebieg jest następujący:

1. użytkownik otwiera nested list `DocumentFiles`,
2. klika `Dodaj pliki`,
3. controller tworzy non-persistent `DocumentFileUploadParameters`,
4. popup pokazuje:
   - typ dokumentu,
   - opis,
   - strefę drag-drop z `DxUpload`,
5. `DxUpload` wysyła pliki na `/api/document-files/upload`,
6. endpoint zapisuje każdy plik jako osobny rekord `DocumentFile`,
7. po zamknięciu popupu lista jest odświeżana.

Endpoint:

- wymaga autoryzacji przez `[Authorize]`,
- akceptuje wiele plików,
- obsługuje `Employee` i `DemoTask`,
- przypisuje typ `OTHER`, jeśli `typeId` nie został podany,
- waliduje whitelistę rozszerzeń.

## Testy

Doszedł nowy plik:

- [CS/MainDemo.WebAPI.Tests/DocumentFileUploadTests.cs](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.WebAPI.Tests/DocumentFileUploadTests.cs)

Testy sprawdzają dwa przypadki:

1. upload trzech plików do `Employee` przez prawdziwy endpoint `multipart/form-data`,
2. fallback do typu `OTHER`, gdy `typeId` nie został podany.

To ważne, bo test nie sprawdza tylko encji. Sprawdza cały przepływ:

- JWT auth,
- `HttpRequestMessage` z `MultipartFormDataContent`,
- zapis `FileData`,
- przypięcie do właściciela,
- odczyt z bazy po commit.

## Błędy, które wyszły przy wdrożeniu

To jest najważniejsza część tego dokumentu, bo pokazuje realne potknięcia w tej aplikacji.

### 1. Konflikt `EditorAliases`

Objaw:

- kompilator zgłosił niejednoznaczność między:
  - `MainDemo.Module.Editors.EditorAliases`
  - `DevExpress.ExpressApp.Editors.EditorAliases`

Przyczyna:

- w `DocumentFile.cs` jedna właściwość używała aliasu modułowego (`DocumentPreviewPropertyEditor`),
- druga właściwość używała wbudowanego aliasu DevExpress (`FileDataPropertyEditor`),
- oba typy miały tę samą nazwę klasy statycznej.

Poprawka:

- zostawiłem alias `using EditorAliases = MainDemo.Module.Editors.EditorAliases;`,
- dla pola `File` użyłem pełnej nazwy:

```csharp
[EditorAlias(DevExpress.ExpressApp.Editors.EditorAliases.FileDataPropertyEditor)]
```

To od razu zamknęło konflikt.

### 2. Zły typ event args w `DxUpload`

Objaw:

- kompilator nie znajdował `UploadFileEventArgs`.

Przyczyna:

- w tej wersji komponentów DevExpress event `FileUploaded` oczekuje `FileUploadEventArgs`, nie `UploadFileEventArgs`.

Poprawka:

- zmieniłem metodę renderera na:

```csharp
private void OnFileUploaded(FileUploadEventArgs args)
```

### 3. `AllowedFileExtensions` oczekiwało `List<string>`

Objaw:

- błąd konwersji z `string[]` na `List<string>`.

Przyczyna:

- `DxUpload` w tym buildzie ma silniej typowaną właściwość niż w wielu snippetach internetowych.

Poprawka:

- zmieniłem definicję z tablicy na:

```csharp
private static readonly List<string> AllowedExtensions =
    [".pdf", ".jpg", ".jpeg", ".png", ".gif", ".docx", ".xlsx"];
```

### 4. `CollectionSourceBase` nie miało `Owner`

Objaw:

- `View.CollectionSource.Owner` nie kompilowało się.

Przyczyna:

- w tym repo i w tej wersji API XAF właściciel nested listy jest dostępny przez `PropertyCollectionSource.MasterObject`, a nie przez `Owner`.

Poprawka:

- controller został przepisany na:

```csharp
if (View?.CollectionSource is PropertyCollectionSource propertyCollectionSource) {
    return propertyCollectionSource.MasterObject;
}
```

To jest ważny detal przy przenoszeniu wzorca między różnymi repo i wersjami XAF.

## Co zostało świadomie uproszczone

Nie wszystko z oryginalnego HOWTO zostało wdrożone jeden do jednego. Celowo uprościłem kilka rzeczy:

1. `Employee` i `DemoTask` są jedynymi właścicielami dokumentów w tej iteracji.
2. `DOCX` i `XLSX` są wspierane na uploadzie, ale nie mają jeszcze konwersji do PDF do podglądu inline.
3. Nie dodawałem jeszcze walidacji MIME po nagłówkach ani skanowania antywirusowego.
4. Nie przenosiłem istniejącego `Resume` / `PortfolioFileData` na nowy model, żeby nie mieszać dwóch osobnych mechanizmów w jednej zmianie.

To są rozsądne ograniczenia dla demonstracji i wzorca wdrożeniowego. Mechanizm już działa, testuje się i nadaje się do rozszerzeń.

## Komendy weryfikacyjne

Build:

```powershell
dotnet build CS\MainDemo.NET.EFCore.sln -c Debug
```

Testy:

```powershell
dotnet test CS\MainDemo.WebAPI.Tests\MainDemo.WebAPI.Tests.csproj -c Debug --filter "DocumentFileUploadTests|DynamicAppearanceRuleTests|LocalizationTests"
```

Obie komendy przechodzą. Zostały tylko ostrzeżenia `NU1903` dla `System.Security.Cryptography.Xml`.

## Minimalny zestaw do skopiowania do innego projektu

Jeżeli chcesz przenieść ten wzorzec do innego projektu XAF Blazor + EF Core, minimalny pakiet to:

1. `DocumentFileType`
2. `DocumentFile`
3. `IHasDocumentFiles`
4. kolekcje `DocumentFiles` na właścicielach
5. `DbSet` i relacje w `DbContext`
6. `DocumentFileUploadParameters`
7. `DocumentPreviewPropertyEditor`
8. `DocumentUploadAreaPropertyEditor`
9. `DocumentFileNestedListViewController`
10. `DocumentFileUploadController`
11. model XAF z zakładką `Załączniki`
12. lokalizacja podpisów

Jeżeli potrzebujesz tylko PDF preview bez uploadu wieloplikowego, wystarczy mniej: encja dokumentu, relacja do właściciela i edytor `PreviewFile`.

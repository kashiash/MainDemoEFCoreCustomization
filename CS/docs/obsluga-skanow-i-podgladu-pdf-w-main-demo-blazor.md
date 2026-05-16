# Obsługa skanów i podglądu PDF w MainDemo Blazor

Ten dokument pokazuje, jak dodać dokumenty, upload wielu plików i podgląd PDF do aplikacji XAF Blazor + EF Core.

Cel:

1. właściciel, na przykład `Employee`, ma zakładkę `Załączniki`,
2. użytkownik klika `Dodaj pliki`,
3. przeciąga wiele PDF-ów naraz,
4. każdy plik zapisuje się jako osobny rekord `DocumentFile`,
5. po otwarciu dokumentu PDF jest widoczny inline.

Opisuję tylko wariant wdrożony w tej aplikacji.

## Co trzeba dodać

Trzeba dodać cztery grupy elementów:

1. w modelu danych,
2. w `DbContext`,
3. w warstwie XAF i Blazor,
4. w modelu widoków.

Najpierw model danych. Potem baza. Na końcu UI i widoki.

### 1. Klasy danych

- `DocumentFileType`
  To słownik typów dokumentów, na przykład `Faktura`, `Umowa`, `Korespondencja`.

- `DocumentFile`
  To encja dokumentu. Przechowuje plik, typ, opis i datę dodania.

- `IHasDocumentFiles`
  To interfejs dla obiektów, które mają mieć zakładkę `Załączniki`.
  Dzięki temu jeden kontroler XAF obsługuje różne klasy właścicieli.

- `DocumentFiles` na właścicielu
  To kolekcja dokumentów na klasie takiej jak `Employee` albo `DemoTask`.

- `DocumentFileUploadParameters`
  To obiekt pomocniczy do popupu `Dodaj pliki`.
  Trzyma typ dokumentu, opis i identyfikator właściciela.

### 2. Rejestracja w bazie i `DbContext`

- `DbSet<DocumentFile>`
  To tabela dokumentów.

- `DbSet<DocumentFileType>`
  To tabela typów dokumentów.

- relacje `DocumentFile -> Employee` i `DocumentFile -> DemoTask`
  Te relacje wskazują właściciela dokumentu.

- mapowanie `UploadedAtUtc`
  Ustaw je jawnie.

### 3. Warstwa XAF i Blazor

- `DocumentFileNestedListViewController`
  Dodaje akcję `Dodaj pliki`.
  Otwiera popup i odświeża listę.
  Nie zapisuje plików sam.

  Robi cztery rzeczy:
  1. pokazać akcję na nested liście dokumentów,
  2. otworzyć popup,
  3. przekazać do popupu informację, kto jest właścicielem dokumentów,
  4. odświeżyć listę po zakończeniu uploadu.

- `DocumentUploadAreaRenderer`
  To komponent Blazor z `DxUpload`.
  Umożliwia przeciągnięcie wielu plików.

- `DocumentFileUploadController`
  To endpoint HTTP.
  Dla każdego pliku tworzy osobny rekord `DocumentFile`.

- `DocumentPreviewRenderer`
  To komponent podglądu.
  Dla PDF używa `<object>`.
  Dla obrazów używa `<img>`.

### 4. Model widoków

- wpis `DocumentFiles` do detail view właściciela
  Dzięki temu użytkownik widzi zakładkę `Załączniki`.

- `DocumentFile_DetailView` z `PreviewFile`
  Dzięki temu po otwarciu dokumentu widać podgląd.

## Krok 1. Słownik typów dokumentów

Plik:

- [DocumentFileType.cs](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Module/BusinessObjects/DocumentFileType.cs)

Kod:

```csharp
[DefaultClassOptions]
[ImageName("BO_Category")]
[XafDefaultProperty(nameof(Name))]
public class DocumentFileType : BaseObject {
    [RuleRequiredField]
    [RuleUniqueValue]
    [MaxLength(20)]
    public virtual string Code { get; set; }

    [RuleRequiredField]
    [MaxLength(100)]
    public virtual string Name { get; set; }

    [MaxLength(255)]
    public virtual string Description { get; set; }

    public virtual bool IsActive { get; set; } = true;
}
```

## Krok 2. Encja dokumentu

Plik:

- [DocumentFile.cs](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Module/BusinessObjects/DocumentFile.cs)

Kod:

```csharp
[ImageName("BO_FileAttachment")]
[XafDefaultProperty(nameof(DisplayName))]
public class DocumentFile : BaseObject {
    [RuleRequiredField]
    [EditorAlias(DevExpress.ExpressApp.Editors.EditorAliases.FileDataPropertyEditor)]
    public virtual FileData File { get; set; }

    public virtual DocumentFileType Type { get; set; }

    [MaxLength(500)]
    public virtual string Description { get; set; }

    public virtual DateTime UploadedAtUtc { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual DemoTask DemoTask { get; set; }

    [NotMapped]
    [EditorAlias(EditorAliases.DocumentPreviewPropertyEditor)]
    public virtual DocumentFilePreview PreviewFile => new(File);

    [NotMapped]
    public virtual string DisplayName => string.IsNullOrWhiteSpace(File?.FileName)
        ? Type?.Name ?? "Document"
        : File.FileName;

    public override void OnCreated() {
        base.OnCreated();
        UploadedAtUtc = DateTime.UtcNow;
    }
}
```

W tej aplikacji `DocumentFile` ma dwa pola właściciela:

1. `Employee`
2. `DemoTask`

To wystarcza w tym projekcie.

## Krok 3. Interfejs i kolekcja dokumentów na właścicielu

Plik:

- [IHasDocumentFiles.cs](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Module/BusinessObjects/IHasDocumentFiles.cs)

Kod:

```csharp
public interface IHasDocumentFiles {
    IList<DocumentFile> DocumentFiles { get; set; }
}
```

Na właścicielu:

```csharp
[Aggregated]
public virtual IList<DocumentFile> DocumentFiles { get; set; } = new ObservableCollection<DocumentFile>();
```

Tak jest w klasach:

1. [Employee.cs](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Module/BusinessObjects/Employee.cs)
2. [DemoTask.cs](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Module/BusinessObjects/DemoTask.cs)

## Krok 4. `DbContext`

Plik:

- [MainDemoDbContext.cs](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Module/BusinessObjects/MainDemoDbContext.cs)

Dodaj:

```csharp
public DbSet<DocumentFile> DocumentFiles { get; set; }
public DbSet<DocumentFileType> DocumentFileTypes { get; set; }
```

Relacje:

```csharp
modelBuilder.Entity<DocumentFile>()
    .HasOne(documentFile => documentFile.Employee)
    .WithMany(employee => employee.DocumentFiles)
    .OnDelete(DeleteBehavior.Cascade);

modelBuilder.Entity<DocumentFile>()
    .HasOne(documentFile => documentFile.DemoTask)
    .WithMany(task => task.DocumentFiles)
    .OnDelete(DeleteBehavior.Cascade);
```

## Krok 5. Seed typów dokumentów

Plik:

- [Updater.cs](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Module/DatabaseUpdate/Updater.cs)

Dodaj seed:

```csharp
private void EnsureDocumentFileTypes() {
    (string Code, string Name)[] documentTypes = [
        ("INVOICE", "Faktura"),
        ("CONTRACT", "Umowa"),
        ("LETTER", "Korespondencja"),
        ("ID_SCAN", "Skan dokumentu"),
        ("PROTOCOL", "Protokół"),
        ("OTHER", "Inne")
    ];

    foreach(var (code, name) in documentTypes) {
        if(ObjectSpace.FirstOrDefault<DocumentFileType>(item => item.Code == code) != null) {
            continue;
        }

        var documentType = ObjectSpace.CreateObject<DocumentFileType>();
        documentType.Code = code;
        documentType.Name = name;
    }

    ObjectSpace.CommitChanges();
}
```

## Krok 6. Popup parametrów uploadu

Plik:

- [DocumentFileUploadParameters.cs](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Module/BusinessObjects/DocumentFileUploadParameters.cs)

Kod:

```csharp
[DomainComponent]
public class DocumentFileUploadParameters {
    public virtual DocumentFileType Type { get; set; }
    public virtual string Description { get; set; }
    public virtual DocumentUploadArea UploadArea { get; set; } = new();

    [Browsable(false)]
    public virtual string OwnerObjectType { get; set; }

    [Browsable(false)]
    public virtual Guid OwnerObjectId { get; set; }
}
```

## Krok 7. Kontroler XAF z akcją `Dodaj pliki`

Ta klasa dodaje akcję `Dodaj pliki` i otwiera popup uploadu.

Plik:

- [DocumentFileNestedListViewController.cs](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Blazor.Server/Controllers/DocumentFileNestedListViewController.cs)

Kod:

```csharp
public class DocumentFileNestedListViewController : ObjectViewController<ListView, DocumentFile> {
    private readonly PopupWindowShowAction addFilesAction;

    public DocumentFileNestedListViewController() {
        TargetViewNesting = Nesting.Nested;

        addFilesAction = new PopupWindowShowAction(this, "AddDocumentFiles", PredefinedCategory.RecordEdit) {
            Caption = "Dodaj pliki",
            ImageName = "BO_FileAttachment",
            AcceptButtonCaption = "Zamknij"
        };

        addFilesAction.CustomizePopupWindowParams += AddFilesAction_CustomizePopupWindowParams;
        addFilesAction.Execute += AddFilesAction_Execute;
    }

    protected override void OnActivated() {
        base.OnActivated();
        addFilesAction.Active["HasOwner"] = GetOwner() is IHasDocumentFiles;
    }

    private void AddFilesAction_CustomizePopupWindowParams(object sender, CustomizePopupWindowParamsEventArgs e) {
        if(GetOwner() is not BaseObject owner) {
            throw new UserFriendlyException("Brak obiektu nadrzędnego dla załączników.");
        }

        var popupObjectSpace = Application.CreateObjectSpace(typeof(DocumentFileUploadParameters));
        var parameters = popupObjectSpace.CreateObject<DocumentFileUploadParameters>();
        parameters.OwnerObjectType = owner.GetType().Name;
        parameters.OwnerObjectId = owner.ID;
        parameters.Type = popupObjectSpace.FirstOrDefault<DocumentFileType>(item => item.Code == "OTHER");

        e.View = Application.CreateDetailView(popupObjectSpace, "DocumentFileUploadParameters_DetailView", true, parameters);
        e.DialogController.SaveOnAccept = false;
        e.Maximized = true;
    }

    private void AddFilesAction_Execute(object sender, PopupWindowShowActionExecuteEventArgs e) {
        View.ObjectSpace.Refresh();
        View.Refresh();
    }

    private object GetOwner() {
        if(View?.CollectionSource is PropertyCollectionSource propertyCollectionSource) {
            return propertyCollectionSource.MasterObject;
        }
        return null;
    }
}
```

Ten kontroler:

1. dodaje akcję `Dodaj pliki`,
2. otwiera popup,
3. przekazuje do popupu identyfikator właściciela,
4. po zamknięciu odświeża listę.

## Krok 8. Komponent Blazor do przeciągnięcia wielu plików

To jest element, który pozwala wrzucić 20 PDF-ów jednym przeciągnięciem.

Plik:

- [DocumentUploadAreaRenderer.razor](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Blazor.Server/Editors/Documents/DocumentUploadAreaRenderer.razor)

Najważniejszy fragment:

```razor
<DxUpload Name="files"
          UploadUrl="@uploadUrl"
          AllowMultiFileUpload="true"
          UploadMode="UploadMode.Instant"
          AllowedFileExtensions="@AllowedExtensions"
          MaxFileSize="100_000_000"
          ExternalDropZoneCssSelector=".upload-drop-zone"
          ExternalSelectButtonCssSelector=".upload-select-button"
          AdditionalParameters="@additionalParams"
          FileUploaded="OnFileUploaded" />
```

To właśnie `AllowMultiFileUpload="true"` i `UploadMode="Instant"` dają:

1. wiele plików naraz,
2. natychmiastowy upload po przeciągnięciu.

## Krok 9. Endpoint API zapisujący pliki

Plik:

- [DocumentFileUploadController.cs](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Blazor.Server/API/Documents/DocumentFileUploadController.cs)

Kod:

```csharp
[ApiController]
[Authorize]
[Route("api/document-files")]
public class DocumentFileUploadController : ControllerBase {
    private readonly INonSecuredObjectSpaceFactory objectSpaceFactory;

    public DocumentFileUploadController(INonSecuredObjectSpaceFactory objectSpaceFactory) {
        this.objectSpaceFactory = objectSpaceFactory;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(100_000_000)]
    public async Task<IActionResult> Upload(
        [FromForm] List<IFormFile> files,
        [FromForm] string ownerObjectType,
        [FromForm] Guid ownerObjectId,
        [FromForm] Guid? typeId,
        [FromForm] string description) {

        using IObjectSpace objectSpace = objectSpaceFactory.CreateNonSecuredObjectSpace(typeof(DocumentFile));
        DocumentFileType documentType = ResolveDocumentType(objectSpace, typeId);

        foreach(var formFile in files.Where(item => item.Length > 0)) {
            var documentFile = objectSpace.CreateObject<DocumentFile>();
            var fileData = objectSpace.CreateObject<FileData>();

            await using var stream = formFile.OpenReadStream();
            fileData.LoadFromStream(formFile.FileName, stream);

            documentFile.File = fileData;
            documentFile.Type = documentType;
            documentFile.Description = description;
            AddToOwnerDocuments(objectSpace, documentFile, ownerObjectType, ownerObjectId);
        }

        objectSpace.CommitChanges();
        return Ok();
    }
}
```

Ten endpoint:

1. odbiera wiele plików,
2. tworzy dla każdego osobny `DocumentFile`,
3. przypina go do właściciela,
4. robi jeden `CommitChanges()`.

## Krok 10. Podgląd PDF

PDF nie wymaga własnego silnika renderującego.

Plik:

- [DocumentPreviewRenderer.razor](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Blazor.Server/Editors/Documents/DocumentPreviewRenderer.razor)

Najważniejszy fragment:

```razor
@if (Extension == "pdf") {
    <object data="@ContentUrl" type="application/pdf" width="100%" height="800"></object>
}
```

To działa tak:

1. komponent przygotowuje URL do pliku,
2. `<object>` osadza go w widoku,
3. renderowanie PDF wykonuje standardowa przeglądarka PDF w przeglądarce użytkownika.

## Krok 11. Zakładka `Załączniki` na właścicielu

Do detail view właściciela musisz dodać `DocumentFiles`.

To jest zrobione w modelu XAF.

Najważniejszy efekt:

1. użytkownik widzi nested listę dokumentów,
2. kontroler `DocumentFileNestedListViewController` działa właśnie na tej liście.

## Krok 12. Detail view dokumentu

`DocumentFile_DetailView` ma pokazywać:

1. `File`
2. `Type`
3. `Description`
4. `PreviewFile`

To daje:

1. metadane dokumentu,
2. możliwość pobrania pliku,
3. podgląd PDF inline.

## Jak działa cały przepływ

Pełny przepływ jest taki:

1. użytkownik otwiera detail view `Employee`,
2. przechodzi do zakładki `Załączniki`,
3. klika `Dodaj pliki`,
4. XAF controller otwiera popup,
5. popup pokazuje `DxUpload`,
6. użytkownik przeciąga 20 PDF-ów,
7. `DxUpload` wysyła je do `/api/document-files/upload`,
8. endpoint tworzy 20 rekordów `DocumentFile`,
9. każdy rekord trafia do `owner.DocumentFiles`,
10. po zamknięciu popupu lista dokumentów się odświeża,
11. po otwarciu dokumentu detail view pokazuje PDF przez `<object>`.

## Jak to zostało dołożone do `Resume` pracownika

W tym repo istnieje już klasa:

- [Resume.cs](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Module/BusinessObjects/Resume.cs)

To jest klasa biznesowa CV pracownika.

Kod:

```csharp
public class Resume : BaseObject {
    [RuleRequiredField]
    public virtual Employee Employee { get; set; }

    [FileTypeFilter("pdf-only", "PDF file", "*.pdf")]
    public virtual FileData File { get; set; }

    [EditorAlias(EditorAliases.PdfViewerPropertyEditor)]
    public FileData ResumeView => File;
}
```

Ta klasa miała już:

1. powiązanie z `Employee`,
2. pojedynczy plik PDF,
3. podgląd PDF w detail view.

Brakowało dodawania wielu PDF-ów przez drag and drop na `Employee.Resumes`.

Dodałem to osobno. `Resume` i `DocumentFile` dalej mają różne role.

### 1. Popup dla uploadu CV

Plik:

- [ResumeUploadParameters.cs](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Module/BusinessObjects/ResumeUploadParameters.cs)

Kod:

```csharp
[DomainComponent]
public class ResumeUploadParameters {
    [EditorAlias(EditorAliases.ResumeUploadAreaPropertyEditor)]
    public DocumentUploadArea UploadArea { get; set; } = new();

    public Guid EmployeeId { get; set; }
}
```

Ten obiekt trzyma tylko `EmployeeId`.

### 2. Kontroler nested listy `Resumes`

Plik:

- [ResumeNestedListViewController.cs](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Blazor.Server/Controllers/ResumeNestedListViewController.cs)

Ten kontroler:

1. działa na nested liście `Resume`,
2. dodaje akcję `Dodaj CV`,
3. otwiera `ResumeUploadParameters_DetailView`,
4. po zamknięciu popupu odświeża listę.

Najważniejszy fragment:

```csharp
addResumesAction = new PopupWindowShowAction(this, "AddEmployeeResumes", PredefinedCategory.RecordEdit) {
    Caption = "Dodaj CV",
    ImageName = "BO_Resume",
    AcceptButtonCaption = "Zamknij"
};
```

### 3. Komponent uploadu PDF

Pliki:

- [ResumeUploadAreaPropertyEditor.cs](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Blazor.Server/Editors/Documents/ResumeUploadAreaPropertyEditor.cs)
- [ResumeUploadAreaRenderer.razor](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Blazor.Server/Editors/Documents/ResumeUploadAreaRenderer.razor)

Najważniejszy fragment:

```razor
<DxUpload Name="files"
          UploadUrl="@UploadUrl"
          AllowMultiFileUpload="true"
          UploadMode="UploadMode.Instant"
          AllowedFileExtensions="@AllowedExtensions"
          AdditionalParameters="@AdditionalParameters" />
```

I ustawienia:

```csharp
private const string UploadUrl = "/api/resumes/upload";
private static readonly List<string> AllowedExtensions = [".pdf"];
```

To daje:

1. przeciągnięcie wielu PDF-ów naraz,
2. natychmiastowy upload,
3. ograniczenie do PDF.

### 4. Endpoint zapisujący `Resume`

Plik:

- [ResumeUploadController.cs](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Blazor.Server/API/Documents/ResumeUploadController.cs)

Ten endpoint:

1. przyjmuje listę plików,
2. przyjmuje `employeeId`,
3. odrzuca rozszerzenia inne niż `.pdf`,
4. dla każdego pliku tworzy osobny rekord `Resume`,
5. ustawia `Resume.Employee`,
6. ustawia `Resume.File`,
7. zapisuje zmiany.

Najważniejszy fragment:

```csharp
var resume = objectSpace.CreateObject<Resume>();
var fileData = objectSpace.CreateObject<FileData>();

await using var stream = formFile.OpenReadStream();
fileData.LoadFromStream(formFile.FileName, stream);

resume.Employee = employee;
resume.File = fileData;
```

## Efekt w UI dla pracownika

Na `Employee_DetailView` przywróciłem zakładkę `CV`.

Efekt:

1. użytkownik otwiera pracownika,
2. przechodzi do zakładki `CV`,
3. widzi nested listę `Resumes`,
4. klika `Dodaj CV`,
5. przeciąga wiele PDF-ów,
6. każdy PDF zapisuje się jako osobny rekord `Resume`,
7. po otwarciu rekordu działa standardowy podgląd PDF z `ResumeView`.

## Komendy

Build:

```powershell
dotnet build CS\MainDemo.NET.EFCore.sln -c Debug
```

Testy:

```powershell
dotnet test CS\MainDemo.WebAPI.Tests\MainDemo.WebAPI.Tests.csproj -c Debug --filter "DocumentFileUploadTests|DynamicAppearanceRuleTests|LocalizationTests"
```

Start aplikacji:

```powershell
dotnet run --no-launch-profile --project CS/MainDemo.Blazor.Server/MainDemo.Blazor.Server.csproj -c Debug --urls http://127.0.0.1:5115
```

## Co sprawdzić ręcznie

1. czy `Employee` ma zakładkę `Załączniki`,
2. czy jest przycisk `Dodaj pliki`,
3. czy popup otwiera się poprawnie,
4. czy można przeciągnąć wiele PDF-ów naraz,
5. czy po zamknięciu popupu lista jest odświeżona,
6. czy każdy plik utworzył osobny rekord,
7. czy detail view dokumentu pokazuje PDF inline.

## Minimalny zestaw do skopiowania

Jeżeli ktoś ma dodać ten mechanizm do własnej aplikacji XAF Blazor + EF Core, potrzebuje dokładnie:

1. `DocumentFileType`
2. `DocumentFile`
3. `IHasDocumentFiles`
4. kolekcji `DocumentFiles` na właścicielach
5. `DbSet` i relacji w `DbContext`
6. `DocumentFileUploadParameters`
7. `DocumentFileNestedListViewController`
8. `DocumentUploadAreaRenderer`
9. `DocumentFileUploadController`
10. `DocumentPreviewRenderer`
11. wpisu `DocumentFiles` do detail view właściciela
12. `PreviewFile` w detail view dokumentu

To jest najkrótsza kompletna ścieżka do osiągnięcia celu.

## Instrukcja dla agenta AI

Wdróż pełną obsługę dokumentów w aplikacji XAF Blazor + EF Core zgodnie z tym dokumentem.

Zakres:

1. dodaj `DocumentFileType`,
2. dodaj `DocumentFile`,
3. dodaj `IHasDocumentFiles`,
4. dodaj kolekcję `DocumentFiles` na klasach właścicieli,
5. zarejestruj `DbSet` i relacje w `DbContext`,
6. dodaj `DocumentFileUploadParameters`,
7. dodaj `DocumentFileNestedListViewController`,
8. dodaj `DocumentUploadAreaRenderer` z `DxUpload`,
9. dodaj `DocumentFileUploadController`,
10. dodaj `DocumentPreviewRenderer`,
11. dodaj zakładkę `Załączniki` do detail view właściciela,
12. dodaj `PreviewFile` do `DocumentFile_DetailView`,
13. dodaj seed typów dokumentów,
14. dodaj migrację EF Core,
15. dodaj test integracyjny uploadu wielu plików,
16. skompiluj rozwiązanie,
17. uruchom testy,
18. uruchom aplikację i popraw błędy kompilacji, błędy uruchomieniowe oraz brakujące rejestracje.

Zasady:

1. użyj powiązania od właściciela przez `DocumentFiles`,
2. nie dodawaj wielu pól właściciela do `DocumentFile`,
3. dla PDF użyj standardowej przeglądarki PDF w przeglądarce przez `<object>`,
4. nie dodawaj wariantów pobocznych,
5. zostaw jedną działającą ścieżkę od uploadu do podglądu.

Warunek zakończenia:

1. użytkownik może wejść na detail view właściciela,
2. może otworzyć zakładkę `Załączniki`,
3. może kliknąć `Dodaj pliki`,
4. może przeciągnąć wiele PDF-ów naraz,
5. każdy plik zapisuje się jako osobny `DocumentFile`,
6. lista odświeża się po zamknięciu popupu,
7. detail view dokumentu pokazuje PDF inline,
8. build przechodzi,
9. testy przechodzą,
10. rozwiązanie uruchamia się bez błędów.

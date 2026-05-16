# Obsługa skanów i podglądu PDF w MainDemo Blazor

To jest najprostsza ścieżka wdrożenia pełnej obsługi dokumentów w aplikacji XAF Blazor + EF Core.

Cel:

1. właściciel, na przykład `Employee`, ma zakładkę `Załączniki`,
2. użytkownik klika `Dodaj pliki`,
3. przeciąga wiele PDF-ów naraz,
4. każdy plik zapisuje się jako osobny rekord `DocumentFile`,
5. po otwarciu dokumentu PDF jest widoczny inline.

Ten dokument pokazuje tylko ten wariant, który działa w tej aplikacji.

## Co dokładnie trzeba dodać

1. `DocumentFileType` jako słownik typów dokumentów,
2. `DocumentFile` jako encję dokumentu,
3. `IHasDocumentFiles` jako interfejs właściciela,
4. `DocumentFiles` jako kolekcję na właścicielu,
5. `DbSet` i relacje w `DbContext`,
6. `DocumentFileUploadParameters` do popupu,
7. `DocumentFileNestedListViewController` z akcją `Dodaj pliki`,
8. `DocumentUploadAreaRenderer` z `DxUpload`,
9. `DocumentFileUploadController` jako endpoint API,
10. `DocumentPreviewRenderer` do podglądu PDF,
11. wpis `DocumentFiles` do detail view właściciela,
12. `DocumentFile_DetailView` z `PreviewFile`.

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

W tej aplikacji `DocumentFile` ma dwa właścicielskie pola:

1. `Employee`
2. `DemoTask`

To wystarcza dla tego wdrożenia.

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

Tak jest w:

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

To jest klasa, która uruchamia cały proces.

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

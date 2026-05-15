# HOWTO: dodanie obsługi skanów i podglądu PDF w aplikacji XAF (Blazor + EF Core)

Tutorial dla programisty, który ma działającą aplikację **XAF Blazor + EF Core** i chce dodać:

- załączanie wielu plików (PDF, JPG, PNG, DOCX, XLSX) do dowolnego obiektu biznesowego (klient, faktura, pacjent, sprawa, …),
- słownik typów dokumentów (faktura, umowa, korespondencja, …),
- podgląd plików inline (PDF/obrazy/dokumenty),
- akcję „Dodaj pliki" z drag-drop i multi-select.

Wzorzec wzięty z produkcyjnego systemu HIS (zdrowie publiczne) — patrz `HIS_DOCUMENT_FILE_PATTERN.md` w tym katalogu po dokumentację samego wzorca. Ten plik to **krok-po-kroku przepis** do wklejenia w swoim projekcie.

## Założenia

- XAF v24.2+ albo v25.1+, platforma Blazor Server, ORM EF Core (testowane na 25.2).
- Masz już DbContext, klasę bazową (np. `BaseObject` z `DevExpress.Persistent.BaseImpl.EF`) i działający `BlazorApplication`.
- Twoje obiekty biznesowe mają `Guid` jako klucz główny.

W tutorialu nazwę projekty zgodnie z DataDrive (`DataDrive.Module`, `DataDrive.Blazor.Server`). Podmień na swoje.

---

## Krok 1 — Słownik typów dokumentów

`DataDrive.Module/BusinessObjects/Documents/DocumentFileType.cs`:

```csharp
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;

namespace DataDrive.Module.BusinessObjects;

[DefaultClassOptions]
[XafDefaultProperty(nameof(Name))]
[ImageName("BO_Category")]
public class DocumentFileType : OutlookInspiredBaseObject {
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

Polskie tłumaczenia (`Model.DesignedDiffs.Localization.pl.xafml`):

```xml
<Class Name="DataDrive.Module.BusinessObjects.DocumentFileType" Caption="Typ dokumentu">
  <OwnMembers>
    <Member Name="Code" Caption="Kod" />
    <Member Name="Name" Caption="Nazwa" />
    <Member Name="Description" Caption="Opis" />
    <Member Name="IsActive" Caption="Aktywny" />
  </OwnMembers>
</Class>
```

Seed w `DataDrive.Module/DatabaseUpdate/Updater.cs`, metoda `UpdateDatabaseAfterUpdateSchema()`:

```csharp
SeedDocumentTypes();

private void SeedDocumentTypes() {
    (string Code, string Name)[] types = [
        ("INVOICE",  "Faktura"),
        ("CONTRACT", "Umowa"),
        ("LETTER",   "Korespondencja"),
        ("ID_SCAN",  "Skan dowodu / dokumentu tożsamości"),
        ("PROTOCOL", "Protokół"),
        ("OTHER",    "Inne")
    ];
    foreach(var (code, name) in types) {
        var existing = ObjectSpace.FirstOrDefault<DocumentFileType>(t => t.Code == code);
        if(existing != null) continue;
        var type = ObjectSpace.CreateObject<DocumentFileType>();
        type.Code = code;
        type.Name = name;
    }
    ObjectSpace.CommitChanges();
}
```

## Krok 2 — Encja `DocumentFile`

`DataDrive.Module/BusinessObjects/Documents/DocumentFile.cs`:

```csharp
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;

namespace DataDrive.Module.BusinessObjects;

[XafDefaultProperty(nameof(DisplayName))]
[ImageName("BO_FileAttachment")]
public class DocumentFile : OutlookInspiredBaseObject {
    [EditorAlias(EditorAliases.FileDataPropertyEditor)]   // zamień na CustomFileDataPropertyEditor po Kroku 5
    public virtual FileData File { get; set; }

    public virtual Guid? FileId { get; set; }

    public virtual DocumentFileType Type { get; set; }
    public virtual Guid? TypeId { get; set; }

    [MaxLength(500)]
    public virtual string Description { get; set; }

    public virtual DateTime UploadedAt { get; set; }

    // --- powiązania polimorficzne (po jednym FK na typ rodzica) ---
    public virtual Customer Customer { get; set; }
    public virtual Guid? CustomerId { get; set; }

    public virtual Order Order { get; set; }
    public virtual Guid? OrderId { get; set; }

    // dodaj kolejne FK gdy potrzebujesz: Quote, CustomerEmployee, itd.

    [NotMapped]
    public string DisplayName => string.IsNullOrWhiteSpace(File?.FileName)
        ? (Type?.Name ?? "Dokument")
        : File.FileName;

    public override void OnCreated() {
        base.OnCreated();
        UploadedAt = DateTime.UtcNow;
    }
}
```

**Notatka:** XAF ma już `FileAttachmentBase` w `DevExpress.Persistent.BaseImpl`, ale nie umie polimorficznych FK. Własna klasa = pełna kontrola.

## Krok 3 — Interfejs i kolekcje na obiektach-rodzicach

`DataDrive.Module/BusinessObjects/Documents/IHasDocumentFiles.cs`:

```csharp
using System.Collections.ObjectModel;

namespace DataDrive.Module.BusinessObjects;

public interface IHasDocumentFiles {
    ObservableCollection<DocumentFile> DocumentFiles { get; set; }
}
```

Na `Customer.cs` dodaj:

```csharp
[InverseProperty(nameof(DocumentFile.Customer))]
[Aggregated]
public virtual ObservableCollection<DocumentFile> DocumentFiles { get; set; } = new();
```

I `Customer : OutlookInspiredBaseObject, …, IHasDocumentFiles`. Analogicznie dla `Order` i innych klas, które mają mieć skany.

**Po co interfejs:** jeden kontroler obsługuje wszystkie obiekty implementujące `IHasDocumentFiles` (Krok 7).

## Krok 4 — DbContext i migracja

W `OutlookInspiredDbContext.cs` dodaj DbSety:

```csharp
public DbSet<DocumentFile> DocumentFiles { get; set; }
public DbSet<DocumentFileType> DocumentFileTypes { get; set; }
```

W `OnModelCreating` (opcjonalnie — kaskady):

```csharp
modelBuilder.Entity<DocumentFile>()
    .HasOne(d => d.Customer).WithMany(c => c.DocumentFiles)
    .HasForeignKey(d => d.CustomerId).OnDelete(DeleteBehavior.Cascade);
modelBuilder.Entity<DocumentFile>()
    .HasOne(d => d.Order).WithMany() // jeśli Order też dostaje IHasDocumentFiles
    .HasForeignKey(d => d.OrderId).OnDelete(DeleteBehavior.SetNull);
modelBuilder.Entity<DocumentFile>().Property(d => d.UploadedAt)
    .HasColumnType("timestamp without time zone");
```

Migracja:

```powershell
dotnet ef migrations add AddDocumentFiles `
    --project DataDrive.Module/DataDrive.Module.csproj `
    --startup-project DataDrive.Blazor.Server/DataDrive.Blazor.Server.csproj
```

Zweryfikuj wygenerowany plik i wykonaj `dotnet ef database update`.

## Krok 5 — Custom property editor dla `FileData` (podgląd)

To jest **klucz**: zamiast standardowego XAF-owego edytora (który tylko pokazuje przycisk download), własny renderer pokazuje PDF/obrazek inline.

### 5.1 Editor (C#)

`DataDrive.Blazor.Server/Editors/FileDataEditor/CustomFileDataPropertyEditor.cs`:

```csharp
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Blazor.Editors.Adapters;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;

namespace DataDrive.Blazor.Server.Editors;

public static class CustomEditorAliases {
    public const string CustomFileDataPropertyEditor = nameof(CustomFileDataPropertyEditor);
}

[PropertyEditor(typeof(IFileData), CustomEditorAliases.CustomFileDataPropertyEditor, false)]
public class CustomFileDataPropertyEditor : BlazorPropertyEditorBase {
    public CustomFileDataPropertyEditor(Type objectType, IModelMemberViewItem model)
        : base(objectType, model) { }

    protected override IComponentAdapter CreateComponentAdapter()
        => new FileDataAdapter(new FileDataModel());
}

public class FileDataModel : ComponentModelBase {
    public IFileData FileData {
        get => GetPropertyValue<IFileData>();
        set => SetPropertyValue(value);
    }
    public bool ReadOnly {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }
}

public class FileDataAdapter : ComponentAdapterBase<FileDataModel> {
    public FileDataAdapter(FileDataModel model) : base(model) { }
    public override Type ComponentType => typeof(FileDataRenderer);
}
```

### 5.2 Renderer (Razor)

`DataDrive.Blazor.Server/Editors/FileDataEditor/FileDataRenderer.razor`:

```razor
@using DevExpress.Persistent.Base
@inject IJSRuntime JSRuntime

@if (string.IsNullOrEmpty(content)) {
    <div class="text-muted">— brak pliku —</div>
}
else {
    <div class="d-flex gap-2 mb-2">
        <button class="btn btn-sm btn-outline-primary" @onclick="Download">
            Pobierz @fileName
        </button>
    </div>

    @if (extension == "pdf") {
        <object data="@content" type="application/pdf" width="100%" height="800"></object>
    }
    else if (extension is "jpg" or "jpeg" or "png" or "gif") {
        <img src="@content" style="max-width:100%; max-height:800px;" />
    }
    else if (extension == "html") {
        <iframe src="@content" style="width:100%; height:800px; border:0;"></iframe>
    }
    else {
        <div class="alert alert-info">
            Podgląd niedostępny dla rozszerzenia <code>.@extension</code>. Pobierz plik.
        </div>
    }
}

@code {
    [Parameter] public FileDataModel Model { get; set; } = null!;

    private string content = "";
    private string rawBase64 = "";
    private string fileName = "";
    private string extension = "";

    protected override void OnParametersSet() {
        var fd = Model.FileData;
        fileName = fd?.FileName ?? "";
        extension = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();

        if(fd == null || fd.Size == 0) {
            content = "";
            return;
        }

        using var ms = new MemoryStream();
        fd.SaveToStream(ms);
        rawBase64 = Convert.ToBase64String(ms.ToArray());
        string mime = extension switch {
            "pdf" => "application/pdf",
            "jpg" or "jpeg" => "image/jpeg",
            "png" => "image/png",
            "gif" => "image/gif",
            "html" => "text/html",
            _ => "application/octet-stream"
        };
        content = $"data:{mime};base64,{rawBase64}";
    }

    private async Task Download() {
        await JSRuntime.InvokeVoidAsync("downloadFileFromBase64", fileName, rawBase64);
    }
}
```

### 5.3 JS dla pobierania

`DataDrive.Blazor.Server/wwwroot/js/file-download.js`:

```javascript
window.downloadFileFromBase64 = (fileName, base64) => {
    const link = document.createElement('a');
    link.download = fileName;
    link.href = 'data:application/octet-stream;base64,' + base64;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};
```

Wpięcie skryptu w `_Host.cshtml` lub `App.razor`:

```html
<script src="js/file-download.js"></script>
```

### 5.4 Podpięcie edytora na polu `File`

W `DocumentFile.cs` zamień:

```csharp
[EditorAlias(EditorAliases.FileDataPropertyEditor)]
```

na

```csharp
[EditorAlias(CustomEditorAliases.CustomFileDataPropertyEditor)]
```

(import `using DataDrive.Blazor.Server.Editors;` — albo wynieś stałe `CustomEditorAliases` do modułu, jeśli nie chcesz reference Blazor → Module).

**Rozszerzenie:** żeby pokazywać DOCX/XLSX, dodaj w rendererze konwersję przez `DevExpress.XtraRichEdit.RichEditDocumentServer.ExportToPdf()` / `DevExpress.Spreadsheet.Workbook.ExportToPdf()` i traktuj wynik jak `pdf`. To wymaga referencji `DevExpress.Docs.v25.X.dll`.

## Krok 6 — Non-persistent klasa do popupu uploadu

`DataDrive.Module/Features/Documents/DocumentFileUploadParameters.cs`:

```csharp
using DevExpress.ExpressApp.DC;

namespace DataDrive.Module.Features.Documents;

[DomainComponent]
public class DocumentFileUploadParameters {
    public DocumentFileType Type { get; set; }
    public string Description { get; set; }

    [Browsable(false)]
    public string OwnerObjectType { get; set; }
    [Browsable(false)]
    public Guid OwnerObjectId { get; set; }
}
```

Zarejestruj non-persistent w `Module.cs`:

```csharp
public override void Setup(XafApplication application) {
    base.Setup(application);
    application.SetupComplete += (s, e) => {
        application.ObjectSpaceProviders.Add(
            new NonPersistentObjectSpaceProvider(application.TypesInfo, null));
    };
}
```

(albo `AddNonPersistent(typeof(DocumentFileUploadParameters))` w nowszych XAF).

## Krok 7 — Kontroler z akcją „Dodaj pliki"

`DataDrive.Blazor.Server/Features/Documents/DocumentFileNestedListViewController.cs`:

```csharp
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using DevExpress.Persistent.Base;

namespace DataDrive.Blazor.Server.Features.Documents;

public class DocumentFileNestedListViewController : ObjectViewController<ListView, DocumentFile> {
    private readonly PopupWindowShowAction addFilesAction;

    public DocumentFileNestedListViewController() {
        TargetViewNesting = Nesting.Nested;
        addFilesAction = new PopupWindowShowAction(this, "AddDocumentFiles", PredefinedCategory.RecordEdit) {
            Caption = "Dodaj pliki",
            ImageName = "BO_FileAttachment",
            AcceptButtonCaption = "Zamknij"
        };
        addFilesAction.CustomizePopupWindowParams += OnCustomizePopup;
        addFilesAction.Execute += OnExecute;
    }

    protected override void OnActivated() {
        base.OnActivated();
        addFilesAction.Active["HasOwner"] = Frame.Template != null
            && View.CollectionSource.Owner is IHasDocumentFiles;
    }

    private void OnCustomizePopup(object sender, CustomizePopupWindowParamsEventArgs e) {
        var owner = View.CollectionSource.Owner;
        if(owner == null) {
            throw new UserFriendlyException("Brak obiektu nadrzędnego.");
        }

        IObjectSpace os = Application.CreateObjectSpace(typeof(DocumentFileUploadParameters));
        var parameters = os.CreateObject<DocumentFileUploadParameters>();
        parameters.OwnerObjectType = owner.GetType().Name;
        parameters.OwnerObjectId = (Guid)Application.GetKeyValue(owner);

        e.View = Application.CreateDetailView(os, parameters);
        e.DialogController.SaveOnAccept = false;
    }

    private void OnExecute(object sender, PopupWindowShowActionExecuteEventArgs e) {
        View.ObjectSpace.Refresh();   // odśwież listę po uploadach (które poszły bezpośrednio API)
    }
}
```

Akcja pojawi się na każdym nested list view typu `DocumentFile_ListView` osadzonym w detail view rodzica, który implementuje `IHasDocumentFiles`.

## Krok 8 — Komponent uploadu (multi-select + drag-drop)

`DataDrive.Blazor.Server/Editors/UploadDocumentFileEditor/UploadDocumentFileEditorRenderer.razor`:

```razor
@using DevExpress.Blazor
@inject IJSRuntime JSRuntime

<div class="d-flex flex-column gap-2">
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

    <div class="upload-drop-zone p-4 text-center text-muted"
         style="border:2px dashed #ccc; border-radius:8px;">
        Przeciągnij pliki tutaj
    </div>
    <button class="upload-select-button btn btn-primary">Wybierz pliki</button>

    @if (uploadedCount > 0) {
        <div class="alert alert-success">Wgrano plików: @uploadedCount</div>
    }
</div>

@code {
    [Parameter] public UploadDocumentFileModel Model { get; set; } = null!;

    private static readonly string[] AllowedExtensions = [".pdf",".jpg",".jpeg",".png",".docx",".xlsx"];
    private readonly Dictionary<string, string> additionalParams = new();
    private string uploadUrl = "/api/document-files/upload";
    private int uploadedCount;

    protected override void OnParametersSet() {
        additionalParams["ownerObjectType"] = Model.OwnerObjectType;
        additionalParams["ownerObjectId"]   = Model.OwnerObjectId.ToString();
        additionalParams["typeId"]          = Model.TypeId?.ToString() ?? "";
        additionalParams["description"]     = Model.Description ?? "";
    }

    private void OnFileUploaded(UploadFileEventArgs e) {
        uploadedCount++;
        StateHasChanged();
    }
}
```

Property editor + adapter dla `DocumentFileUploadParameters` analogicznie do Kroku 5 — pomijam dla zwięzłości (skopiuj `CustomFileDataPropertyEditor` i podmień model). W praktyce możesz też zostawić standardowy formularz XAF z polami Type/Description, a sam `DxUpload` dodać jako kolejny `ViewItem` w widoku.

## Krok 9 — Endpoint API uploadu

`DataDrive.Blazor.Server/Controllers/DocumentFileUploadController.cs`:

```csharp
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;
using DataDrive.Module.BusinessObjects;
using Microsoft.AspNetCore.Mvc;

namespace DataDrive.Blazor.Server.Controllers;

[ApiController]
[Route("api/document-files")]
public class DocumentFileUploadController : ControllerBase {
    private readonly INonSecuredObjectSpaceFactory _osFactory;

    public DocumentFileUploadController(INonSecuredObjectSpaceFactory osFactory) {
        _osFactory = osFactory;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(100_000_000)]
    public async Task<IActionResult> Upload(
            [FromForm] IFormFile files,
            [FromForm] string ownerObjectType,
            [FromForm] Guid ownerObjectId,
            [FromForm] Guid? typeId,
            [FromForm] string description) {

        if(files == null || files.Length == 0) {
            return BadRequest("Pusty plik.");
        }

        using IObjectSpace os = _osFactory.CreateNonSecuredObjectSpace(typeof(DocumentFile));
        var documentFile = os.CreateObject<DocumentFile>();

        using var stream = files.OpenReadStream();
        var fileData = os.CreateObject<FileData>();
        fileData.LoadFromStream(files.FileName, stream);
        documentFile.File = fileData;
        documentFile.Description = description;

        if(typeId.HasValue) {
            documentFile.Type = os.GetObjectByKey<DocumentFileType>(typeId.Value);
        }

        AttachToOwner(os, documentFile, ownerObjectType, ownerObjectId);
        os.CommitChanges();
        return Ok(new { id = documentFile.ID, name = files.FileName });
    }

    private static void AttachToOwner(IObjectSpace os, DocumentFile file,
                                       string ownerType, Guid ownerId) {
        switch(ownerType) {
            case nameof(Customer):
                file.Customer = os.GetObjectByKey<Customer>(ownerId);
                break;
            case nameof(Order):
                file.Order = os.GetObjectByKey<Order>(ownerId);
                break;
            default:
                throw new InvalidOperationException(
                    $"Nieobsługiwany typ właściciela: {ownerType}");
        }
    }
}
```

Zarejestruj controllery w `DataDrive.Blazor.Server/Startup.cs`:

```csharp
services.AddControllers();
// ...
app.MapControllers();
```

Każdy z N wybranych plików = N requestów POST = N nowych `DocumentFile` przypiętych do tego samego rodzica. `UploadMode.Instant` w `DxUpload` zapewnia, że to dzieje się automatycznie.

## Krok 10 — Layout w detail view rodzica

Dodaj nested ListView `DocumentFiles` jako zakładkę w `Customer_DetailView` (przez Model Editor albo w `Model.DesignedDiffs.xafml`):

```xml
<DetailView Id="Customer_DetailView">
  <Layout>
    <LayoutGroup Id="Main">
      <TabbedGroup Id="Tabs">
        <LayoutGroup Id="DocumentFiles" ImageName="BO_FileAttachment">
          <LayoutItem Id="DocumentFiles" ViewItem="DocumentFiles" />
        </LayoutGroup>
      </TabbedGroup>
    </LayoutGroup>
  </Layout>
</DetailView>
```

I tłumaczenia PL:

```xml
<DetailView Id="Customer_DetailView">
  <Layout>
    <LayoutGroup Id="Main">
      <TabbedGroup Id="Tabs">
        <LayoutGroup Id="DocumentFiles" Caption="Załączniki" />
      </TabbedGroup>
    </LayoutGroup>
  </Layout>
</DetailView>
```

## Test ręczny

1. `dotnet ef database update`
2. Uruchom Blazor, otwórz klienta.
3. Zakładka **Załączniki** → przycisk **Dodaj pliki**.
4. Wybierz typ („Faktura"), opcjonalnie opis.
5. Przeciągnij 3 PDF do drop-zone albo kliknij „Wybierz pliki" i zaznacz wiele.
6. Każdy plik wgrywa się natychmiast (instant mode) — widać licznik „Wgrano plików: 3".
7. Zamknij popup → lista odświeża się, widać 3 nowe pozycje.
8. Otwórz jedną z nich → w detail view widać podgląd PDF inline.

## Test automatyczny (xUnit, integracja)

```csharp
public class DocumentFileUploadTests : OnDatabaseTestBase {
    protected override TestDatabaseProvider Provider => TestDatabaseProvider.PostgreSql;

    [Fact]
    public async Task Can_attach_multiple_pdfs_to_customer() {
        var customer = DbContext.CreateProxy<Customer>();
        DbContext.Customers.Add(customer);
        await DbContext.SaveChangesAsync();

        var type = DbContext.CreateProxy<DocumentFileType>();
        type.Code = "INVOICE"; type.Name = "Faktura";
        DbContext.DocumentFileTypes.Add(type);
        await DbContext.SaveChangesAsync();

        for(int i = 0; i < 3; i++) {
            var doc = DbContext.CreateProxy<DocumentFile>();
            var fd = DbContext.CreateProxy<FileData>();
            using var ms = new MemoryStream(File.ReadAllBytes("TestData/sample.pdf"));
            fd.LoadFromStream($"faktura-{i}.pdf", ms);
            doc.File = fd;
            doc.Type = type;
            doc.Customer = customer;
            DbContext.DocumentFiles.Add(doc);
        }
        await DbContext.SaveChangesAsync();

        var loaded = await DbContext.Customers
            .Include(c => c.DocumentFiles).ThenInclude(d => d.File)
            .Include(c => c.DocumentFiles).ThenInclude(d => d.Type)
            .SingleAsync(c => c.ID == customer.ID);

        Assert.Equal(3, loaded.DocumentFiles.Count);
        Assert.All(loaded.DocumentFiles, d => Assert.Equal("INVOICE", d.Type.Code));
        Assert.All(loaded.DocumentFiles, d => Assert.True(d.File.Size > 0));
    }
}
```

## Czego brakuje (produkcyjne polish)

- **Walidacja MIME** po stronie serwera (nie tylko whitelist extension).
- **Antywirus** — np. ClamAV przez Docker przy uploadzie.
- **Thumbnails dla PDF** — `PdfDocumentProcessor.LoadDocument().Pages[0].CreateBitmap()` przy `OnCreated`. Bez tego lista 50 załączników jest bolesna w pamięci.
- **OCR** — Tesseract.NET albo Azure Document Intelligence. Dodaj pole `OcrText` (long string z indeksem full-text) i wypełniaj asynchronicznie po uploadzie.
- **Blob storage** zamiast BLOB-a w DB — Azure Blob / S3 / lokalny dysk. Klasa `FileData` przyjmuje `byte[]` w pamięci; podmień edytor i magazyn, gdy załączniki przekraczają ~5 MB albo masz dużo rekordów.
- **Wersjonowanie** — pole `Version` + relacja do poprzedniej wersji + akcja „Wgraj nową wersję".
- **Permissions** — typowy XAF: `MemberPermission` na `DocumentFile.File` dla ról, które nie mogą widzieć skanów (RODO).
- **Audyt** — kto kiedy podejrzał plik. Custom controller na `DetailView<DocumentFile>` z logowaniem przy `OnActivated`.

## Pliki które się dodaje (zbiorczo)

```
DataDrive.Module/
  BusinessObjects/Documents/
    DocumentFile.cs
    DocumentFileType.cs
    IHasDocumentFiles.cs
  Features/Documents/
    DocumentFileUploadParameters.cs
  Migrations/
    <timestamp>_AddDocumentFiles.cs (wygenerowane przez dotnet ef)

DataDrive.Blazor.Server/
  Editors/FileDataEditor/
    CustomFileDataPropertyEditor.cs
    FileDataRenderer.razor
  Editors/UploadDocumentFileEditor/
    UploadDocumentFileEditorRenderer.razor
    (analogiczny editor + adapter dla UploadModel)
  Controllers/
    DocumentFileUploadController.cs
  Features/Documents/
    DocumentFileNestedListViewController.cs
  wwwroot/js/
    file-download.js
```

Modyfikacje istniejące:

```
DataDrive.Module/BusinessObjects/Customer.cs            (+kolekcja DocumentFiles, +IHasDocumentFiles)
DataDrive.Module/BusinessObjects/Order.cs               (j.w.)
DataDrive.Module/BusinessObjects/OutlookInspiredDbContext.cs   (+2 DbSet)
DataDrive.Module/DatabaseUpdate/Updater.cs              (+SeedDocumentTypes)
DataDrive.Module/Model.DesignedDiffs.xafml              (+layout tab "DocumentFiles")
DataDrive.Module/Model.DesignedDiffs.Localization.pl.xafml (+tłumaczenia)
DataDrive.Blazor.Server/Startup.cs                      (+AddControllers/MapControllers)
DataDrive.Blazor.Server/_Host.cshtml                    (+script file-download.js)
```

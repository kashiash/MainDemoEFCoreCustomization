# Custom DateEditor z blokadą kółka myszy

Ten dokument pokazuje dokładnie, jak działa nasz globalny editor daty w `MainDemo.Blazor.Server`.

## Co robi ta zmiana

Editor robi cztery rzeczy:

1. przejmuje wszystkie pola `DateTime` i `DateTime?`,
2. blokuje zmianę wartości przez kółko myszy,
3. pozwala wyłączyć tę blokadę dla wybranego pola,
4. czyta maskę i format z modelu XAF.

## Plik 1. `CS/MainDemo.Module/Editors/DateEditMouseWheelAttribute.cs`

To jest atrybut dla pojedynczego pola:

```csharp
namespace MainDemo.Module.Editors;

[AttributeUsage(AttributeTargets.Property)]
public sealed class DateEditMouseWheelAttribute(bool blockMouseWheel) : Attribute {
    public bool BlockMouseWheel { get; } = blockMouseWheel;
}
```

## Plik 2. `CS/MainDemo.Module/Editors/EditorAliases.cs`

Stała aliasu editora siedzi w module:

```csharp
namespace MainDemo.Module.Editors;

public static class EditorAliases {
    public const string MainDemoDateTimeEditor = "MainDemoDateTimeEditor";
    public const string DocumentPreviewPropertyEditor = "DocumentPreviewPropertyEditor";
    public const string DocumentUploadAreaPropertyEditor = "DocumentUploadAreaPropertyEditor";
    public const string ResumeUploadAreaPropertyEditor = "ResumeUploadAreaPropertyEditor";
}
```

## Plik 3. `CS/MainDemo.Blazor.Server/Editors/Date/MainDemoDateTimeEditor.cs`

To jest editor dla `DateTime`:

```csharp
using DevExpress.Blazor;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Blazor.Editors.Adapters;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using EditorAliases = MainDemo.Module.Editors.EditorAliases;

namespace MainDemo.Blazor.Server.Editors.Date;

[PropertyEditor(typeof(DateTime), EditorAliases.MainDemoDateTimeEditor, true)]
public class MainDemoDateTimeEditor(Type objectType, IModelMemberViewItem model)
    : DateTimePropertyEditor(objectType, model) {
    protected override void OnControlCreated() {
        base.OnControlCreated();
        if (Control is DxDateEditModel<DateTime> adapter) {
            ConfigureMaskCaretMode();
            MainDemoDateTimeEditorConfigurator.Configure(adapter, Model);
        }
    }

    void ConfigureMaskCaretMode() {
        MaskCaretMode caretMode = MainDemoDateTimeEditorConfigurator.GetMaskCaretMode(Model);
        DxDateEditMaskProperties.DateTime.CaretMode = caretMode;
        DxDateEditMaskProperties.DateOnly.CaretMode = caretMode;
        DxDateEditMaskProperties.DateTimeOffset.CaretMode = caretMode;
    }
}
```

Trzeci parametr `true` oznacza, że to jest domyślny editor dla `DateTime`.

## Plik 4. `CS/MainDemo.Blazor.Server/Editors/Date/MainDemoNullableDateTimeEditor.cs`

To jest editor dla `DateTime?`:

```csharp
using DevExpress.Blazor;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Blazor.Editors.Adapters;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using EditorAliases = MainDemo.Module.Editors.EditorAliases;

namespace MainDemo.Blazor.Server.Editors.Date;

[PropertyEditor(typeof(DateTime?), EditorAliases.MainDemoDateTimeEditor, true)]
public class MainDemoNullableDateTimeEditor(Type objectType, IModelMemberViewItem model)
    : DateTimePropertyEditor(objectType, model) {
    protected override void OnControlCreated() {
        base.OnControlCreated();
        if (Control is DxDateEditModel<DateTime?> adapter) {
            ConfigureMaskCaretMode();
            MainDemoDateTimeEditorConfigurator.Configure(adapter, Model);
        }
    }

    void ConfigureMaskCaretMode() {
        MaskCaretMode caretMode = MainDemoDateTimeEditorConfigurator.GetMaskCaretMode(Model);
        DxDateEditMaskProperties.DateTime.CaretMode = caretMode;
        DxDateEditMaskProperties.DateOnly.CaretMode = caretMode;
        DxDateEditMaskProperties.DateTimeOffset.CaretMode = caretMode;
    }
}
```

## Plik 5. `CS/MainDemo.Blazor.Server/Editors/Date/MainDemoDateTimeEditorConfigurator.cs`

Cała logika siedzi tutaj:

```csharp
using DevExpress.Blazor;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Model;
using MainDemo.Module.Editors;

namespace MainDemo.Blazor.Server.Editors.Date;

internal static class MainDemoDateTimeEditorConfigurator {
    static readonly HashSet<char> TimeFormatTokens = new() { 'H', 'h', 's', 't', 'f', 'F', 'K', 'z' };
    static readonly HashSet<string> DateTimeStandardFormats = new(StringComparer.Ordinal) {
        "f", "F", "g", "G", "o", "O", "r", "R", "s", "t", "T", "u", "U"
    };

    public static MaskCaretMode GetMaskCaretMode(IModelMemberViewItem model) {
        if (model?.Application?.Options is IModelOptionsDateEditMouseWheel options) {
            return options.DateEditMaskCaretMode;
        }
        return MaskCaretMode.Advancing;
    }

    public static void Configure<T>(DxDateEditModel<T> adapter, IModelMemberViewItem model) {
        string editMask = NormalizeModelFormat(model?.EditMask);
        string displayFormat = NormalizeModelFormat(model?.DisplayFormat);

        if (!string.IsNullOrWhiteSpace(displayFormat)) {
            adapter.Format = displayFormat;
            adapter.DisplayFormat = displayFormat;
        }

        if (!string.IsNullOrWhiteSpace(editMask)) {
            adapter.Mask = editMask;
        }

        string effectiveFormat = editMask ?? displayFormat;
        bool hasTime = IncludesTimeSection(effectiveFormat);
        adapter.TimeSectionVisible = hasTime;
        if (hasTime) {
            adapter.TimeSectionScrollPickerFormat = "H m";
        }

        ApplyMouseWheelBehavior(adapter, model);
    }

    static void ApplyMouseWheelBehavior<T>(DxDateEditModel<T> adapter, IModelMemberViewItem model) {
        bool shouldBlock = ShouldBlockMouseWheel(model);
        AppendCssClass(adapter, shouldBlock
            ? DateEditorCssAliases.MouseWheelBlocked
            : DateEditorCssAliases.MouseWheelAllowed);
    }

    static bool ShouldBlockMouseWheel(IModelMemberViewItem model) {
        if (model == null) return true;

        var attribute = model.ModelMember?.MemberInfo?.FindAttribute<DateEditMouseWheelAttribute>();
        if (attribute != null) {
            return attribute.BlockMouseWheel;
        }

        if (model is IModelMemberViewItemMouseWheel { BlockMouseWheel: bool viewItemValue }) {
            return viewItemValue;
        }

        if (model.Application?.Options is IModelOptionsDateEditMouseWheel options) {
            return options.BlockDateEditMouseWheelByDefault;
        }

        return true;
    }

    static void AppendCssClass<T>(DxDateEditModel<T> adapter, string cssClass) {
        adapter.CssClass = string.IsNullOrWhiteSpace(adapter.CssClass)
            ? cssClass
            : adapter.CssClass + " " + cssClass;
        adapter.InputCssClass = string.IsNullOrWhiteSpace(adapter.InputCssClass)
            ? cssClass
            : adapter.InputCssClass + " " + cssClass;
    }

    static string NormalizeModelFormat(string format) {
        if (string.IsNullOrWhiteSpace(format)) return null;
        string normalized = format.Trim();
        if (normalized.StartsWith("{0:", StringComparison.Ordinal) && normalized.EndsWith("}")) {
            normalized = normalized.Substring(3, normalized.Length - 4);
        }
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    static bool IncludesTimeSection(string format) {
        if (string.IsNullOrWhiteSpace(format)) return false;
        string normalized = NormalizeModelFormat(format) ?? string.Empty;
        if (DateTimeStandardFormats.Contains(normalized)) return true;

        string maskWithoutLiterals = RemoveQuotedAndEscapedLiterals(normalized);
        for (int i = 0; i < maskWithoutLiterals.Length; i++) {
            char token = maskWithoutLiterals[i];
            if (TimeFormatTokens.Contains(token)) return true;
            if (token == 'm' && normalized.Length > 1) return true;
        }
        return false;
    }

    static string RemoveQuotedAndEscapedLiterals(string value) {
        var result = new System.Text.StringBuilder(value.Length);
        bool inSingle = false, inDouble = false;
        for (int i = 0; i < value.Length; i++) {
            char current = value[i];
            if (current == '\\') { i++; continue; }
            if (current == '\'' && !inDouble) { inSingle = !inSingle; continue; }
            if (current == '\"' && !inSingle) { inDouble = !inDouble; continue; }
            if (!inSingle && !inDouble) result.Append(current);
        }
        return result.ToString();
    }
}
```

Ten plik:

1. czyta `EditMask` i `DisplayFormat`,
2. decyduje, czy pokazać część czasu,
3. decyduje, czy blokować kółko myszy,
4. dokleja odpowiednią klasę CSS.

## Plik 6. `CS/MainDemo.Blazor.Server/Editors/Date/IModelOptionsDateEditMouseWheel.cs`

Globalne ustawienia dla aplikacji:

```csharp
using System.ComponentModel;
using DevExpress.Blazor;

namespace MainDemo.Blazor.Server.Editors.Date;

public interface IModelOptionsDateEditMouseWheel {
    [Category("Behavior")]
    [Description("Globalne ustawienie domyślne. Gdy True, przewijanie kółkiem myszy wewnątrz edytorów daty nie zmienia wartości pola.")]
    [DefaultValue(true)]
    bool BlockDateEditMouseWheelByDefault { get; set; }

    [Category("Behavior")]
    [Description("Globalny tryb przesuwania kursora w maskach edytorów daty. Advancing oznacza, że kursor sam przeskakuje do następnej sekcji po wpisaniu maksymalnej liczby znaków.")]
    [DefaultValue(MaskCaretMode.Advancing)]
    MaskCaretMode DateEditMaskCaretMode { get; set; }
}
```

## Plik 7. `CS/MainDemo.Blazor.Server/Editors/Date/IModelMemberViewItemMouseWheel.cs`

Ustawienie dla konkretnego pola w konkretnym widoku:

```csharp
using System.ComponentModel;
using DevExpress.ExpressApp.Model;

namespace MainDemo.Blazor.Server.Editors.Date;

public interface IModelMemberViewItemMouseWheel : IModelMemberViewItem {
    [Category("Behavior")]
    [Description("Opcjonalne ustawienie dla konkretnego pola. Null oznacza: użyj wartości z Options.BlockDateEditMouseWheelByDefault.")]
    bool? BlockMouseWheel { get; set; }
}
```

## Plik 8. `CS/MainDemo.Blazor.Server/Editors/Date/DateEditorCssAliases.cs`

Stałe klas CSS:

```csharp
namespace MainDemo.Blazor.Server.Editors.Date;

public static class DateEditorCssAliases {
    public const string MouseWheelBlocked = "maindemo-dateedit-wheel-blocked";
    public const string MouseWheelAllowed = "maindemo-dateedit-wheel-allowed";
}
```

## Plik 9. `CS/MainDemo.Blazor.Server/Editors/Date/DateEditMouseWheelGuardController.cs`

Kontroler ładuje moduł JavaScript:

```csharp
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace MainDemo.Blazor.Server.Editors.Date;

public class DateEditMouseWheelGuardController : ViewController {
    IJSRuntime jsRuntime;

    protected override void OnActivated() {
        base.OnActivated();
        jsRuntime = Application?.ServiceProvider?.GetService<IJSRuntime>();
    }

    protected override void OnViewControlsCreated() {
        base.OnViewControlsCreated();
        _ = RegisterWheelGuard();
    }

    async Task RegisterWheelGuard() {
        if (jsRuntime == null) {
            return;
        }
        try {
            var module = await jsRuntime.InvokeAsync<IJSObjectReference>(
                "import",
                "./js/maindemo-date-edit-wheel-guard.js");
            await module.InvokeVoidAsync("ensureRegistered");
            await module.DisposeAsync();
        }
        catch (JSException ex) {
            Tracing.Tracer.LogError(ex);
        }
    }
}
```

## Plik 10. `CS/MainDemo.Blazor.Server/wwwroot/js/maindemo-date-edit-wheel-guard.js`

To jest cały moduł JS:

```javascript
let registered = false;

export function ensureRegistered() {
    if (registered) {
        return;
    }

    registered = true;
    document.addEventListener('wheel', function (e) {
        const target = e.target;
        if (!target || typeof target.closest !== 'function') {
            return;
        }

        if (target.closest('.maindemo-dateedit-wheel-allowed')) {
            return;
        }

        if (target.closest('.maindemo-dateedit-wheel-blocked')) {
            e.preventDefault();
            e.stopImmediatePropagation();
        }
    }, { capture: true, passive: false });
}
```

## Plik 11. `CS/MainDemo.Blazor.Server/BlazorModule.cs`

Tu rejestrujemy rozszerzenia modelu:

```csharp
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.BaseImpl.EF;
using MainDemo.Blazor.Server.Controllers;
using MainDemo.Blazor.Server.Editors.Date;

namespace MainDemo.Blazor.Server;

public sealed class MainDemoBlazorModule : ModuleBase {
    public MainDemoBlazorModule() {
        Description = "XAF Blazor Demo module";
    }

    public override void Setup(XafApplication application) {
        base.Setup(application);
        application.CreateCustomLogonWindowControllers += Application_CreateCustomLogonWindowControllers;
        application.CreateCustomUserModelDifferenceStore += Application_CreateCustomUserModelDifferenceStore;
    }

    private void Application_CreateCustomUserModelDifferenceStore(object sender, CreateCustomModelDifferenceStoreEventArgs e) {
        e.Store = new ModelDifferenceDbStore((XafApplication)sender, typeof(ModelDifference), false, "Blazor");
        e.Handled = true;
    }

    private void Application_CreateCustomLogonWindowControllers(object sender, CreateCustomLogonWindowControllersEventArgs e) {
        e.Controllers.Add(Application.CreateController<LogonParametersViewController>());
    }

    protected override IEnumerable<Type> GetDeclaredExportedTypes() {
        return Type.EmptyTypes;
    }

    public override void CustomizeTypesInfo(ITypesInfo typesInfo) {
        base.CustomizeTypesInfo(typesInfo);
    }

    public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders) {
        base.ExtendModelInterfaces(extenders);
        extenders.Add<IModelOptions, IModelOptionsDateEditMouseWheel>();
        extenders.Add<IModelMemberViewItem, IModelMemberViewItemMouseWheel>();
    }
}
```

Najważniejsza część to:

```csharp
extenders.Add<IModelOptions, IModelOptionsDateEditMouseWheel>();
extenders.Add<IModelMemberViewItem, IModelMemberViewItemMouseWheel>();
```

## Plik 12. `CS/MainDemo.Module/BusinessObjects/Employee.cs`

W tym repo świadomy wyjątek ma pole `Birthday`:

```csharp
[DateEditMouseWheel(false)]
public virtual DateTime? Birthday { get; set; }
```

Cały fragment klasy wygląda tak:

```csharp
public class Employee : BaseObject, IHasDocumentFiles {
    public virtual String FirstName { get; set; }

    public virtual String LastName { get; set; }

    public virtual String MiddleName { get; set; }

    [DateEditMouseWheel(false)]
    public virtual DateTime? Birthday { get; set; }

    [FieldSize(255)]
    public virtual String Email { get; set; }

    [SearchMemberOptions(SearchMemberMode.Exclude)]
    [JsonIgnore]
    [PersistentAlias("Concat(FirstName, ' ', MiddleName, Iif(Len([MiddleName]) > 0, ' ', ''), LastName)")]
    public String FullName {
        get => EvaluateAlias<String>();
    }

    [ImageEditor]
    public virtual Byte[] Photo { get; set; }

    [Aggregated]
    public virtual IList<PhoneNumber> PhoneNumbers { get; set; } = new ObservableCollection<PhoneNumber>();

    [Aggregated, ExpandObjectMembers(ExpandObjectMembers.Never)]
    public virtual Address Address1 { get; set; }

    [Aggregated, ExpandObjectMembers(ExpandObjectMembers.Never)]
    public virtual Address Address2 { get; set; }
}
```

## Kolejność decyzji

Blokada kółka jest wyliczana w tej kolejności:

1. atrybut na właściwości,
2. ustawienie `BlockMouseWheel` w modelu widoku,
3. ustawienie globalne `BlockDateEditMouseWheelByDefault`.

## Jak to wdrożyć

1. dodać atrybut i alias do `MainDemo.Module`,
2. dodać oba editory do `MainDemo.Blazor.Server/Editors/Date/`,
3. dodać konfigurator, interfejsy modelu i kontroler,
4. dodać moduł JS do `wwwroot/js/`,
5. zarejestrować interfejsy w `BlazorModule`,
6. opcjonalnie oznaczyć wybrane pola atrybutem `[DateEditMouseWheel(false)]`.

## Zmienione pliki

```text
CS/MainDemo.Module/Editors/DateEditMouseWheelAttribute.cs
CS/MainDemo.Module/Editors/EditorAliases.cs
CS/MainDemo.Blazor.Server/Editors/Date/MainDemoDateTimeEditor.cs
CS/MainDemo.Blazor.Server/Editors/Date/MainDemoNullableDateTimeEditor.cs
CS/MainDemo.Blazor.Server/Editors/Date/MainDemoDateTimeEditorConfigurator.cs
CS/MainDemo.Blazor.Server/Editors/Date/IModelOptionsDateEditMouseWheel.cs
CS/MainDemo.Blazor.Server/Editors/Date/IModelMemberViewItemMouseWheel.cs
CS/MainDemo.Blazor.Server/Editors/Date/DateEditorCssAliases.cs
CS/MainDemo.Blazor.Server/Editors/Date/DateEditMouseWheelGuardController.cs
CS/MainDemo.Blazor.Server/wwwroot/js/maindemo-date-edit-wheel-guard.js
CS/MainDemo.Blazor.Server/BlazorModule.cs
CS/MainDemo.Module/BusinessObjects/Employee.cs
```

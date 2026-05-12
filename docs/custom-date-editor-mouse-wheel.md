# Custom DateEditor z parametrem modelowym do blokady kółka myszy

Ten dokument opisuje konkretną zmianę, którą zrobiłem w `MainDemo.NET.EFCore`. Pattern wzięty 1:1 z `HIS.Blazor.Server\Editors\DateEditor.cs`, ale rozszerzony o jedną rzecz, której w HIS nie ma: **opt-out z poziomu Model Editora XAF** dla pojedynczego pola. Czyli developer nie musi grzebać w kodzie, żeby na danej kolumnie wyłączyć blokadę kółka, jak akurat ją gdzieś chce.

## Po co to wszystko

DevExpress `DxDateEdit` ma takie zachowanie, że jak kursor stoi w którejś sekcji maski daty (dzień/miesiąc/rok), to **scroll kółkiem myszy zmienia wartość tej sekcji**. Dla operatorek wbijających daty cały dzień to jest często wrogie zachowanie — przesuwamy listę w dół, mijamy date editora, wartość się ciurka zmienia, klient ma w bazie krzywą datę.

Drugi temat — `MaskCaretMode`. DevExpress domyślnie ma `Static` (kursor stoi w sekcji aż przerzucisz Tab/strzałką). `Advancing` skacze do następnej sekcji jak skończysz wpisywać poprzednią. Trzeba to zmienić, bo dla maski `dd.MM.yyyy` przy `Static` operator co dwa znaki musi `Tab`-ować.

Te dwa fixy chodzą zawsze razem.

## Co jest w repo po tej zmianie

### Plik 1: `CS\MainDemo.Blazor.Server\Editors\DateEditor.cs`

Cały plik, jeden custom editor dla `DateTime`, drugi dla `DateTime?` (XAF property editory są typo-specyficzne — nullable to oddzielna klasa). Plus statyczne stałe aliasów i interfejs rozszerzający Model Editor XAF-a.

Stałe i interfejs modelu:

```csharp
public static class CustomEditorAliases {
    public const string DateEditor = "DateEditor";
    public const string DateEditorNullable = "DateEditorNullable";
    public const string MouseWheelBlockerCssClass = "maindemo-wheel-blocked";
}

public interface IModelMemberViewItemMouseWheel : IModelMemberViewItem {
    [Category("Behavior")]
    [Description("When true, scrolling the mouse wheel inside this date editor will not change the value.")]
    [DefaultValue(true)]
    bool BlockMouseWheel { get; set; }
}
```

Sam editor (skrócony — dla nullable wygląda identycznie z `DxDateEditModel<DateTime?>`):

```csharp
[PropertyEditor(typeof(DateTime), CustomEditorAliases.DateEditor, false)]
public class DateEditor(Type objectType, IModelMemberViewItem model) : DateTimePropertyEditor(objectType, model) {
    protected override void OnControlCreated() {
        base.OnControlCreated();
        if (Control is DxDateEditModel<DateTime> adapter) {
            DxDateEditMaskProperties.DateTime.CaretMode = MaskCaretMode.Advancing;
            DxDateEditMaskProperties.DateOnly.CaretMode = MaskCaretMode.Advancing;
            DxDateEditMaskProperties.DateTimeOffset.CaretMode = MaskCaretMode.Advancing;

            adapter.Format = "dd.MM.yyyy";
            adapter.DisplayFormat = "dd.MM.yyyy";
            adapter.Mask = "dd.MM.yyyy";
            ApplyMouseWheelBlocker(adapter);
            adapter.Buttons = CreateButton();
        }
    }

    void ApplyMouseWheelBlocker<T>(DxDateEditModel<T> adapter) {
        if (Model is IModelMemberViewItemMouseWheel m && !m.BlockMouseWheel) {
            return;
        }
        adapter.CssClass = string.IsNullOrEmpty(adapter.CssClass)
            ? CustomEditorAliases.MouseWheelBlockerCssClass
            : adapter.CssClass + " " + CustomEditorAliases.MouseWheelBlockerCssClass;
    }
    // CreateViewComponentCore + CreateButton — patrz pełen plik
}
```

Punkty wymagające uwagi:

- `isDefaultEditor: false` w `[PropertyEditor]` — to jest **świadoma decyzja**. Edytor jest dostępny pod aliasem, ale nie nadpisuje globalnie `DateTimePropertyEditor` dla wszystkich `DateTime` w aplikacji. Włącza się go per pole atrybutem `[EditorAlias]`.
- `DxDateEditMaskProperties.{DateTime,DateOnly,DateTimeOffset}.CaretMode` to są **statyczne globalne** w DevExpress Blazor. Ustawienie ich w `OnControlCreated` per instancja jest redundantne — wystarczyłoby raz przy starcie aplikacji w `Program.cs`. Zostawiłem jak w HIS, bo nie szkodzi i pattern jest spójny. Jak ktoś będzie miał ochotę posprzątać, można to wyciąć i przerzucić do `BlazorModule.Setup` albo `Program.Main`.
- `ApplyMouseWheelBlocker` doczepia CSS klasę `maindemo-wheel-blocked` do root-a kontrolki przez `adapter.CssClass`. **Tylko** gdy `Model.BlockMouseWheel == true`. Jeśli developer w Model Editor ustawi `BlockMouseWheel = False` dla konkretnego pola, klasa nie zostanie dodana i kółko działa normalnie.

### Plik 2: `CS\MainDemo.Blazor.Server\wwwroot\js\disable-wheel-on-editors.js`

```javascript
(function () {
    document.addEventListener('wheel', function (e) {
        var t = e.target;
        if (t && typeof t.closest === 'function' && t.closest('.maindemo-wheel-blocked')) {
            e.preventDefault();
            e.stopImmediatePropagation();
        }
    }, { capture: true, passive: false });
})();
```

Kluczowe parametry:

- `capture: true` — listener łapie zdarzenie **przed** DevExpress, w fazie capture. Bez tego DevExpress dostaje wheel pierwszy i zmienia wartość zanim my zdążymy `preventDefault`.
- `passive: false` — nowoczesne przeglądarki domyślnie traktują `wheel` jako passive (`preventDefault` jest ignorowane). Trzeba jawnie wymusić non-passive.
- `stopImmediatePropagation()` — zatrzymuje też inne listenery na tym samym elemencie (w tym potencjalne wewnętrzne `wheel` DevExpressa zarejestrowane też w capture).
- Selektor `.maindemo-wheel-blocked` — celuje wyłącznie w nasz marker. Niezależny od wewnętrznych klas DevExpress (`dxbl-dateedit` itp.), które się zmieniają między wersjami.

### Plik 3: `CS\MainDemo.Blazor.Server\Pages\_Host.cshtml`

Po `js/scripts.js`, przed zamknięciem `</body>`:

```html
<script src="js/disable-wheel-on-editors.js"></script>
```

Kolejność ma znaczenie — `_framework/blazor.server.js` musi być pierwszy.

### Plik 4: `CS\MainDemo.Blazor.Server\BlazorModule.cs`

XAF nie zobaczy `IModelMemberViewItemMouseWheel` w Model Editorze, dopóki nie zarejestrujemy interfejsu jako rozszerzenia bazowego `IModelMemberViewItem`:

```csharp
public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders) {
    base.ExtendModelInterfaces(extenders);
    extenders.Add<IModelMemberViewItem, IModelMemberViewItemMouseWheel>();
}
```

Po tym kroku każdy `ViewItem` modelu typu `IModelMemberViewItem` dostaje w Application Model property `BlockMouseWheel` (sekcja `Behavior`, default `True`).

### Plik 5: `CS\MainDemo.Blazor.Server\MainDemo.Blazor.Server.csproj`

`EnableDefaultItems` jest tu `false`, więc dorzucenie pliku `.cs` w `Editors\` wymaga jawnego wpisu:

```xml
<Compile Include="Editors\DateEditor.cs" />
```

### Plik 6: Business objects z `[EditorAlias]`

Włączenie edytora na konkretnych polach. Trzy w `DemoTask.cs`:

```csharp
[EditorAlias("DateEditorNullable")]
public virtual DateTime? DateCompleted { get; set; }

[EditorAlias("DateEditorNullable")]
public virtual DateTime? DueDate { get; set; }

[EditorAlias("DateEditorNullable")]
public virtual DateTime? StartDate { get; set; }
```

Jeden w `Employee.cs`:

```csharp
[EditorAlias("DateEditorNullable")]
public virtual DateTime? Birthday { get; set; }
```

## Jak to działa razem

1. Developer dodaje `[EditorAlias("DateEditorNullable")]` na property `DateTime?` w jakiejś klasie biznesowej.
2. XAF buduje View Item i zamiast wbudowanego `DateTimePropertyEditor` instancjonuje nasze `DateEditorNullable`.
3. `OnControlCreated` ustawia maskę `dd.MM.yyyy`, `MaskCaretMode.Advancing`, ikonę zegara (klik = `DateTime.Now`).
4. `ApplyMouseWheelBlocker` patrzy w model: jeśli `Model.BlockMouseWheel == true` (default), dorzuca klasę CSS `maindemo-wheel-blocked` do roota kontrolki.
5. Globalny listener z `disable-wheel-on-editors.js` łapie wheel w capture phase. Jak target jest pod elementem z `.maindemo-wheel-blocked`, robi `preventDefault` + `stopImmediatePropagation`.
6. DevExpress nie dostaje już wheel-a, wartość się nie zmienia.

## Jak developer dezaktywuje blokadę kółka dla jednego pola

Zero zmian w kodzie. W Model Editor:

```
Application Model → Views → DemoTask_DetailView → Items → DueDate
   Property: BlockMouseWheel = False
```

Zapis `Model.xafml` zostaje przy projekcie albo per użytkownik (`Model.User.xafml`) — zależy gdzie wchodzisz w editor. Po reloadzie aplikacji to konkretne pole znów reaguje na scroll, reszta zostaje zablokowana.

## Jak developer dodaje custom DateEditor do swojego pola

Pole `DateTime?`:

```csharp
[EditorAlias("DateEditorNullable")]
public virtual DateTime? KiedyZrobione { get; set; }
```

Pole `DateTime` (non-nullable):

```csharp
[EditorAlias("DateEditor")]
public virtual DateTime CzasUtworzenia { get; set; }
```

To wszystko. Caret mode i blokada kółka działają automatycznie z domyślnymi ustawieniami.

## Dlaczego ten wariant, a nie inny

Rozważałem trzy:

1. **Globalny JS po klasach DevExpress** (`.dxbl-dateedit, .dxbl-timeedit`) — zacząłem od tego. Nie zadziałało. Klasy DevExpressa w v25.2 są inne niż w v24, a poza tym DevExpress łapie `wheel` w capture phase, więc bubble-listener przyszedł za późno. Naprawiać dwa razy nie chciałem.
2. **JSInterop per editor w `OnControlCreated`** — czyste C#-side, ale daje koszt per instancja kontrolki (resolve IJSRuntime, await invocation) i nie da się tego zrobić w XAF property editor bez ręcznego pobierania serwisu z `Application.ServiceProvider`. Nadmiernie skomplikowane.
3. **Marker CSS + globalny JS targetujący tylko ten marker** ← wybrałem to. Listener jest jeden, w capture phase, działa niezależnie od DevExpressa, a opt-out wisi w modelu, nie w kodzie.

Wariant 3 ma jeden minus: jeśli ktoś usunie plik JS, blokada przestaje działać i nie ma żadnego sygnału. Asercja w `_Host.cshtml` że ten skrypt jest, plus convention test, to opcjonalne ulepszenia.

## Pułapki

- **Nie zapomnij o `ExtendModelInterfaces`**. Bez rejestracji w `BlazorModule` interfejs `IModelMemberViewItemMouseWheel` istnieje, ale Model Editor go nie zobaczy. Wszystkie pola będą blokować kółko (default `true` w interfejsie) i nie ma jak tego wyłączyć z modelu, tylko z kodu.
- **`EnableDefaultItems=false` w csproj**. Każdy nowy `.cs` musi być dorzucony do `<Compile Include>`. Łatwo zgubić ten krok, bo Visual Studio dorzuca to automatycznie tylko w projektach z domyślnymi itemami.
- **`{ passive: false }` musi być jawnie**. Bez tego `preventDefault` jest cicho zignorowane i wartość się dalej zmienia.
- **`capture: true` ma znaczenie**. Bez tego DevExpress czyta wheel pierwszy.
- **`MaskCaretMode.Advancing` jest globalny dla aplikacji**. Ustawienie go w `OnControlCreated` per kontrolka działa, ale nadpisuje wartość dla **wszystkich** `DxDateEdit` w aplikacji, też tych nie używających naszego editora. Jeśli ktoś chce mieć `Static` gdzieś indziej — trzeba przerzucić to do `Program.cs` i jawnie zdecydować na poziomie aplikacji.

## Lista zmienionych plików

- `CS\MainDemo.Blazor.Server\Editors\DateEditor.cs` (nowy)
- `CS\MainDemo.Blazor.Server\wwwroot\js\disable-wheel-on-editors.js` (nowy)
- `CS\MainDemo.Blazor.Server\Pages\_Host.cshtml`
- `CS\MainDemo.Blazor.Server\BlazorModule.cs`
- `CS\MainDemo.Blazor.Server\MainDemo.Blazor.Server.csproj`
- `CS\MainDemo.Module\BusinessObjects\DemoTask.cs`
- `CS\MainDemo.Module\BusinessObjects\Employee.cs`

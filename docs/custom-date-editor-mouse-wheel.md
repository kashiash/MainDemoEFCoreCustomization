# Custom DateEditor z blokadą kółka myszy i konfigurowalnym caret mode

Ten dokument opisuje custom property editor dla pól typu `DateTime` / `DateTime?` w `MainDemo.Blazor.Server`. Editor robi dwie rzeczy: **blokuje scroll kółka myszy** od zmieniania wartości daty oraz **konfiguruje `MaskCaretMode`** tak, żeby kursor sam przeskakiwał między sekcjami maski. Obie konfiguracje są sterowane z poziomu Model Editora XAF i atrybutami na property.

## Po co to wszystko

DevExpress `DxDateEdit` ma takie zachowanie, że jak kursor stoi w którejś sekcji maski daty (dzień/miesiąc/rok), to **scroll kółkiem myszy zmienia wartość tej sekcji**. Dla operatorek wbijających daty cały dzień to jest często wrogie zachowanie — przesuwamy listę w dół, mijamy date editora, wartość się ciurka zmienia, klient ma w bazie krzywą datę.

Drugi temat — `MaskCaretMode`. DevExpress domyślnie ma `Static` (kursor stoi w sekcji aż przerzucisz Tab/strzałką). `Advancing` skacze do następnej sekcji jak skończysz wpisywać poprzednią. Trzeba to zmienić, bo dla maski `dd.MM.yyyy` przy `Static` operator co dwa znaki musi `Tab`-ować.

Te dwa fixy chodzą zawsze razem.

## Wersja minimalna — wszystko na sztywno, bez konfiguracji

Jeśli akceptujemy, że scroll będzie zablokowany **wszędzie** i `MaskCaretMode` ma być **zawsze** `Advancing`, bez żadnej możliwości wyjątku per pole czy per widok, do celu wystarczą **dwa pliki**: jeden `ViewController` po stronie C# i jeden plik JS po stronie przeglądarki. Bez własnego property editora, bez atrybutu, bez rozszerzania Application Model, bez `ExtendModelInterfaces`. To jest ten sam pomysł, który DevExpress dokumentuje pod hasłem „Customize a Built-in Property Editor" — kontroler zamiast subclass-owania.

### Krok 1: kontroler ustawiający caret mode i CSS-classę

Plik `CS\MainDemo.Blazor.Server\Controllers\GlobalDateEditorTweaksController.cs` (zwykły `ViewController<DetailView>`, XAF wykryje go automatycznie):

```csharp
using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Editors;

namespace MainDemo.Blazor.Server.Controllers;

public class GlobalDateEditorTweaksController : ViewController<DetailView> {
    protected override void OnActivated() {
        base.OnActivated();
        DxDateEditMaskProperties.DateTime.CaretMode = MaskCaretMode.Advancing;
        DxDateEditMaskProperties.DateOnly.CaretMode = MaskCaretMode.Advancing;
        DxDateEditMaskProperties.DateTimeOffset.CaretMode = MaskCaretMode.Advancing;
    }

    protected override void OnViewControlsCreated() {
        base.OnViewControlsCreated();
        foreach (var item in View.Items.OfType<PropertyEditor>()) {
            Type t = item.MemberInfo.MemberType;
            if (t == typeof(DateTime) && item.Control is DxDateEditModel<DateTime> a1) {
                AppendCss(a1);
            }
            else if (t == typeof(DateTime?) && item.Control is DxDateEditModel<DateTime?> a2) {
                AppendCss(a2);
            }
        }
    }

    static void AppendCss<T>(DxDateEditModel<T> adapter) {
        const string cls = "maindemo-dateedit-wheel-blocked";
        adapter.CssClass = string.IsNullOrEmpty(adapter.CssClass) ? cls : adapter.CssClass + " " + cls;
        adapter.InputCssClass = string.IsNullOrEmpty(adapter.InputCssClass) ? cls : adapter.InputCssClass + " " + cls;
    }
}
```

Kontroler iteruje po wszystkich `PropertyEditor`-ach w `DetailView`, sprawdza, czy to pole typu `DateTime` lub `DateTime?`, i doczepia stałą CSS-class do adaptera DevExpress. Nie tworzy własnego editora, nie nadpisuje `[PropertyEditor]`, nie wymaga `[EditorAlias]` na business objectcie. Działa, bo XAF Blazor `DateTimePropertyEditor` używa pod spodem `DxDateEditModel<T>` jako adapter modelu komponentu — ten sam typ, do którego cast-uje docelowa wersja z `Editors/Date/`.

### Krok 2: globalny listener JS

W `CS\MainDemo.Blazor.Server\Pages\_Host.cshtml` po `_framework/blazor.server.js`:

```html
<script>
(function () {
    document.addEventListener('wheel', function (e) {
        var t = e.target;
        if (t && typeof t.closest === 'function' && t.closest('.maindemo-dateedit-wheel-blocked')) {
            e.preventDefault();
            e.stopImmediatePropagation();
        }
    }, { capture: true, passive: false });
})();
</script>
```

Trzy kluczowe flagi: `capture: true` (listener łapie zdarzenie przed DevExpress), `passive: false` (`preventDefault` faktycznie działa), `stopImmediatePropagation()` (zatrzymuje też inne listenery na tym samym elemencie). Selektor `.maindemo-dateedit-wheel-blocked` celuje wyłącznie w nasz marker — niezależny od wewnętrznych klas DevExpress, które zmieniają się między wersjami.

To wszystko. Dwa pliki, ~40 linii kodu razem, zero rejestracji modeli, zero atrybutów na business objectach. Build + run i wszystkie pola daty w aplikacji mają zablokowany scroll i `MaskCaretMode.Advancing`.

### Czego ta wersja nie daje

- **Nie ma wyjątków per pole.** Jeśli kiedyś jedno konkretne pole ma scrollować (np. data urodzenia, gdzie wygodniej cofnąć rok kółkiem), musimy albo zmienić kod, albo zrobić wyjątek dodając mu inną CSS-class z innego controllera. W obu wariantach jest to twarda zmiana w kodzie, nie konfiguracja.
- **Nie ma wyjątków per widok.** Gdyby data urodzenia była scrollowalna w widoku rekrutacji, a zablokowana w HR, jednego controllera już nie wystarczy.
- **Nie ma konfiguracji bez recompile.** Klient/operator nie wpłynie na zachowanie inaczej niż przez nowy build i deploy.
- **Caret mode jest globalny.** `Advancing` dla wszystkich pól bez wyjątku. Gdyby ktoś chciał `Static` (np. dla pola gdzie maska jest niestandardowa), trzeba przerobić kontroler.
- **`DxDateEditMaskProperties.*.CaretMode` to globalny statyczny stan DevExpress.** Ustawiamy go w `OnActivated` przy każdym widoku — redundantnie, ale nieszkodliwie. Jeśli kiedykolwiek chcielibyśmy różne caret-mode w różnych widokach, ta strategia nie ma jak.
- **`ListView` (grid inline edit)** nie jest pokryty, bo controller jest `ViewController<DetailView>`. Dla `ListView` trzeba dorobić analogiczny lub zmienić bazę na samo `ViewController` i obsłużyć oba typy. Tematycznie należałoby przemyśleć, czy w gridzie też ma blokować.

Dla większości projektów typu „demo + jedna aplikacja, jedna domena" ta wersja jest wystarczająca. Dla projektów, gdzie:

- różne klasy biznesowe potrzebują różnych ustawień,
- chcemy, żeby zmianę zachowania per widok mógł zrobić operator/admin bez rebuilda,
- mamy zespół developerów i chcemy deklaratywnego API (atrybut na property zamiast „przeczytaj, co robi kontroler"),

przechodzimy na wersję pełną opisaną w pozostałej części dokumentu.

## Co dodać, żeby zarządzać tym z Model Editora

Wersja minimalna z poprzedniej sekcji ma sztywno wpisane: „wszystko zablokowane, caret mode Advancing". Żeby z tego zrobić system, w którym deweloper i admin mogą wpływać na zachowanie bez rebuilda, dorzucamy następujące elementy. Każdy z nich rozwiązuje jeden konkretny problem wersji minimalnej, więc można je wprowadzać iteracyjnie — niekoniecznie wszystkie naraz.

1. **Własna subclass `DateTimePropertyEditor` zamiast generycznego kontrolera** (dwie klasy: dla `DateTime` i `DateTime?`). Powód: PropertyEditor ma własne `Model` (`IModelMemberViewItem`), do którego XAF potrafi dorzucić nasze własne property widoczne w Model Editor. Generyczny `ViewController` nie ma „własnego Model" i nie zostanie pokazany w Model Editorze jako konfigurowalny.
2. **Atrybut `[DateEditMouseWheel(false)]`** na property w business objectcie. Powód: są pola, gdzie decyzja „scroll OK / scroll blokuje" należy do modelu domenowego, nie do xafml — np. `Employee.Birthday`. Atrybut trzyma tę informację w kodzie razem z definicją property, gdzie ją znajdzie review pull requesta.
3. **Interfejs `IModelMemberViewItemMouseWheel`** z nullable `BlockMouseWheel`. Powód: w Model Editor każdy `MemberViewItem` dostaje nowe property `BlockMouseWheel`. Pozwala wyłączyć blokadę dla pola w **konkretnym widoku** bez zmiany kodu — pole może być scrollowalne w jednym widoku, a zablokowane w drugim.
4. **Interfejs `IModelOptionsDateEditMouseWheel`** z `BlockDateEditMouseWheelByDefault` i `DateEditMaskCaretMode`. Powód: globalna wartość domyślna dla całej aplikacji zapisana w `Model.xafml`. Admin/devops może to zmienić w trakcie deploy-a bez recompile.
5. **`ExtendModelInterfaces` w `BlazorModule.cs`**. Powód: bez tego XAF Model Editor nie pokaże nowych property z punktów 3 i 4 — interfejsy istnieją, ale nie są zaczepione do bazowych `IModelMemberViewItem` / `IModelOptions`.
6. **Konfigurator z kaskadą trzech poziomów** (atrybut → ViewItem → IModelOptions). Powód: trzymanie logiki decyzji „blokować czy nie" w jednym miejscu zamiast duplikowania w obu editorach. Przy okazji zapewnia jednoznaczną kolejność precedencji.
7. **Wykrywanie sekcji czasu z formatu (`EditMask` / `DisplayFormat`)** w konfiguratorze. Powód: pole z `DisplayFormat="dd.MM.yyyy HH:mm"` powinno mieć widoczną sekcję czasu w UI, a pole z `dd.MM.yyyy` — nie. Bez wykrywania trzeba by trzymać dwa różne typy editorów albo manualnie ustawiać `TimeSectionVisible` w każdym xafml-u.
8. **Dwie CSS-classy (`-blocked` i `-allowed`) zamiast jednej**. Powód: opt-out per pole działa tak, że pole „dozwolone" dostaje klasę `-allowed`. JS guard widząc tę klasę robi `return` **przed** sprawdzeniem `-blocked`. To prostszy mechanizm niż usuwanie klasy `-blocked`, bo działa też dla pól zagnieżdżonych.
9. **JS jako moduł ESM z idempotentnym `ensureRegistered()`, ładowany przez controller**. Powód: pozbywamy się hardcoded `<script>` w `_Host.cshtml` (kolejność ładowania bywa krucha) i mamy gwarancję, że listener nie jest rejestrowany dwa razy nawet przy SignalR-reconnect.

W pozostałej części artykułu każdy z tych punktów jest rozwinięty: **„Struktura plików"** pokazuje, gdzie który element siedzi; **„Trzy poziomy konfiguracji blokady kółka"** opisuje kaskadę z punktu 6 i atrybut z punktu 2; **„`MaskCaretMode`"** opisuje globalną konfigurację caret mode; **„Wykrywanie sekcji czasu z formatu"** rozwija punkt 7; **„Rejestracja w `BlazorModule`"** rozwija punkt 5; **„Blokada kółka po stronie JS"** rozwija punkty 8 i 9.

## Struktura plików

Edytor jest rozbity tematycznie — po wzorcu z `OutlookInspiredDemo/DataDrive.Blazor.Server/Editors/`, każda odpowiedzialność w osobnym pliku.

### `MainDemo.Module` — części reusable między platformami

- `CS\MainDemo.Module\Editors\EditorAliases.cs` — stałe stringowe nazwy aliasów editorów. Trzymane w Module, żeby business objecty mogły referować je przez `[EditorAlias(EditorAliases.MainDemoDateTimeEditor)]` zamiast hardkodować literał.
- `CS\MainDemo.Module\Editors\DateEditMouseWheelAttribute.cs` — atrybut deklaratywny do nakładania na pojedyncze property. Nie zależy od żadnego typu UI, więc bezpiecznie żyje w Module.

### `MainDemo.Blazor.Server` — części UI-specyficzne

- `CS\MainDemo.Blazor.Server\Editors\Date\MainDemoDateTimeEditor.cs` — editor dla `DateTime`.
- `CS\MainDemo.Blazor.Server\Editors\Date\MainDemoNullableDateTimeEditor.cs` — editor dla `DateTime?` (XAF property editory są typo-specyficzne, nullable to oddzielna klasa).
- `CS\MainDemo.Blazor.Server\Editors\Date\MainDemoDateTimeEditorConfigurator.cs` — wspólna logika obu editorów: parsowanie formatu/maski, wykrywanie sekcji czasu, doczepianie CSS-class blokady kółka.
- `CS\MainDemo.Blazor.Server\Editors\Date\IModelOptionsDateEditMouseWheel.cs` — interfejs rozszerzający globalne `IModelOptions` w Application Model XAF: `BlockDateEditMouseWheelByDefault` i `DateEditMaskCaretMode`.
- `CS\MainDemo.Blazor.Server\Editors\Date\IModelMemberViewItemMouseWheel.cs` — interfejs rozszerzający `IModelMemberViewItem`: nullable `BlockMouseWheel` dla pojedynczego pola.
- `CS\MainDemo.Blazor.Server\Editors\Date\DateEditorCssAliases.cs` — stałe nazw CSS class używanych przez editor i konsumowanych przez JS-marker.
- `CS\MainDemo.Blazor.Server\Editors\Date\DateEditMouseWheelGuardController.cs` — kontroler XAF, który przy każdym widoku ładuje JS-moduł blokujący scroll i rejestruje globalny listener.
- `CS\MainDemo.Blazor.Server\wwwroot\js\maindemo-date-edit-wheel-guard.js` — moduł ES z idempotentnym `ensureRegistered()`, który dorzuca jeden `wheel` listener w capture phase.

## Trzy poziomy konfiguracji blokady kółka

Konfigurator sprawdza kolejno (pierwsza znaleziona wartość wygrywa):

1. **Atrybut na property** — `[DateEditMouseWheel(false)]` na konkretnym property w business objectcie wyłącza blokadę dla tego pola. To opcja dla developera, który wie z góry, że dane pole ma być scrollowalne.
2. **`IModelMemberViewItemMouseWheel.BlockMouseWheel`** — nullable bool ustawiany w Model Editor per ViewItem. Pozwala wyłączyć blokadę dla konkretnego pola w konkretnym widoku bez dotykania kodu. Wartość `null` oznacza „spadnij do poziomu globalnego".
3. **`IModelOptionsDateEditMouseWheel.BlockDateEditMouseWheelByDefault`** — globalna wartość domyślna dla całej aplikacji. Default `true`, bo zazwyczaj chcemy blokować.

Jeśli żaden poziom nic nie ustawi, blokada jest włączona.

## `MaskCaretMode`

Konfiguracja na poziomie globalnym przez `IModelOptionsDateEditMouseWheel.DateEditMaskCaretMode`. Default `Advancing`. Ustawiana w `DxDateEditMaskProperties.{DateTime,DateOnly,DateTimeOffset}.CaretMode` przy każdym tworzeniu kontrolki — **uwaga: to globalny statyczny stan DevExpress**. Dziś źródłem jest jeden interfejs `IModelOptions`, więc nieszkodliwe; gdyby kiedyś trzeba było per-View, ostatnio otwarty widok wygra dla wszystkich.

## Wykrywanie sekcji czasu z formatu

`MainDemoDateTimeEditorConfigurator.IncludesTimeSection(format)` parsuje `EditMask` / `DisplayFormat` z modelu, usuwa cytowane literały (`'r.'`, `"r."`, escapowane znaki), i sprawdza czy w pozostałych tokenach jest jeden z: `H h s t f F K z` lub samodzielne `m` w masce dłuższej niż 1 znak. Standardowe formaty `f F g G o O r R s t T u U` traktuje jako mające czas. To pozwala editorowi automatycznie włączać `TimeSectionVisible` tylko dla pól z czasem, niezależnie od tego czy w xafml siedzi `DisplayFormat="dd.MM.yyyy HH:mm"`, czy `EditMask="g"`.

## Rejestracja w `BlazorModule`

XAF nie zobaczy interfejsów modelu, dopóki nie zarejestrujemy ich w `ExtendModelInterfaces`:

```csharp
public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders) {
    base.ExtendModelInterfaces(extenders);
    extenders.Add<IModelOptions, IModelOptionsDateEditMouseWheel>();
    extenders.Add<IModelMemberViewItem, IModelMemberViewItemMouseWheel>();
}
```

Po tym Application Model dostaje na `Application.Options` dwa nowe property (sekcja `Behavior`), a każdy `IModelMemberViewItem` dostaje `BlockMouseWheel`.

## Blokada kółka po stronie JS

`maindemo-date-edit-wheel-guard.js` rejestruje **jeden** listener globalny w capture phase:

```javascript
document.addEventListener('wheel', function (e) {
    const target = e.target;
    if (!target || typeof target.closest !== 'function') return;
    if (target.closest('.maindemo-dateedit-wheel-allowed')) return;
    if (target.closest('.maindemo-dateedit-wheel-blocked')) {
        e.preventDefault();
        e.stopImmediatePropagation();
    }
}, { capture: true, passive: false });
```

Kluczowe parametry:

- `capture: true` — listener łapie zdarzenie **przed** DevExpress, w fazie capture. Bez tego DevExpress dostaje wheel pierwszy i zmienia wartość zanim my zdążymy `preventDefault`.
- `passive: false` — nowoczesne przeglądarki domyślnie traktują `wheel` jako passive (`preventDefault` jest ignorowane). Trzeba jawnie wymusić non-passive.
- `stopImmediatePropagation()` — zatrzymuje też inne listenery na tym samym elemencie.
- Selektory `.maindemo-dateedit-wheel-allowed` / `.maindemo-dateedit-wheel-blocked` — celują wyłącznie w nasze markery. Niezależne od wewnętrznych klas DevExpress (`dxbl-dateedit` itp.), które się zmieniają między wersjami.

Moduł jest ładowany przez kontroler `DateEditMouseWheelGuardController` przez `IJSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/maindemo-date-edit-wheel-guard.js")` na każdym widoku. Funkcja `ensureRegistered()` jest idempotentna — kolejne wywołania są no-op, listener rejestrowany tylko raz. Dzięki temu nie ma `<script>` taga w `_Host.cshtml` (kiedyś było, ale wymagało dodatkowej koordynacji kolejności względem `blazor.server.js`).

## Jak to działa razem

1. Developer dodaje pole `DateTime?` w klasie biznesowej. Edytor `MainDemoDateTimeEditor` jest zarejestrowany jako `isDefaultEditor: true`, więc XAF używa go automatycznie dla wszystkich `DateTime` / `DateTime?` bez potrzeby `[EditorAlias]`.
2. `OnControlCreated` ustawia `MaskCaretMode` (globalnie) i wywołuje `Configurator.Configure(adapter, Model)`.
3. Configurator parsuje `EditMask` / `DisplayFormat` z modelu, ustawia `Mask` / `Format` / `DisplayFormat` na adapterze, decyduje o `TimeSectionVisible` i doczepia odpowiednią klasę CSS (`maindemo-dateedit-wheel-blocked` albo `maindemo-dateedit-wheel-allowed`) do `CssClass` i `InputCssClass`.
4. `DateEditMouseWheelGuardController` na `OnViewControlsCreated` woła `ensureRegistered()` z modułu JS. Listener globalny jest rejestrowany raz na sesję.
5. Użytkownik scrolluje. Listener sprawdza, czy target jest pod elementem z `.maindemo-dateedit-wheel-blocked` (i nie pod `-allowed` — gdyby kiedyś trzeba było zagnieżdżone wyjątki). Jeśli tak, robi `preventDefault` + `stopImmediatePropagation`.
6. DevExpress nie dostaje wheel-a, wartość się nie zmienia.

## Jak developer wyłącza blokadę dla jednego pola

Trzy opcje, w kolejności od najbardziej do najmniej trwałej:

**Atrybutem na property:**

```csharp
[DateEditMouseWheel(false)]
public virtual DateTime? KiedyZrobione { get; set; }
```

**Z Model Editora (per ViewItem):**

```
Application Model → Views → DemoTask_DetailView → Items → DueDate
   Property: BlockMouseWheel = False
```

**Globalnie dla całej aplikacji:**

```
Application Model → Options
   Property: BlockDateEditMouseWheelByDefault = False
```

### Żywy przykład: `Employee.Birthday`

W tym projekcie pole `Employee.Birthday` ma świadomie odblokowany scroll. Powód: data urodzenia to często edycja „cofnij o kilka lat" — wygodniej przewinąć rok kółkiem niż wpisywać go ręcznie. Pozostałe pola daty w aplikacji (np. `DemoTask.DueDate`, `DemoTask.StartDate`) zostają zablokowane, bo to typowo „dziś plus parę dni" i scroll przeszkadza.

Zrobione zostało wariantem atrybutowym — wybór nie jest case-by-case decyzją operatora, tylko świadomą decyzją modelu domenowego, więc lepiej trzymać ją w kodzie niż w xafml:

```csharp
// CS\MainDemo.Module\BusinessObjects\Employee.cs
using MainDemo.Module.Editors;

// ...

[DateEditMouseWheel(false)]
public virtual DateTime? Birthday { get; set; }
```

Po stronie UI editor automatycznie podpina klasę CSS `maindemo-dateedit-wheel-allowed` zamiast `-blocked` do tej konkretnej kontrolki. Globalny listener wheel widzi `.maindemo-dateedit-wheel-allowed`, robi `return` przed czekiem na `.maindemo-dateedit-wheel-blocked`, i scroll przelatuje do DevExpressa normalnie. Reszta pól daty w widoku detail (`Anniversary` w tym samym widoku, daty w `Tasks` itp.) pozostaje zablokowana, bo nie mają tego atrybutu.

Weryfikacja w przeglądarce: F12 → Elements → znajdź input daty urodzenia → potwierdź, że ma klasę `maindemo-dateedit-wheel-allowed` (a nie `-blocked`). W konsoli:

```javascript
document.querySelectorAll('.maindemo-dateedit-wheel-allowed').length
```

powinno zwrócić ≥ 1 na widoku z polem `Birthday`.

Gdyby decyzja była bardziej kontekstowa — np. „w widoku rekrutera scroll na Birthday OK, w widoku HR-owca nie" — wtedy lepszy byłby wariant z Model Editora per ViewItem, bo różne widoki tej samej klasy mogą wpisać różne `BlockMouseWheel`. Atrybut na property dotyczy wszystkich widoków jednakowo, model jest twardszy niż xafml.

## Pułapki

- **Nie zapomnij o `ExtendModelInterfaces`**. Bez rejestracji w `BlazorModule` interfejsy istnieją, ale Model Editor ich nie zobaczy. Wszystkie pola będą blokować kółko (default `true`) i nie ma jak tego wyłączyć z modelu, tylko z kodu (atrybutem).
- **`EnableDefaultItems=false` w obu csproj**. Każdy nowy `.cs` musi być dorzucony do `<Compile Include>`. W `MainDemo.Module.csproj` jest glob `<Compile Include="Editors\**\*.cs" />`. W `MainDemo.Blazor.Server.csproj` jest glob `<Compile Include="Editors\Date\**\*.cs" />`. Dorzucanie kolejnych editorów do tych podfolderów działa automatycznie; nowy folder na poziomie `Editors\` wymaga osobnego wpisu.
- **`{ passive: false }` musi być jawnie**. Bez tego `preventDefault` jest cicho zignorowane i wartość się dalej zmienia.
- **`capture: true` ma znaczenie**. Bez tego DevExpress czyta wheel pierwszy.
- **`DxDateEditMaskProperties.*.CaretMode` to globalny stan DevExpress**. Ustawienie go w `OnControlCreated` per kontrolka działa, ale nadpisuje wartość dla **wszystkich** `DxDateEdit` w aplikacji. Dziś nieszkodliwe, bo źródłem jest jedna globalna wartość; gdyby kiedyś trzeba było per-View, trzeba to wyciągnąć z editora.
- **Konflikt nazwy `EditorAliases`**. W DevExpress jest `DevExpress.ExpressApp.Editors.EditorAliases`. Nasza klasa `MainDemo.Module.Editors.EditorAliases` koliduje przez nazwę — używamy aliasu using `using EditorAliases = MainDemo.Module.Editors.EditorAliases;` po wzorcu z `OutlookInspiredDemo`.

## Jak dodać kolejny custom editor zgodny z tym pattern

1. Stałą aliasu (publiczny string-literał używany przez `[PropertyEditor]` i `[EditorAlias]`) dodaj do `CS\MainDemo.Module\Editors\EditorAliases.cs`. Wartość literału ustawia raz na zawsze — zmiana wymaga migracji wszystkich `Model.xafml`.
2. Klasy edytora i ich helpery wkładaj do nowego folderu `CS\MainDemo.Blazor.Server\Editors\<Name>\`. Namespace `MainDemo.Blazor.Server.Editors.<Name>`.
3. Atrybuty deklaratywne (bez zależności od UI) wrzucaj do `MainDemo.Module\Editors\`. Interfejsy modelu (`IModelOptions*`, `IModelMemberViewItem*`) zostawiaj w `Blazor.Server`, dopóki tylko Blazor je honoruje.
4. Jeśli editor potrzebuje JS, używaj wzorca z `DateEditMouseWheelGuardController` — ładuj jako moduł ES przez `IJSRuntime`, funkcja `ensureRegistered()` idempotentna. Unikaj inline `<script>` w `_Host.cshtml`.
5. Rejestruj rozszerzenia modelu w `BlazorModule.ExtendModelInterfaces` z usingiem na nowy namespace `MainDemo.Blazor.Server.Editors.<Name>`.

## Lista plików

**Powstałe przy tej zmianie / refaktorze (układ docelowy):**

- `CS\MainDemo.Module\Editors\EditorAliases.cs`
- `CS\MainDemo.Module\Editors\DateEditMouseWheelAttribute.cs`
- `CS\MainDemo.Blazor.Server\Editors\Date\MainDemoDateTimeEditor.cs`
- `CS\MainDemo.Blazor.Server\Editors\Date\MainDemoNullableDateTimeEditor.cs`
- `CS\MainDemo.Blazor.Server\Editors\Date\MainDemoDateTimeEditorConfigurator.cs`
- `CS\MainDemo.Blazor.Server\Editors\Date\IModelOptionsDateEditMouseWheel.cs`
- `CS\MainDemo.Blazor.Server\Editors\Date\IModelMemberViewItemMouseWheel.cs`
- `CS\MainDemo.Blazor.Server\Editors\Date\DateEditorCssAliases.cs`
- `CS\MainDemo.Blazor.Server\Editors\Date\DateEditMouseWheelGuardController.cs`
- `CS\MainDemo.Blazor.Server\wwwroot\js\maindemo-date-edit-wheel-guard.js`

**Zmodyfikowane:**

- `CS\MainDemo.Blazor.Server\BlazorModule.cs` — rejestracja rozszerzeń modelu
- `CS\MainDemo.Blazor.Server\MainDemo.Blazor.Server.csproj` — glob `Editors\Date\**\*.cs`
- `CS\MainDemo.Module\MainDemo.Module.csproj` — glob `Editors\**\*.cs`

# Plan refaktoru `DateEditor` — porządkowanie po wzorcu z `OutlookInspiredDemo`

Ten plan opisuje co i w jakiej kolejności chcemy zrobić w `MainDemo.NET.EFCore`, żeby uporządkować nasz custom DateEditor po wzorcu (organizacyjnym, nie funkcjonalnym) z `OutlookInspiredDemo` / `DataDrive.Module`. Funkcjonalnie nasz edytor robi **więcej** niż cokolwiek w OID (OID nie ma żadnego custom DateEditora) — zmiany są wyłącznie strukturalne i higieniczne. Plik wynikowy: ten dokument plus konkretne zmiany w kodzie zgodnie z poniższą listą zadań.

## Cel

1. Wyciągnąć stałe `EditorAlias` z projektu Blazor.Server do `MainDemo.Module`, tak jak OID trzyma je w `DataDrive.Module/EditorAliases.cs`. Dzięki temu business objecty z modułu będą mogły referować alias przez stałą (`[EditorAlias(EditorAliases.MainDemoDateTime)]`) zamiast hardkodować literał.
2. Rozbić monolitowy plik `DateEditor.cs` (184 linie, 6 odpowiedzialności w jednym pliku) na strukturę folderową `Editors/Date/`, w której każdy plik ma jedną odpowiedzialność — tak samo jak OID trzyma `Editors/HyperLink/`, `Editors/Label/`, `Editors/HtmlEditor/`.
3. Usunąć drobne zapachy (puste `catch`, mutacja statyki DevExpress per-view, niespójna nazwa pliku vs klas) tak, żeby kod był utrzymywalny w dłuższym horyzoncie.
4. Zachować **całe obecne zachowanie** edytora — wszystkie trzy poziomy konfiguracji (atrybut, ViewItem, IModelOptions), wykrywanie sekcji czasu z formatu, blokadę kółka przez CSS + JS interop. Refaktor nie zmienia API i nie zmienia tego, co użytkownik widzi na ekranie.

## Czego ten refaktor NIE obejmuje

- Dodawania edytora w `MainDemo.Win` (WinForms) — to osobny temat. Dziś WinForms używa standardowego DevExpress DateEdit i nikt nie zgłaszał problemu z kółkiem. Jeśli kiedyś dojdzie, atrybut `DateEditMouseWheelAttribute` przeniesiony do Module będzie już gotowy do wykorzystania, a interfejsy modelu pozostają Blazor-only (OID nie ma analogicznej potrzeby).
- Zmiany logiki blokady scrolla. JS module `maindemo-date-edit-wheel-guard.js` i kontroler `DateEditMouseWheelGuardController` zostają bez zmian funkcjonalnych — tylko porządkujemy obsługę błędów.
- Aktualizacji istniejącej dokumentacji `docs/custom-date-editor-mouse-wheel.md`. Tamten plik opisuje "jak to działa funkcjonalnie" i zostanie zaktualizowany **po** zakończeniu refaktoru, w osobnym kroku, żeby nie mieszać planu z gotową dokumentacją.

## Priorytety

Zmiany są podzielone na trzy etapy. Każdy etap jest osobno mergowalny — można zatrzymać się po dowolnym z nich, jeśli czas/uwaga się skończy, i repozytorium zostaje w spójnym stanie.

### Priorytet 1 — wartość architektoniczna, małe ryzyko

Wyciągnięcie stałych aliasów do `MainDemo.Module`. To zmiana, której zysk rośnie z czasem (każdy kolejny custom editor będzie szedł po tym samym pattern), a ryzyko jest minimalne, bo modyfikuje tylko widoczność stałej stringowej, nie jej wartość.

### Priorytet 2 — czytelność i utrzymanie, średnie ryzyko

Rozbicie `DateEditor.cs` na folder `Editors/Date/`. Ryzyko polega głównie na tym, że trzeba poprawnie zaktualizować `MainDemo.Blazor.Server.csproj` (projekt ma `EnableDefaultItems=false`) i ostrożnie przenieść klasy bez zmiany ich publicznego API.

### Priorytet 3 — drobiazgi higieniczne, niskie ryzyko

Puste `catch`, mutacja statyki DevExpress, nazwy plików. Każde z tych zadań samo w sobie jest <30 minut pracy, ale bez priorytetów 1-2 są przedwczesnym kosmetykiem, więc robimy je na końcu.

## Lista zadań

### Etap 1 — stałe aliasów do Module

- [ ] **1.1.** Utworzyć plik `CS/MainDemo.Module/Editors/EditorAliases.cs` z `public static class EditorAliases` zawierającym jedno pole: `public const string MainDemoDateTimeEditor = "MainDemoDateTimeEditor"`. Wartość stringa musi być identyczna z obecnym `CustomEditorAliases.DateTimeEditor`, żeby istniejące rekordy `Model.xafml` nadal się rozwiązywały.
- [ ] **1.2.** Zaktualizować `CS/MainDemo.Module/MainDemo.Module.csproj`: dodać `<Compile Include="Editors\**\*.cs" />` w sekcji `ItemGroup` z `<Compile>` (linia ~62). Wzorzec katalogowy jest zgodny z istniejącymi wpisami (`BusinessObjects\**\*.cs`, `Controllers\**\*.cs`).
- [ ] **1.3.** W `CS/MainDemo.Blazor.Server/Editors/DateEditor.cs` usunąć z `CustomEditorAliases` pole `DateTimeEditor` (zostaje w Module) i podmienić oba użycia `[PropertyEditor(typeof(DateTime), CustomEditorAliases.DateTimeEditor, true)]` na `[PropertyEditor(typeof(DateTime), EditorAliases.MainDemoDateTimeEditor, true)]` (z `using MainDemo.Module.Editors;`). CSS-classy (`MouseWheelBlockerCssClass`, `MouseWheelAllowedCssClass`) zostają w `CustomEditorAliases` — to detal Blazor-only, nie powinny zaśmiecać Module.
- [ ] **1.4.** Zbudować całe rozwiązanie (`dotnet build CS\MainDemo.NET.EFCore.sln -c Debug`) i potwierdzić zero błędów. `TreatWarningsAsErrors=true` w Blazor.Server wyłapie ewentualne nieużywane `using`.
- [ ] **1.5.** Uruchomić Blazor (`dotnet run --project CS\MainDemo.Blazor.Server\MainDemo.Blazor.Server.csproj -c Debug --urls http://localhost:5115`), otworzyć detail view `Employee` lub `DemoTask` z polem daty, kliknąć w date editor i przewinąć kółkiem — wartość nie może się zmieniać. To regresja: dowodzi, że alias nadal się rozwiązuje na nasz custom editor.

### Etap 2 — rozbicie monolitu na strukturę folderową

- [ ] **2.1.** Utworzyć folder `CS/MainDemo.Blazor.Server/Editors/Date/`.
- [ ] **2.2.** Wyciągnąć atrybut `DateEditMouseWheelAttribute` do `CS/MainDemo.Module/Editors/DateEditMouseWheelAttribute.cs`. Atrybut nie zależy od Blazora ani od żadnego typu DevExpress (`AttributeTargets.Property`, jeden boolean), więc bezpiecznie żyje w Module obok `EditorAliases`. Business objecty w Module będą mogły go używać bez referencji do Blazor.Server.
- [ ] **2.3.** Przenieść `IModelOptionsDateEditMouseWheel` do `CS/MainDemo.Blazor.Server/Editors/Date/IModelOptionsDateEditMouseWheel.cs`. **Zostawiamy w Blazor.Server**, bo dziś tylko Blazor honoruje te opcje przez Model Editor — przeniesienie do Module byłoby przedwczesną abstrakcją (per `CLAUDE.md` „Don't design for hypothetical future requirements").
- [ ] **2.4.** Przenieść `IModelMemberViewItemMouseWheel` do `CS/MainDemo.Blazor.Server/Editors/Date/IModelMemberViewItemMouseWheel.cs`. Z tych samych powodów co 2.3.
- [ ] **2.5.** Przenieść klasę `MainDemoDateTimeEditor` (dla `DateTime`) do `CS/MainDemo.Blazor.Server/Editors/Date/MainDemoDateTimeEditor.cs`. Nazwa pliku zgodna z nazwą klasy — to rozwiązuje obecną niespójność (plik `DateEditor.cs`, klasa `MainDemoDateTimeEditor`).
- [ ] **2.6.** Przenieść klasę `MainDemoNullableDateTimeEditor` (dla `DateTime?`) do `CS/MainDemo.Blazor.Server/Editors/Date/MainDemoNullableDateTimeEditor.cs`.
- [ ] **2.7.** Przenieść statyczny `MainDemoDateTimeEditorConfigurator` do `CS/MainDemo.Blazor.Server/Editors/Date/MainDemoDateTimeEditorConfigurator.cs`. Jeśli chcemy iść w pełną zgodność z OID, sensowne byłoby zostawienie go jako `internal`, ale nie zmieniajmy widoczności w ramach tego refaktoru — niech zmiana będzie czysto strukturalna.
- [ ] **2.8.** Przenieść `DateEditMouseWheelGuardController` z `Editors/DateEditMouseWheelGuardController.cs` do `Editors/Date/DateEditMouseWheelGuardController.cs`. Kontroler jest tematycznie częścią pakietu „Date editor", więc trzymanie go w tym samym folderze co reszta upraszcza nawigację.
- [ ] **2.9.** Pozostawić w `CS/MainDemo.Blazor.Server/Editors/DateEditor.cs` tylko klasę `CustomEditorAliases` (z polami CSS-classy) lub przenieść ją do `Editors/Date/DateEditorCssAliases.cs` i usunąć `DateEditor.cs` w całości. Drugi wariant jest czystszy — zalecam ten.
- [ ] **2.10.** Zaktualizować `CS/MainDemo.Blazor.Server/MainDemo.Blazor.Server.csproj`: jeśli `<Compile>` używa explicit list (sprawdzić przed zmianą — projekt ma `EnableDefaultItems=false`), dodać wpis dla `Editors\Date\**\*.cs` lub `Editors\**\*.cs`. Usunąć stary wpis dla `Editors\DateEditor.cs` i `Editors\DateEditMouseWheelGuardController.cs` jeśli występują.
- [ ] **2.11.** Sprawdzić wszystkie `using`-i w przeniesionych plikach. Każda nowa klasa potrzebuje minimalnego zestawu usingów; zostawienie zbędnych zaśmieca, brakujących — nie kompiluje.
- [ ] **2.12.** Build całego solution. Smoke test w przeglądarce identyczny jak w 1.5.

### Etap 3 — higiena

- [ ] **3.1.** W `DateEditMouseWheelGuardController.RegisterWheelGuard()` (obecnie `Editors/DateEditMouseWheelGuardController.cs:31`, po Etapie 2: `Editors/Date/DateEditMouseWheelGuardController.cs`) zastąpić pusty `catch { }` przez `catch (JSException ex) { Tracing.Tracer.LogError(ex); }`. Pusty `catch` ukrywa też błędy nie-JS (np. NullReferenceException w naszym kodzie), które chcemy widzieć w logach.
- [ ] **3.2.** W `MainDemoDateTimeEditorConfigurator.ConfigureMaskCaretMode` (na obu klasach edytora) skomentować jednoznacznie, że `DxDateEditMaskProperties.DateTime.CaretMode = caretMode` mutuje **globalny statyczny stan DevExpress**. Dziś źródłem jest jeden `IModelOptions.DateEditMaskCaretMode`, więc nieszkodliwe, ale komentarz powinien ostrzec przyszłego developera, że jeśli kiedyś trafi tam wartość per-View, ostatnio otwarty widok wygra dla wszystkich. Alternatywa: przenieść tę inicjalizację jednorazowo do `MainDemoBlazorModule.Setup` — wtedy nie ma mutacji per-render w ogóle. Zdecyduj który wariant; rekomenduję komentarz teraz, bo nie wiemy czy w przyszłości nie będziemy chcieli różnych trybów per-View.
- [ ] **3.3.** Sprawdzić, czy w `Model.xafml` (Blazor.Server i Module) nie ma już nigdzie pozostałości starych nazw aliasów (`DateEditor`, `DateEditorNullable` z dokumentacji historycznej). Jeśli są — zaktualizować na `MainDemoDateTimeEditor`. Jeśli nie ma — zostawić.
- [ ] **3.4.** Zbudować, uruchomić, ręcznie sprawdzić scroll i Tab-keying w maskach daty na trzech polach: `Employee.Birthday` (DateOnly-style), `DemoTask.StartDate` (DateTime z czasem), oraz polu, na którym celowo wyłączyliśmy blokadę przez Model Editor (jeśli takie istnieje; jeśli nie — zostawić nieprzetestowane i zostawić notatkę w PR-ze).

### Etap 4 — dokumentacja (po wszystkim)

- [ ] **4.1.** Zaktualizować `docs/custom-date-editor-mouse-wheel.md`: nowe ścieżki plików, nowe nazwy aliasów (jeśli się zmieniły), wzmianka o tym, że `EditorAliases` siedzi w Module. Stary tekst opisuje pliki z lokalizacji sprzed refaktoru — bez aktualizacji wprowadzi w błąd.
- [ ] **4.2.** Dodać krótką sekcję „Jak dodać kolejny custom editor zgodny z tym pattern" — wzór `Editors/<Name>/` + alias w Module + ewentualne CSS-classy w Blazor.Server. To poradnik dla przyszłego developera, który chce iść po naszych śladach.

## Ryzyka

- **`EnableDefaultItems=false` w obu csproj** — najłatwiej tu się potknąć. Dodanie pliku bez aktualizacji `.csproj` powoduje, że klasa nie kompiluje się do assembly, ale `dotnet build` nie zgłosi tego błędu w przyjazny sposób (po prostu typ „nie istnieje"). Sprawdzić każdy nowy plik przez `dotnet build` natychmiast po dodaniu.
- **Model.xafml różnic użytkownika** — jeśli ktoś (np. Twoja działająca instalacja) ma w bazie diff modelu, który referuje `CustomEditorAliases.DateTimeEditor` przez nazwę typu (a nie wartość stałej), ta nazwa typu się nie zmienia (literał stringa jest ten sam `"MainDemoDateTimeEditor"`). Ale gdyby zmieniła się wartość stałej — różnice modelu osierocą. **Dlatego krok 1.1 wymaga zachowania identycznej wartości literału**.
- **`TreatWarningsAsErrors=true` w Blazor.Server** — usuwanie usingów po przenoszeniu klas musi być chirurgiczne. Każdy nieużywany `using` to błąd kompilacji.
- **Nullable disable w Module** — `MainDemo.Module.csproj` ma `<Nullable>disable</Nullable>`, więc atrybut przenoszony w 2.2 nie może używać `T?` ani innych nullable-tylko-z-włączonym-trybem konstrukcji. Atrybut jako taki to plain class z bool property — nie ma problemu.

## Definicja „done"

Refaktor uznajemy za zakończony, gdy:

1. Wszystkie zadania z etapów 1–3 są wykonane.
2. `dotnet build CS\MainDemo.NET.EFCore.sln -c Debug` przechodzi bez ostrzeżeń i bez błędów.
3. `dotnet test CS\MainDemo.WebAPI.Tests\MainDemo.WebAPI.Tests.csproj -c Debug` zielony (jeśli były zielone przed — nie powinniśmy ich popsuć refaktorem UI).
4. Ręczny smoke test w przeglądarce: scroll na polu daty nie zmienia wartości, Tab/strzałki działają normalnie, w polach z czasem widać sekcję czasu.
5. Etap 4 (dokumentacja) zaktualizowany.

## Otwarte pytania do decyzji

- Czy `MainDemoDateTimeEditorConfigurator` ma zostać `internal` (jak typowy worker), czy `public`? Dziś jest `internal static`. Refaktor go nie ruszamy, ale warto wiedzieć, czy planujemy go kiedyś używać z innego assembly.
- Czy zostawiamy nazwę aliasu `"MainDemoDateTimeEditor"`, czy skracamy do `"DateTimeEditor"`? Krótsza wersja jest czytelniejsza, ale wymaga aktualizacji wszystkich `Model.xafml`, które już to referują. **Rekomenduję zostawić** — wartość niezmieniona to zero ryzyka migracji modelu.
- Czy pakiet `Editors/Date/` powinien dostać własny `ReadMe.md` lokalnie obok kodu (na wzór `BusinessObjects/ReadMe.txt`), czy wystarczy `docs/custom-date-editor-mouse-wheel.md`? Skłaniam się ku „wystarczy docs/" — projekt ma już ustaloną konwencję, że długie dokumenty trafiają do `docs/`.

# Plan aktualizacji `docs/custom-date-editor-mouse-wheel.md`

Ten plan opisuje, co dopisać i co rozbudować w dokumencie opisującym custom DateEditor po refaktorze. Punkt wyjścia: dokument po przepisaniu w ramach Etapu 4 planu refaktoru (`docs/refaktor-dateeditor-plan.md`). Stan obecny ma 190 linii, dwanaście sekcji, jest spójny, ale niektóre miejsca są zbyt szkicowe, brakuje kilku ról dla różnych czytelników, i prawie nie ma snippetów kodu.

## Decyzja: aktualizacja, nie przepisywanie

Bieżąca struktura dokumentu jest dobra i nie wymaga przebudowy. Tytuł, motywacja, lista plików, kaskada konfiguracji, walkthrough, pułapki i sekcja „jak dodać kolejny editor" są w odpowiednim porządku, czyta się to linearnie. Styl (polski, pełne zdania, dwuczęściowe argumenty „dlatego X, bo Y") jest spójny i czytelny.

Przepisywanie miałoby sens tylko gdybyśmy chcieli zmienić **audytorium docelowe** — np. zrobić z tego porting tutorial („krok po kroku, jak wziąć ten pattern do swojego XAF Blazor projektu") zamiast referencji opisującej co tu jest. Dziś dokument próbuje obsłużyć obie role naraz i częściowo z tego powodu jest płytki. Alternatywą zamiast przepisywania jest **rozszczepienie** na dwa dokumenty (referencja + porting tutorial) — ale to też więcej pracy niż uzupełnienie obecnej wersji o brakujące fragmenty.

Rekomendacja: zostać przy jednym dokumencie i uzupełnić go zgodnie z poniższą listą. Jeśli w trakcie pracy okaże się, że sekcja „Krok po kroku — jak zaaplikować ten pattern w innym projekcie" rozrasta się bardzo (powyżej ~80 linii), wtedy rozważyć wydzielenie do osobnego `docs/porting-date-editor.md`.

## Priorytety

Trzy etapy, każdy osobno mergowalny. Numeracja kontynuuje przedstawienie z `refaktor-dateeditor-plan.md` — tu po prostu osobny dokument, ale pasują podobne zasady.

### Priorytet 1 — kod, którego dziś nie ma w dokumencie

Bez snippetów kodu dokument jest „opowieścią o kodzie", nie „dokumentacją kodu". Czytelnik musi otwierać 9 plików, żeby zweryfikować, że rozumie poprawnie. Każda kluczowa decyzja powinna mieć przed sobą snippet, do którego prozaiczny opis się odnosi.

### Priorytet 2 — nowe sekcje dla niepokrytych ról czytelnika

Dziś dokument obsługuje dobrze rolę „rozszerzam pattern" i „audytuję kod". Brakuje: „debugguję, dlaczego nie działa", „testuję, że działa" i „przenoszę do innego projektu".

### Priorytet 3 — drobiazgi i precyzja

Niejasności w obecnym tekście (np. „idempotent JS module" — idempotent dla czego? dla circuit? dla taba?), nieprecyzyjne stwierdzenia, brakujące case'y dla grid inline edit i popup widoków.

## Lista zadań

### Etap 1 — dodać snippety kodu w istniejących sekcjach

- [ ] **1.1.** W sekcji **Struktura plików** po liście plików dodać sub-sekcję **„Co siedzi w każdym pliku — w skrócie"** z 3-5-liniowym snippetem szkieletu kluczowych klas. Wzorzec: nazwa pliku, jeden snippet, jedno zdanie pointujące do tego, co tam jest najważniejsze.
  - `EditorAliases.cs` — pokazać dosłownie zawartość, dwie linie.
  - `MainDemoDateTimeEditor.cs` — szkielet klasy: atrybut `[PropertyEditor(...)]`, `OnControlCreated`, wywołanie `MainDemoDateTimeEditorConfigurator.Configure`.
  - `IModelOptionsDateEditMouseWheel.cs` — sygnatura interfejsu z dwoma property.
  - `DateEditMouseWheelGuardController.cs` — ten ma już snippet w obecnej wersji (cały JS), ale brakuje strony C# — dodać `OnViewControlsCreated` z wywołaniem `import`.
- [ ] **1.2.** W sekcji **Trzy poziomy konfiguracji blokady kółka** każdy z trzech bullet pointów dostaje snippet pokazujący użycie:
  - Atrybut — `[DateEditMouseWheel(false)] public virtual DateTime? DueDate { get; set; }`
  - Per ViewItem — fragment `Model.xafml` z `<MemberViewItem Id="DueDate" BlockMouseWheel="false" />`
  - Globalnie — fragment `Model.xafml` z `<Options BlockDateEditMouseWheelByDefault="false" />`

  Każdy snippet wpisany pod własnym bullet pointem, krótko (3 linie max). Bez tego czytelnik nie wie, gdzie konkretnie wpisać te wartości.
- [ ] **1.3.** W sekcji **Wykrywanie sekcji czasu z formatu** dorzucić tabelę przykładów: `Format → hasTime`. Trzy-cztery wiersze: `dd.MM.yyyy → false`, `dd.MM.yyyy HH:mm → true`, `g → true`, `{0:HH:mm} → true (po normalizacji)`. Pokazuje od razu, jakie inputy są obsłużone, bez schodzenia do kodu konfiguratora.
- [ ] **1.4.** W sekcji **Blokada kółka po stronie JS** uzupełnić snippet o **pełne ciało** modułu (jest tylko sam listener), tj. razem z linijką `let registered = false;` i `export function ensureRegistered()`. Bez tego czytelnik widzi listener, ale nie widzi mechanizmu idempotencji, do którego dwa zdania niżej referuje opis.

### Etap 2 — trzy nowe sekcje

- [ ] **2.1.** Sekcja **„Troubleshooting"** wstawiona po **Pułapki**. Każdy wpis to jeden objaw + jedno wyjaśnienie + jedna konkretna komenda/sprawdzenie. Pokrywać:
  - „Scroll dalej zmienia wartość" → sprawdź w devtools, czy `<input>` daty ma klasę `maindemo-dateedit-wheel-blocked`. Brak → editor nie podpiął się do tego pola. Sprawdź `[PropertyEditor]` registration order i czy `MainDemoDateTimeEditor` w ogóle się zarejestrował (breakpoint w `OnControlCreated`).
  - „Klasa jest, ale scroll dalej działa" → sprawdź w konsoli, czy moduł JS się załadował. `window` nie powinno mieć globalu, ale brak `import("./js/maindemo-date-edit-wheel-guard.js")` w Network = nie został zaimportowany. Sprawdź `DateEditMouseWheelGuardController.OnViewControlsCreated` (breakpoint), czy `IJSRuntime` nie jest `null`.
  - „MaskCaretMode nie skacze do następnej sekcji" → wartość `IModelOptionsDateEditMouseWheel.DateEditMaskCaretMode` ustaw przez Model Editor → Options. Default `Advancing`, ale jeśli ktoś zapisał `Static` do diff modelu — wygrywa.
  - „Po wpisaniu daty kursor zostaje w polu zamiast skoczyć do kolejnego pola" → to nie ten sam temat co `MaskCaretMode` (caret porusza się w sekcjach jednej maski). Pole-do-pola obsługuje XAF/Blazor, nie DevExpress mask.
  - „Editor działa na detail view, ale nie na grid inline edit" → na inline edit `Control` w `OnControlCreated` może nie być `DxDateEditModel<T>`. Dorzucić w troubleshooting czek + odsyłacz do sekcji „Edge cases".
  - „W LookupListEditorze data nie ma blokady" — analogicznie.
- [ ] **2.2.** Sekcja **„Edge cases"** po troubleshooting. Cztery przypadki, każdy 4-6 linii prozy plus ewentualny snippet:
  - **Grid inline edit (DxGridListEditor)** — XAF tworzy adapter inny niż w detail view; `Control is DxDateEditModel<T>` może nie zadziałać. Sprawdzić, czy w `MainDemo.Blazor.Server` jest aktywne dla `Employee.Birthday` w grid view. Jeśli nie — opisać, co dalej.
  - **Lookup list editor** — pola daty w lookup oknie, czy editor jest aktywny.
  - **Popup window detail view** — `IJSRuntime` jest tym samym instance, listener już jest zarejestrowany, więc działa. Ale CSS-class musi się doczepić do roota w popupie — sprawdzić w devtools.
  - **Modal dialog z DataGrid** — j.w.
- [ ] **2.3.** Sekcja **„Weryfikacja po wdrożeniu"** przed listą plików (jako sekcja zamykająca implementacyjna). Konkretny checklist do odhaczenia po dodaniu pattern do projektu:
  1. Build solution-a zielony.
  2. `Model.xafml` w Blazor.Server otwiera się w Model Editor bez błędów.
  3. W Model Editor → Options widoczne są property `BlockDateEditMouseWheelByDefault` i `DateEditMaskCaretMode` (jeśli nie — `ExtendModelInterfaces` nie wywołane lub na złym module).
  4. Detail view z polem `DateTime?` (np. `Employee.Birthday`): kliknij w date input, przewiń kółkiem — wartość nie powinna się zmienić. Caret powinien przeskakiwać między sekcjami po dwóch znakach.
  5. W tej samej kontrolce kliknij dropdown — kalendarz się otwiera normalnie, scroll kalendarza działa (bo listener targetuje tylko `.maindemo-dateedit-wheel-blocked`, nie dropdown).
  6. F12 → Console → `document.querySelectorAll('.maindemo-dateedit-wheel-blocked').length` > 0 dla widoku z datą.
  7. Wyłącz na jednym polu przez Model Editor (`BlockMouseWheel = false`) → ten konkretny input dostaje `.maindemo-dateedit-wheel-allowed`, scroll znowu działa, reszta dat w tym widoku jest dalej zablokowana.

### Etap 3 — precyzja i drobiazgi

- [ ] **3.1.** Doprecyzować w sekcji **Blokada kółka po stronie JS**, że zmienna `registered` jest zakresu modułu ESM — czyli pojedynczy import per dokument HTML (per tab przeglądarki), niezależnie od circuita SignalR. Jeśli użytkownik otworzy aplikację w dwóch tabach, każdy ma własny `registered`. Tab odświeżony przeładowuje moduł od nowa. To **nie** jest zmienna per-circuit XAF.
- [ ] **3.2.** Doprecyzować w sekcji **Rejestracja w `BlazorModule`**, dlaczego dwie rejestracje (nie jedna). `IModelOptions` to globalne, `IModelMemberViewItem` to per ViewItem. Bez obu nie ma trzech poziomów konfiguracji.
- [ ] **3.3.** Dorzucić wzmiankę przy **Lista plików** o **DemoTask.cs** i **Employee.cs** jako business objects ilustrujące użycie — to są pliki, które ktoś podglądający chciałby zobaczyć w żywym przykładzie. Wystarczy zdanie typu „W projekcie pole `Employee.Birthday` używa default editora przez `isDefaultEditor: true`; pole `DemoTask.DueDate` analogicznie."
- [ ] **3.4.** W sekcji **Pułapki** dorzucić jeden punkt: **„`MainDemoNullableDateTimeEditor` i `MainDemoDateTimeEditor` są zarejestrowane z `isDefaultEditor: true`"** — oznacza to, że pattern dotyczy **wszystkich** pól `DateTime` / `DateTime?` w aplikacji, nie tylko tych z atrybutem. To różni się od pierwotnej wersji z HIS, gdzie było `false` i wymagało jawnego `[EditorAlias]`. Czytelnik musi wiedzieć, że migrując z HIS-a nie skopiuje 1:1.
- [ ] **3.5.** W sekcji **Jak dodać kolejny custom editor** uzupełnić punkt 2 o uwagi dotyczące dziedziczenia: dla edytorów typo-specyficznych (typu `DateTimePropertyEditor`) trzeba zwykle dwie klasy (T i T?), dla bazowych (`StringPropertyEditor`) wystarczy jedna. To realny mylący moment dla developera idącego po naszych śladach.

### Etap 4 — opcjonalne, do decyzji

- [ ] **4.1.** Czy dorzucić sekcję **„Dlaczego nie WinForms"** — jednoakapitowe wyjaśnienie, że ten pattern jest Blazor-only, bo WinForms `DateEdit` w XAF MainDemo dziś nie zgłaszał problemu z scrollem (różny adapter, różny event handling). Jeśli kiedyś WinForms będzie potrzebować podobnej blokady, atrybut `DateEditMouseWheelAttribute` już jest w Module, czeka.
- [ ] **4.2.** Czy dorzucić **diagram sekwencji** w sekcji „Jak to działa razem". Dziś jest sześcioelementowa lista. ASCII-art diagram mógłby pokazać równolegle: czas (rendering page) → wątek serwera (XAF) → wątek przeglądarki (DevExpress + nasz JS). Plus: ułatwia zrozumienie capture-phase. Minus: ASCII diagramy starzeją się i bywają trudne do utrzymania.
- [ ] **4.3.** Czy zlinkować z poziomu dokumentu do refaktor-plan-u i odwrotnie (interlink), żeby ktoś zaczynający od historii znalazł aktualny stan, i odwrotnie. Wymaga jednego linku na końcu obu dokumentów.

## Styl, który trzymamy

- **Polski**, pełne zdania, dwa argumenty (X, dlatego Y) — bez glib „lepiej X" jednoliniowców.
- **Markdown CommonMark**, snippety w `csharp` / `javascript` / `xml`.
- **Cytowanie plików w nagłówku sekcji** — np. „### `MainDemo.Blazor.Server` — części UI-specyficzne" — zostaje (działa jak inline TOC).
- **Code block ≤ 12 linii** w istniejących sekcjach, ≤ 25 linii w nowej sekcji „Co siedzi w każdym pliku". Nie wklejamy całych klas — wkładamy szkielet, który ilustruje pattern.
- **Backticki dla nazw**: typów, plików, atrybutów, identyfikatorów w Model Editor. Nie wytłuszczamy nazw zmiennych.
- **Nagłówki `##` dla głównych sekcji, `###` dla pod-sekcji**. Trzymać hierarchię — żeby spis treści w VS Code działał.

## Ryzyka

- **Dryf wobec kodu** — najgroźniejsze. Każdy snippet w dokumencie to zobowiązanie: jak kod się zmieni i dokument nie, czytelnik gubi się szybciej niż gdyby snippetu nie było. Reguła: snippet kopiowany z pliku 1:1 z minimalnym kontekstem; jeśli musi być uproszczony, opatrzony komentarzem `// (uproszczone)`.
- **Rozdęcie dokumentu** — jeśli wszystko z Etapu 1+2+3+4 wrzucimy, dokument urośnie do ~400 linii. To granica, po której ludzie czytają tylko TOC. Mitigation: trzymać priorytety; Etap 4 jest opcjonalny.
- **Polski vs angielski w nazwach DevExpress** — `MaskCaretMode.Advancing`, `IModelMemberViewItem` — zostają po angielsku jako identyfikatory. Opisy w prozaie zostają po polsku. Tę konwencję trzymamy, jest spójna z resztą dokumentów (`branding-w-main-demo-blazor.md`, `obsluga-jezyka-polskiego-w-main-demo-blazor.md`).

## Definicja „done"

Aktualizacja uznana za zakończoną, gdy:

1. Wszystkie zadania z Etapu 1, 2, 3 są wykonane.
2. Dokument ma między 280 a 350 linii (obecnie 190, planowane dodanie ~100-150).
3. Każdy snippet kodu kompiluje się 1:1 z aktualnymi plikami w repozytorium (sprawdzić przez wzrokowe porównanie z `Editors/Date/*.cs`).
4. Sekcja troubleshooting ma minimum cztery pozycje, każda z konkretną komendą lub sprawdzeniem (nie samym „sprawdź czy działa").
5. Sekcja weryfikacji ma checklistę ≥ 5 punktów, każdy podlega odhaczeniu (nie ogólnik).
6. Spis treści wygenerowany przez VS Code (lub `markdown-toc`) jest czytelny i pokrywa wszystkie nowe nagłówki.

## Otwarte pytania do decyzji

- **Czy zrobić Etap 4 (opcjonalne dodatki)?** Diagram ASCII i sekcja „dlaczego nie WinForms" są przydatne, ale nie krytyczne. Rekomenduję odłożyć i wracać, jeśli pojawi się pytanie od kogoś trzeciego — wtedy mamy konkretny use case.
- **Czy umieszczać przykłady `Model.xafml`** (zadanie 1.2) w postaci kopiowalnej z atrybutami w pełnej składni XAF, czy w postaci uproszczonej (pseudoxafml)? Sugeruję pełną — łatwiej skopiować, mniej miejsca na pomyłkę.
- **Czy odsyłać do `OutlookInspiredDemo` jako wzorca strukturalnego?** W obecnym dokumencie pojawia się raz, w sekcji „Struktura plików". Można rozbudować w sekcji „Jak dodać kolejny custom editor" o linijkę „Patrz `OutlookInspiredDemo/DataDrive.Blazor.Server/Editors/HyperLink/` jako referencję" — ale tylko jeśli ten projekt jest dostępny dla wszystkich czytelników. Jeśli to repozytorium prywatne, lepiej nie linkować, bo czytelnik dostanie 404.

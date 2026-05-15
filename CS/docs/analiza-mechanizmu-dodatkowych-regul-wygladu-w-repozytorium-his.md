# Analiza mechanizmu dodatkowych reguł wyglądu w repozytorium HIS

## Cel analizy

W repozytorium `C:\Users\Programista\source\repos\HIS` istnieje własny, dodatkowy mechanizm reguł wyglądu, który rozszerza standardowy moduł `ConditionalAppearance` z DevExpress XAF. Ten mechanizm nie ogranicza się do statycznych atrybutów `[Appearance]` zapisanych w klasach biznesowych. Pozwala również definiować reguły w bazie danych, trzymać je w pamięci aplikacji i dokładać je dynamicznie do standardowego `AppearanceController`.

Najkrótsza poprawna odpowiedź na pytanie "jaka klasa o tym decyduje" brzmi następująco:

1. Główną klasą modelującą dodatkową regułę jest `CustomApperance` w pliku `HIS.Module\BusinessObjects\Helpers\CustomApperance.cs`.
2. Główną klasą wykonawczą, która podpina te reguły do XAF w czasie działania, jest `CustomApperanceViewControler` w pliku `HIS.Module\Controllers\HelperControllers\CustomApperanceViewControler.cs`.
3. Dodatkową rolę pełni `CustomApperanceStorage` w pliku `HIS.Module\Storages\CustomApperanceStorage.cs`, ponieważ to on utrzymuje cache reguł w pamięci procesu.

To jednak jest tylko rdzeń. Aby mechanizm działał, trzeba jeszcze skonfigurować moduły XAF, model, uprawnienia, inicjalizację cache oraz w części przypadków osobne renderowanie dla Blazor Grid.

## Architektura rozwiązania

W HIS działają równolegle dwa poziomy reguł wyglądu.

Pierwszy poziom to zwykłe reguły XAF zapisane bezpośrednio w kodzie przy pomocy atrybutu `[Appearance]`. Takich użyć jest bardzo dużo. Wyszukiwanie po repozytorium pokazuje, że atrybuty są rozsiane po wielu obiektach biznesowych, na przykład `Appointment`, `Referral`, `OrganizationSummary`, `PersonSummary` i wielu innych.

Drugi poziom to mechanizm własny, nazwany w kodzie `CustomApperance`. Nazwa zawiera literówkę i jest konsekwentnie używana w całym repozytorium. Nie należy jej poprawiać bez pełnego refaktoru, ponieważ jest już związana z nazwami klas, widoków, tabeli, kontrolerów, migracji i uprawnień.

Ten własny mechanizm działa w następujący sposób:

1. Użytkownik definiuje regułę jako rekord encji `CustomApperance`.
2. Rekord jest zapisywany do tabeli `CustomApperances`.
3. Podczas zapisu obiekt trafia także do pamięci aplikacji przez `CustomApperanceStorage`.
4. Przy starcie aplikacji Blazor cache jest inicjalizowany pełną listą reguł z bazy.
5. Kontroler `CustomApperanceViewControler` nasłuchuje zdarzenia `AppearanceController.CollectAppearanceRules`.
6. Gdy XAF zbiera reguły wyglądu dla bieżącego widoku, kontroler dokłada do nich pasujące rekordy `CustomApperance`.
7. W rezultacie standardowy `AppearanceController` traktuje te reguły prawie tak samo, jak reguły pochodzące z atrybutów `[Appearance]`.

To rozwiązanie jest rozszerzeniem standardowego mechanizmu XAF, a nie jego zamiennikiem.

## Klasa domenowa, która opisuje regułę

Kluczowa definicja znajduje się w `HIS.Module\BusinessObjects\Helpers\CustomApperance.cs`.

Najważniejsze cechy tej klasy są następujące:

1. Klasa dziedziczy po `BaseObject`, więc jest pełnoprawną encją EF Core i obiektem XAF.
2. Klasa implementuje `IAppearanceRuleProperties`, czyli dokładnie ten interfejs, którego oczekuje `AppearanceController`.
3. Klasa implementuje `ICheckedListBoxItemsProvider`, dzięki czemu potrafi dynamicznie podać listę pól możliwych do zaznaczenia w `SelectedProperties`.
4. Klasa jest oznaczona jako:
   - `[DefaultClassOptions]`
   - `[DefaultProperty(nameof(Name))]`
   - `[XafDisplayName("Dodatkowe reguły wyglądu")]`
   - `[ImageName("colorMode")]`

Najważniejsze pola biznesowe tej klasy to:

1. `Name` jako nazwa reguły.
2. `DataType` jako typ obiektu, którego dotyczy reguła.
3. `Criterion` jako warunek XAF Criteria.
4. `SelectedProperties` jako lista pól docelowych.
5. `ItemVisibility` jako ustawienie widoczności.
6. `Priority` jako priorytet reguły.
7. `AppearanceContext` jako kontekst działania reguły.
8. `FontStyle`, `ForeColor`, `BackColor`, `BorderColor`.
9. `ViewId` jako opcjonalne zawężenie reguły do konkretnego widoku.

Technicznie kolory są przechowywane nie jako `Color`, ale jako tekstowe pola:

1. `BackColorString`
2. `ForeColorString`
3. `BorderColorString`

Właściwości `BackColor`, `ForeColor` i `BorderColor` są oznaczone jako `[NotMapped]` i tłumaczą dane pomiędzy `Color` a tekstem w formacie ARGB z prefiksem `#`. To jest ważne, ponieważ:

1. standardowa warstwa trwałości przechowuje tekst,
2. warstwa widoku pracuje na `Color`,
3. Blazor Grid wykorzystuje później `BorderColorString` do ręcznego ustawiania stylu CSS.

## Dlaczego właśnie `CustomApperance` jest centralną klasą

To `CustomApperance` rzeczywiście "decyduje", jakie dodatkowe reguły są dostępne, ponieważ:

1. zawiera komplet danych potrzebnych do oceny reguły,
2. implementuje interfejs oczekiwany przez XAF,
3. jest filtrowana po typie obiektu i `ViewId`,
4. jest źródłem danych dla cache,
5. jest źródłem danych dla reguł wstrzykiwanych do `AppearanceController`,
6. jest źródłem danych dla osobnego mechanizmu obramowania w siatce Blazor.

Sama klasa nie wykonuje jednak oceny reguł. Wykonanie leży po stronie kontrolerów i XAF.

## Klasa wykonawcza, która podpina reguły do XAF

Najważniejszą klasą wykonawczą jest `HIS.Module\Controllers\HelperControllers\CustomApperanceViewControler.cs`.

Ta klasa:

1. dziedziczy po `ObjectViewController<ObjectView, object>`,
2. działa więc szeroko, praktycznie dla wszystkich widoków obiektowych,
3. pobiera z ramki standardowy `AppearanceController`,
4. wywołuje `ResetRulesCache()`,
5. subskrybuje zdarzenie `CollectAppearanceRules`,
6. w zdarzeniu dokłada własne reguły z `CustomApperanceStorage`,
7. po aktywacji odświeża wygląd przez `appearanceController.Refresh()`.

Logika filtrowania jest bardzo konkretna:

1. pobierany jest `View.Id`,
2. pobierana jest nazwa typu z `View.ObjectTypeInfo.Name`,
3. z nazwy usuwany jest sufiks `GlobalConsts.Proxy`,
4. ze storage wybierane są reguły, dla których:
   - `ObjectTypeName == name`,
   - oraz `ViewId` jest puste albo równe bieżącemu `View.Id`.

To oznacza, że mechanizm rozpoznaje dwa poziomy dopasowania:

1. reguła globalna dla typu obiektu,
2. reguła zawężona do konkretnego widoku.

Ta klasa jest faktycznym punktem integracji z `ConditionalAppearance`.

## Rola `CustomApperanceStorage`

Plik `HIS.Module\Storages\CustomApperanceStorage.cs` trzyma listę reguł w pamięci procesu.

Storage ma trzy podstawowe zadania:

1. `InitCustomApperances(List<CustomApperance>)` ładuje pełny stan cache.
2. `PutCustomApperance(CustomApperance)` dodaje lub podmienia rekord po `ID`.
3. `RemoveCustomApperance(CustomApperance)` usuwa rekord z cache.

To rozwiązanie upraszcza działanie kontrolerów, ponieważ nie pytają one bazy przy każdym renderze widoku. Jednocześnie wprowadza ważne ograniczenie architektoniczne: cache jest procesowy i ręcznie odświeżany. Jeżeli aplikacja działa w wielu instancjach, to nie ma tu rozproszonej synchronizacji.

## W jaki sposób encja sama aktualizuje cache

`CustomApperance` ma własne `OnSaving()`.

Podczas zapisu:

1. wywoływane jest `CustomApperanceStorage.PutCustomApperance(this)`,
2. jeżeli obiekt jest oznaczony do usunięcia, wywoływane jest `CustomApperanceStorage.RemoveCustomApperance(this)`.

To jest ważny detal. Autor rozwiązania przyjął, że cache ma być aktualizowany natychmiast przy zmianie obiektu, a nie tylko przy ponownym starcie aplikacji.

W praktyce trzeba jednak pamiętać, że ten wzorzec działa poprawnie tylko wewnątrz tej samej instancji procesu. Zmiana zrobiona w innym procesie nie odświeży cache lokalnej instancji.

## Jak działa mapowanie na interfejs `IAppearanceRuleProperties`

`CustomApperance` nie tylko przechowuje dane. Ona też mapuje swoje pola na kontrakt XAF:

1. `TargetItems` zwraca `*`, gdy `SelectedProperties` jest puste, albo zwraca zawartość `SelectedProperties`.
2. `AppearanceItemType` jest na sztywno ustawione na `ViewItem`.
3. `Criteria` zwraca `"True"`, gdy `Criterion` jest puste, albo właściwy warunek.
4. `Context` zwraca `AppearanceContext.ToString()`.
5. `FontColor` mapuje się na `ForeColor`.
6. `Visibility` mapuje się na `ItemVisibility`.
7. `Enabled` zawsze zwraca `null`.
8. `DeclaringType` zwraca `DataType`.

Z tego wynikają istotne konsekwencje:

1. mechanizm nie obsługuje wyłączania przez `Enabled = false`, mimo że standardowy `Appearance` to potrafi,
2. mechanizm jest skupiony głównie na kolorach, widoczności i stylu czcionki,
3. `AppearanceItemType` nie jest konfigurowalne z poziomu danych i zawsze celuje w `ViewItem`.

To jest jedno z najważniejszych ograniczeń, które trzeba opisać w dokumentacji wdrożeniowej.

## Gdzie użytkownik konfiguruje te reguły

Reguły są wystawione do UI jako normalny obiekt biznesowy XAF.

Konfiguracja interfejsu znajduje się głównie w:

1. `HIS.Module\Model.DesignedDiffs.xafml`
2. `HIS.Blazor.Server\Model_pl.xafml`

Z modelu wynika, że:

1. istnieje `CustomApperance_DetailView`,
2. istnieje `CustomApperance_ListView`,
3. widok jest dodany do nawigacji,
4. w `DetailView` są osobne grupy układu dla kolorów i parametrów reguły,
5. `ListView` pokazuje między innymi `Name`, `BackColor`, `ForeColor`, `BorderColor`, `DataType`, `Priority`, `Criterion`, `ViewId`.

Układ nie jest przypadkowy. Jest jawnie modelowany w XAFML, więc przy przenoszeniu tego mechanizmu do innego systemu nie wystarczy skopiować samej klasy i kontrolera. Trzeba też odtworzyć model widoków albo zaakceptować inny układ.

## Dodatkowa akcja klonowania reguł

`HIS.Module\Controllers\CustomApperanceListViewController.cs` dodaje akcję `Klonuj`.

To nie jest element krytyczny dla samego działania reguł, ale jest częścią ergonomii rozwiązania. Użytkownik może zaznaczyć jedną regułę i utworzyć jej kopię przez `CloneFrom`.

Metoda `CloneFrom` kopiuje wszystkie istotne pola reguły, w tym:

1. kolory,
2. typ obiektu,
3. kryterium,
4. wybrane pola,
5. widoczność,
6. priorytet,
7. kontekst,
8. styl czcionki,
9. `ViewId`.

Jeżeli ten wzorzec ma być przenoszony jeden do jednego, warto zachować również tę akcję.

## Inicjalizacja modułów i zależności XAF

Sam kod domenowy nie wystarcza. Mechanizm zależy od kilku miejsc konfiguracji modułów.

### Rejestracja modułu `ConditionalAppearance`

W `HIS.Module\Module.cs` jest:

`RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.ConditionalAppearance.ConditionalAppearanceModule));`

To jest warunek konieczny. Bez tego `AppearanceController` i cała infrastruktura XAF dla appearance rules nie będą dostępne.

### Rejestracja po stronie Blazor

W `HIS.Blazor.Server\Startup.cs` jest:

`builder.Modules.AddConditionalAppearance()`

To jest drugi warunek konieczny dla aplikacji Blazor.

### Rejestracja po stronie WinForms

W `HIS.Platform.Win\Startup.cs` również jest:

`builder.Modules.AddConditionalAppearance()`

To pokazuje, że mechanizm ma być wspólny dla obu platform, chociaż dalej widać, że część zachowań została dopisana specjalnie dla Blazor.

## Rejestracja encji i załadowanie danych przy starcie

Po stronie Blazor w `HIS.Blazor.Server\Startup.cs` występują dwa kluczowe kroki:

1. `XafTypesInfo.Instance.RegisterEntity(typeof(CustomApperance));`
2. `CustomApperanceStorage.InitCustomApperances([.. objectSpace.GetObjectsQuery<CustomApperance>()]);`

To są miejsca absolutnie krytyczne dla działania rozwiązania.

Pierwszy krok zapewnia rejestrację typu w metadanych XAF. Drugi krok zasila cache w pamięci.

Obok tego ładowane są też inne podobne cache, na przykład `FilteringCriteriaStorage`, `DayOffStorage` i `AppConfigurationStorage`. To sugeruje szerszy lokalny wzorzec architektoniczny: część konfiguracji pomocniczej jest ładowana do singletonowych statycznych storage.

## Ręczne odświeżanie cache z poziomu UI

W `HIS.Module\Controllers\AppConfigurationDetailViewController.cs` istnieje akcja:

`Odśwież cache aplikacji`

Ta akcja wykonuje między innymi:

1. `CustomApperanceStorage.InitCustomApperances(...)`
2. `FilteringCriteriaStorage.InitFilteringCriterias(...)`

To jest bardzo ważny sygnał architektoniczny. Autor systemu świadomie wiedział, że cache może się rozjechać albo wymagać ręcznego przeładowania. Jeżeli w innym projekcie chcesz przenieść ten mechanizm, to dokumentacja operacyjna powinna wprost mówić, kiedy trzeba użyć takiej akcji albo czym zastąpić to rozwiązanie.

## Uprawnienia i nawigacja

Mechanizm nie został potraktowany jako wewnętrzny detal techniczny. Jest normalnie udostępniony użytkownikom.

W `HIS.Module\DatabaseUpdate\Updater.cs` są ważne elementy:

1. `defaultRole.AddTypePermission<CustomApperance>(CreateReadWriteAccess, SecurityPermissionState.Allow);`
2. `help.AddNavigationPermission(@"Application/NavigationItems/Items/Help/Items/CustomApperance_ListView", SecurityPermissionState.Allow);`

Z tego wynika, że:

1. obiekt ma przypisane uprawnienia typu,
2. jest dostępny w nawigacji,
3. w praktyce został przewidziany dla grupy pomocniczej lub administracyjnej.

Jeżeli to rozwiązanie ma być skopiowane do innego systemu, brak konfiguracji security i navigation będzie skutkował tym, że kod będzie istniał, ale nikt nie będzie mógł z niego skorzystać.

## Seed danych startowych

W `HIS.Module\DatabaseUpdate\Updater.cs` znajduje się metoda `AddCustomApperance()`.

Metoda ta tworzy przykładową regułę:

1. nazwa: `Brak - Opis mikroskopowy`,
2. kolor czcionki: czerwony,
3. typ obiektu: `ExaminationObject`,
4. kryterium: `IsNullOrEmpty([MicroscopicDescription])`,
5. `ViewId`: `PathomorphologicalExamination_ExaminationObjects_ListView_Diagnosis`.

To pokazuje bardzo ważny wzorzec praktyczny:

1. reguły są projektowane jako dane konfiguracyjne,
2. mogą być preseedowane na starcie,
3. `ViewId` jest używany do precyzyjnego zawężenia zachowania do jednego widoku.

To jest dobry materiał do dokumentacji wdrożeniowej, bo pokazuje, jak autorzy HIS realnie z tego korzystają.

## Gdzie mechanizm jest używany poza standardowym `AppearanceController`

To jest kluczowy punkt, bo bez niego analiza byłaby niepełna.

### 1. Standardowe widoki XAF

Najważniejsze użycie to `CustomApperanceViewControler`, który działa na `ObjectView` i dokłada reguły do standardowego `AppearanceController`.

To oznacza, że reguły wpływają na zwykłe widoki `DetailView` i `ListView` tam, gdzie XAF respektuje `IAppearanceRuleProperties`.

### 2. Blazor `DxGridListEditor` i obramowanie wierszy

W `HIS.Blazor.Server\XafControllers\DataGridListViewController.cs` istnieje osobne użycie `CustomApperanceStorage`.

Ten kontroler:

1. pobiera reguły pasujące do typu i `ViewId`,
2. filtruje je dodatkowo do takich, które mają `BorderColorString`,
3. podpina się pod `dataGridAdapter.GridModel.CustomizeElement`,
4. dla każdego wiersza pobiera obiekt danych,
5. ocenia warunek przez `ObjectSpace.CustomFit(item, customApperance.DataType, customApperance.Criterion)`,
6. jeśli warunek pasuje, ręcznie dokłada styl CSS `border: 1px solid ...`.

To jest bardzo ważne, ponieważ pokazuje, że standardowy `ConditionalAppearance` nie rozwiązywał wszystkiego. Autorzy HIS dopisali osobny kod renderujący obramowanie w siatki Blazor. Innymi słowy:

1. `CustomApperance` jest źródłem danych,
2. `AppearanceController` obsługuje część przypadków,
3. `DataGridListViewController` obsługuje specjalny przypadek obramowania wierszy w Blazor.

Jeżeli ten mechanizm chcesz przenieść do innej aplikacji Blazor, a pominiesz ten kontroler, to reguły z `BorderColor` będą zapisane i widoczne w konfiguracji, ale nie dadzą oczekiwanego efektu w gridzie.

### 3. Dynamiczne reguły dla `RegistrationTimeSlotNP`

W `HIS.Module\Controllers\RegistrationFlow\RegistrationTimeSlotNPListViewController.cs` istnieje osobny mechanizm dynamicznego budowania listy `AppearanceModel`.

Tutaj nie ma odczytu z `CustomApperanceStorage`. Zamiast tego kontroler:

1. pobiera standardowy `AppearanceController`,
2. subskrybuje `CollectAppearanceRules`,
3. buduje w pamięci listę `AppearanceModel`,
4. dla każdego koloru z `System.Drawing.Color` tworzy regułę,
5. mapuje ją na kryterium `RegistrationTimeSlotNP.ColorInt = <ARGB>`,
6. ustawia `BackColor` dla pól `From` i `To`.

To jest drugi, niezależny wzorzec rozszerzania `AppearanceController` w HIS. Pokazuje on, że autorzy systemu stosują dwa rodzaje dynamicznego appearance:

1. reguły konfigurowalne z bazy, czyli `CustomApperance`,
2. reguły budowane programowo na potrzeby konkretnego ekranu, czyli `AppearanceModel`.

To warto opisać w dokumentacji jako dwa osobne archetypy.

## Klasa `AppearanceModel` i jej znaczenie

`HIS.Module\BusinessObjects\NonPersistent\AppearanceModel.cs` to lekki model implementujący `IAppearanceRuleProperties`.

Nie jest on encją trwałą. Służy do tego, aby kontroler mógł zbudować reguły w locie bez tworzenia rekordów w bazie.

To rozwiązanie pokazuje, że w HIS autorzy nie traktują `IAppearanceRuleProperties` jako kontraktu wyłącznie dla atrybutów. Traktują go jako ogólny format danych wejściowych do `AppearanceController`.

To jest dobra wskazówka projektowa, jeżeli w przyszłości chcesz rozdzielić:

1. reguły stałe zapisane w kodzie,
2. reguły administrowalne zapisane w bazie,
3. reguły generowane runtime dla pojedynczych ekranów.

## Własna ocena kryteriów

W `HIS.Module\Extensions\IObjectSpaceExtensions.cs` istnieją metody:

1. `CustomEvaluate`
2. `CustomFit`

W kontekście wyglądu szczególnie ważne jest `CustomFit`, ponieważ właśnie ono jest użyte w `DataGridListViewController`.

Ta metoda:

1. bierze `IObjectSpace`,
2. tworzy evaluator dla wskazanego typu i wyrażenia,
3. wywołuje `Fit(entity)`.

To jest kolejny element, o którym trzeba pamiętać przy przenoszeniu mechanizmu. Sama encja `CustomApperance` nie wystarczy do pełnego działania renderingu obramowania w Blazor.

## Znaczenie `ViewId`

Pole `ViewId` jest jednym z najważniejszych pól tej architektury.

Jego rola jest następująca:

1. pozwala ograniczyć działanie reguły do jednego widoku,
2. zapobiega ubocznym efektom na innych widokach tego samego typu,
3. jest uwzględniane zarówno przez `CustomApperanceViewControler`, jak i przez `DataGridListViewController`.

W praktyce `ViewId` jest mechanizmem separacji semantycznej. W XAF bardzo często ten sam typ biznesowy występuje w wielu widokach o różnym znaczeniu. Bez `ViewId` reguły mogłyby rozlewać się zbyt szeroko.

## Znaczenie `ObjectTypeName` i `ObjectTypeFullName`

`CustomApperance` przechowuje jednocześnie:

1. `ObjectTypeFullName`
2. `ObjectTypeName`

To nie jest redundancja przypadkowa.

1. `ObjectTypeFullName` służy do odtworzenia `Type` przez `Type.GetType`.
2. `ObjectTypeName` służy do szybkiego filtrowania względem `View.ObjectTypeInfo.Name`.

W kontrolerach porównanie odbywa się po `ObjectTypeName`, a nie po pełnej nazwie. To jest prostsze, ale jednocześnie bardziej kruche. Gdyby w systemie były dwa typy o tej samej nazwie w różnych przestrzeniach nazw, filtr mógłby się pomylić.

W obecnym HIS to zapewne akceptowalne założenie, ale w analizie trzeba je nazwać wprost.

## Miejsca, które trzeba skonfigurować w innej dokumentacji

Poniższa lista powinna trafić do dokumentacji wdrożeniowej albo architektonicznej, jeżeli ten mechanizm ma zostać odtworzony w innym systemie.

### 1. Rejestracja modułu `ConditionalAppearance`

Trzeba opisać, że:

1. moduł biznesowy musi mieć `RequiredModuleTypes.Add(typeof(ConditionalAppearanceModule))`,
2. każdy host aplikacji musi mieć `AddConditionalAppearance()`.

Bez tego dynamiczne reguły nie będą konsumowane.

### 2. Rejestracja typu w XAF

Trzeba opisać, że host uruchomieniowy rejestruje:

`XafTypesInfo.Instance.RegisterEntity(typeof(CustomApperance));`

Bez tego można dostać problemy z metadanymi, widokami lub oceną typu.

### 3. Inicjalizacja cache przy starcie

Trzeba opisać, że przy starcie trzeba wykonać:

`CustomApperanceStorage.InitCustomApperances([.. objectSpace.GetObjectsQuery<CustomApperance>()]);`

Bez tego kontroler będzie działał na pustym storage.

### 4. Odświeżanie cache po zmianach

Trzeba opisać co najmniej jeden z dwóch wariantów:

1. aktualizacja przez `OnSaving()` w tej samej instancji procesu,
2. pełne przeładowanie cache przez akcję administracyjną albo inny mechanizm.

W środowisku wieloinstancyjnym trzeba dopisać osobny plan synchronizacji.

### 5. Uprawnienia bezpieczeństwa

Trzeba opisać:

1. uprawnienie typu do `CustomApperance`,
2. uprawnienie nawigacyjne do `CustomApperance_ListView`.

Bez tego administrator nie skonfiguruje reguł z UI.

### 6. Model XAF

Trzeba opisać, że sam kod nie tworzy ergonomicznego UI. Potrzebne są:

1. `DetailView`,
2. `ListView`,
3. układ pól,
4. miejsce w nawigacji,
5. ewentualne lokalizacje podpisów.

### 7. Osobna obsługa `BorderColor` w Blazor Grid

To jest punkt, który najłatwiej przeoczyć.

Jeżeli dokumentacja mówi, że `CustomApperance` obsługuje obramowanie, to trzeba od razu dopisać, że:

1. standardowy `AppearanceController` nie renderuje tego automatycznie tak, jak oczekuje autor rozwiązania,
2. dlatego potrzebny jest dodatkowy kod w stylu `DataGridListViewController`,
3. ten kod wymaga własnej oceny kryterium przez `ObjectSpace.CustomFit`.

### 8. Seed danych i przykłady użycia

Warto w dokumentacji wdrożeniowej pokazać przynajmniej jeden przykład seedowanej reguły, ponieważ w HIS taki przykład już istnieje.

### 9. Ograniczenia kontraktu

Trzeba dopisać, że obecna implementacja:

1. zawsze ustawia `AppearanceItemType` na `ViewItem`,
2. nie udostępnia sterowania `Enabled`,
3. opiera filtrowanie typu na prostej nazwie klasy,
4. używa cache statycznego w pamięci procesu.

## Co faktycznie steruje wyglądem, a co tylko dostarcza dane

To rozróżnienie jest istotne.

### Elementy sterujące bezpośrednio

1. `AppearanceController` z XAF, bo to on finalnie konsumuje reguły.
2. `CustomApperanceViewControler`, bo to on dokłada reguły z bazy do `AppearanceController`.
3. `DataGridListViewController`, bo to on ręcznie nakłada CSS obramowania w Blazor Grid.
4. `RegistrationTimeSlotNPListViewController`, bo to on dynamicznie buduje runtime appearance dla slotów.

### Elementy dostarczające dane lub wspierające

1. `CustomApperance`, bo opisuje regułę.
2. `CustomApperanceStorage`, bo przechowuje cache.
3. `AppearanceModel`, bo jest lekkim nośnikiem reguł runtime.
4. `IObjectSpaceExtensions.CustomFit`, bo pozwala ocenić warunek poza standardowym pipeline XAF.

## Relacja do standardowych atrybutów `[Appearance]`

Mechanizm `CustomApperance` nie zastępuje zwykłych atrybutów `[Appearance]`. Oba rozwiązania współistnieją.

W HIS można więc wyróżnić trzy poziomy definicji wyglądu:

1. reguły statyczne w kodzie przez `[Appearance]`,
2. reguły dynamiczne z bazy przez `CustomApperance`,
3. reguły dynamiczne budowane programowo przez `AppearanceModel`.

To jest dojrzały, ale też bardziej złożony układ. Dokumentacja architektoniczna powinna to rozdzielić, bo inaczej odbiorca uzna błędnie, że całe sterowanie wyglądem jest w jednej klasie.

## Ograniczenia i ryzyka obecnej implementacji

Poniższe ryzyka nie oznaczają błędu, ale powinny być jawnie opisane.

1. Nazwa `CustomApperance` zawiera literówkę i jest już częścią kontraktu systemu.
2. Cache jest statyczny i lokalny dla procesu.
3. W środowisku wieloserwerowym brak jest mechanizmu synchronizacji cache między instancjami.
4. Filtrowanie typu po `ObjectTypeName` może być kruche przy kolizjach nazw klas.
5. `BorderColor` wymaga osobnej obsługi w Blazor Grid.
6. `Enabled` nie jest realnie wspierane przez klasę `CustomApperance`.
7. `AppearanceItemType` jest na sztywno ustawione i nie daje pełnej elastyczności standardowego XAF.

## Co należałoby przenieść jeden do jednego, jeżeli chcesz odtworzyć ten wzorzec

Jeżeli celem jest wierne skopiowanie mechanizmu do innego repozytorium, minimalny zestaw obejmuje:

1. encję `CustomApperance`,
2. storage `CustomApperanceStorage`,
3. kontroler `CustomApperanceViewControler`,
4. `DbSet<CustomApperance>` w `DbContext`,
5. migrację tworzącą tabelę `CustomApperances`,
6. rejestrację `ConditionalAppearanceModule`,
7. rejestrację `AddConditionalAppearance()` w hostach,
8. `RegisterEntity(typeof(CustomApperance))`,
9. inicjalizację cache przy starcie,
10. mechanizm ręcznego odświeżania cache,
11. model XAF dla `DetailView`, `ListView` i nawigacji,
12. security i navigation permissions,
13. opcjonalnie akcję klonowania,
14. w Blazor dodatkowo `DataGridListViewController`, jeśli chcesz zachować działanie `BorderColor`.

Jeżeli chcesz przenieść tylko ideę, a nie identyczne zachowanie, to można uprościć ten zestaw, ale wtedy trzeba świadomie zrezygnować z części funkcji.

## Wniosek końcowy

Mechanizm dodatkowych reguł wyglądu w HIS nie jest pojedynczą klasą, tylko małą architekturą opartą o XAF `ConditionalAppearance`.

Najważniejsza odpowiedź jest taka:

1. Za definicję reguły odpowiada `CustomApperance`.
2. Za wstrzyknięcie reguły do standardowego mechanizmu XAF odpowiada `CustomApperanceViewControler`.
3. Za stan reguł w pamięci odpowiada `CustomApperanceStorage`.
4. Za dodatkowe obramowanie w siatce Blazor odpowiada `DataGridListViewController`.
5. Za drugi archetyp dynamicznego appearance, generowany programowo, odpowiada `RegistrationTimeSlotNPListViewController` wraz z `AppearanceModel`.

Jeżeli ta analiza ma służyć jako podstawa implementacyjna w innym projekcie, to dokumentacja pomocnicza musi opisywać nie tylko klasę `CustomApperance`, ale cały przepływ: model danych, cache, integrację z `AppearanceController`, konfigurację hosta, bezpieczeństwo, model XAF i osobne wyjątki platformowe dla Blazor.

# Dynamiczne reguły wyglądu z bazy w XAF Blazor i WinForms

## Po co to w ogóle robić

Standardowy `ConditionalAppearance` w XAF świetnie działa dla reguł zapisanych w kodzie przez `[Appearance]`. Problem zaczyna się wtedy, gdy chcesz:

1. zmieniać reguły bez rekompilacji,
2. oddać konfigurację administratorowi,
3. trzymać wygląd jako dane, a nie jako atrybuty w klasach,
4. używać tego samego wzorca w wielu projektach.

W tym repo dodałem warstwę nad standardowym `AppearanceController`. Reguły są zapisywane w bazie, ładowane do pamięci i dokładane do pipeline XAF w czasie działania.

## Co dokładnie zostało dodane w MainDemo

Implementacja siedzi w tych plikach:

1. [DynamicAppearanceRule.cs](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Module/BusinessObjects/DynamicAppearanceRule.cs)
2. [DynamicAppearanceRuleStorage.cs](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Module/Storages/DynamicAppearanceRuleStorage.cs)
3. [DynamicAppearanceRuleViewController.cs](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Module/Controllers/DynamicAppearanceRuleViewController.cs)
4. [MainDemoDbContext.cs](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Module/BusinessObjects/MainDemoDbContext.cs)
5. [MainDemoModule.cs](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Module/MainDemoModule.cs)
6. [Updater.cs](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Module/DatabaseUpdate/Updater.cs)
7. [DynamicAppearanceRuleTests.cs](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.WebAPI.Tests/DynamicAppearanceRuleTests.cs)

To jest wariant pragmatyczny. Nie próbuje odwzorować wszystkiego z HIS jeden do jednego. Zachowuje najważniejszy wzorzec:

1. encja z danymi reguły,
2. cache procesowy,
3. kontroler dokładający reguły do `AppearanceController`,
4. seed przykładowej reguły,
5. testy sprawdzające dane i filtrowanie.

## Klasy, które tworzą to rozwiązanie

Tutaj nie chodzi o ogólny pomysł, tylko o trzy konkretne klasy:

1. `DynamicAppearanceRule` jako encja reguły i implementacja `IAppearanceRuleProperties`,
2. `DynamicAppearanceRuleStorage` jako procesowy cache reguł,
3. `DynamicAppearanceRuleViewController` jako punkt podpięcia do `AppearanceController`.

Bez tych trzech elementów rozwiązanie nie działa w pełni.

## Jak działa ten wariant

Przepływ jest prosty:

1. `DynamicAppearanceRule` przechowuje typ obiektu, kryterium, kontekst, priorytet, kolory i docelowe pola.
2. `DynamicAppearanceRuleStorage` trzyma listę reguł w pamięci procesu.
3. `MainDemoModule` podczas `SetupComplete` ładuje wszystkie reguły z bazy do cache.
4. `DynamicAppearanceRuleViewController` podpina się do `AppearanceController.CollectAppearanceRules`.
5. Dla bieżącego `View.ObjectTypeInfo.Type` i `View.Id` wybiera pasujące reguły ze storage.
6. XAF traktuje je jak zwykłe reguły appearance i stosuje do widoku.

W tym repo seedowana jest przykładowa reguła:

1. nazwa: `Highlight overdue tasks`,
2. typ: `DemoTask`,
3. kryterium: zadanie po terminie i nieukończone,
4. target: `Subject;DueDate;AssignedTo`,
5. efekt: ciemnoczerwona czcionka + jasne tło.

## Przykład encji reguły

Najważniejsza klasa to:

- [DynamicAppearanceRule.cs](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Module/BusinessObjects/DynamicAppearanceRule.cs)

To ona przechowuje dane reguły i wystawia je w formacie oczekiwanym przez XAF:

```csharp
[DefaultClassOptions]
[DefaultProperty(nameof(Name))]
[ImageName("BO_Condition")]
public class DynamicAppearanceRule : BaseObject, IAppearanceRuleProperties {
    [StringLength(256)]
    public virtual string Name { get; set; }

    [StringLength(512)]
    public virtual string ObjectTypeFullName { get; set; }

    [StringLength(256)]
    public virtual string ObjectTypeName { get; set; }

    [NotMapped]
    [ImmediatePostData]
    public virtual Type DataType {
        get => string.IsNullOrWhiteSpace(ObjectTypeFullName) ? null : Type.GetType(ObjectTypeFullName);
        set {
            ObjectTypeFullName = value?.AssemblyQualifiedName;
            ObjectTypeName = value?.Name;
        }
    }

    public virtual string Criteria { get; set; } = "True";
    public virtual string TargetItems { get; set; } = "*";
    public virtual string Context { get; set; } = "Any";
    public virtual string AppearanceItemType { get; set; } = "ViewItem";
    public virtual string ViewId { get; set; }
    public virtual int Priority { get; set; }
}
```

W tej klasie są też pola dla kolorów, `CssClass`, `Method`, `FontStyle`, `Visibility` i `Enabled`, ale powyższy fragment pokazuje najważniejszy rdzeń.

Istotny jest też zapis do cache przy `OnSaving()`:

```csharp
public override void OnSaving() {
    base.OnSaving();
    var objectSpace = ((IObjectSpaceLink)this).ObjectSpace;
    if(objectSpace != null && objectSpace.IsDeletedObject(this)) {
        DynamicAppearanceRuleStorage.Remove(this);
    }
    else {
        DynamicAppearanceRuleStorage.Put(this);
    }
}
```

To oznacza, że reguła po zapisie aktualizuje cache bez czekania na restart aplikacji.

## Przykład storage

Drugą klasą jest:

- [DynamicAppearanceRuleStorage.cs](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Module/Storages/DynamicAppearanceRuleStorage.cs)

To prosty cache procesowy:

```csharp
public static class DynamicAppearanceRuleStorage {
    private static readonly Lock SyncRoot = new();
    private static List<DynamicAppearanceRule> rules = new();

    public static void Initialize(IEnumerable<DynamicAppearanceRule> sourceRules) {
        lock(SyncRoot) {
            rules = sourceRules.Where(rule => rule != null).ToList();
        }
    }

    public static IReadOnlyList<IAppearanceRuleProperties> GetRules(Type objectType, string viewId) {
        lock(SyncRoot) {
            return rules
                .Where(rule => rule.Matches(objectType, viewId))
                .Cast<IAppearanceRuleProperties>()
                .ToList();
        }
    }
}
```

To jest celowo proste rozwiązanie:

1. szybkie,
2. łatwe do debugowania,
3. wystarczające dla pojedynczej instancji aplikacji.

Jeżeli aplikacja działa w wielu instancjach, taki cache nie synchronizuje się sam.

## Przykład kontrolera integrującego z XAF

Trzecia klasa to:

- [DynamicAppearanceRuleViewController.cs](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Module/Controllers/DynamicAppearanceRuleViewController.cs)

To ona dokłada reguły z cache do standardowego `AppearanceController`:

```csharp
public class DynamicAppearanceRuleViewController : ObjectViewController<ObjectView, object> {
    private AppearanceController appearanceController;

    protected override void OnActivated() {
        base.OnActivated();
        appearanceController = Frame.GetController<AppearanceController>();
        if(appearanceController == null) {
            return;
        }
        appearanceController.ResetRulesCache();
        appearanceController.CollectAppearanceRules += AppearanceController_CollectAppearanceRules;
        appearanceController.Refresh();
    }

    private void AppearanceController_CollectAppearanceRules(object sender, CollectAppearanceRulesEventArgs e) {
        foreach(var rule in DynamicAppearanceRuleStorage.GetRules(View.ObjectTypeInfo.Type, View.Id)) {
            e.AppearanceRules.Add(rule);
        }
    }
}
```

To jest właściwe miejsce, w którym mechanizm zaczyna działać. Sama encja w bazie niczego jeszcze nie zmienia w UI.

## Minimalny zestaw do przeniesienia do innego projektu XAF

Jeżeli chcesz to uruchomić w osobnym projekcie XAF, potrzebujesz:

1. encji reguły implementującej `IAppearanceRuleProperties`,
2. `DbSet` w `DbContext`,
3. modułu `ConditionalAppearance`,
4. storage z cache,
5. kontrolera `ObjectViewController<ObjectView, object>`,
6. inicjalizacji cache przy starcie aplikacji,
7. seedu albo UI administracyjnego do tworzenia reguł.

Bez tych siedmiu elementów będziesz mieć tylko fragment rozwiązania.

## Krok po kroku w nowym projekcie

### 1. Włącz `ConditionalAppearance`

W module:

```csharp
RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.ConditionalAppearance.ConditionalAppearanceModule));
```

W hostach:

```csharp
builder.Modules.AddConditionalAppearance();
```

To jest warunek konieczny.

### 2. Dodaj encję reguły

Encja musi implementować `IAppearanceRuleProperties`. W praktyce najwygodniej trzymać:

1. nazwę,
2. pełną nazwę typu i prostą nazwę typu,
3. `Criteria`,
4. `TargetItems`,
5. `Context`,
6. `AppearanceItemType`,
7. `ViewId`,
8. `Priority`,
9. `Enabled`,
10. `Visibility`,
11. kolory.

W MainDemo typ jest ustawiany przez `DataType`, ale do bazy trafiają:

1. `ObjectTypeFullName`,
2. `ObjectTypeName`.

To upraszcza filtrowanie po typie i odtwarzanie `Type`.

### 3. Dodaj `DbSet`

W `DbContext`:

```csharp
public DbSet<DynamicAppearanceRule> DynamicAppearanceRules { get; set; }
```

Jeżeli używasz migracji EF Core, po tej zmianie wygeneruj nową migrację.

### 4. Dodaj storage

Storage powinien mieć minimum:

1. `Initialize(...)`,
2. `Put(...)`,
3. `Remove(...)`,
4. `GetRules(Type objectType, string viewId)`.

W MainDemo storage jest statyczny i procesowy. To jest świadomy kompromis:

1. działa szybko,
2. jest prosty,
3. nie rozwiązuje synchronizacji między wieloma instancjami aplikacji.

Jeżeli uruchamiasz aplikację w wielu instancjach, trzeba dodać osobny mechanizm odświeżania.

### 5. Dodaj kontroler integrujący z XAF

Kontroler musi:

1. pobrać `AppearanceController` z `Frame`,
2. wywołać `ResetRulesCache()`,
3. podpiąć `CollectAppearanceRules`,
4. dodać pasujące reguły do `e.AppearanceRules`.

To jest właściwy punkt integracji z XAF. Sama encja i storage niczego jeszcze nie wyświetlą.

### 6. Zainicjalizuj cache przy starcie

W MainDemo zostało to zrobione w module:

```csharp
application.SetupComplete += Application_SetupComplete;
```

W handlerze:

```csharp
using var objectSpace = application.CreateObjectSpace(typeof(DynamicAppearanceRule));
DynamicAppearanceRuleStorage.Initialize(objectSpace.GetObjects<DynamicAppearanceRule>());
```

To wystarcza, żeby reguły były dostępne od początku działania aplikacji.

### 7. Dodaj seed lub ekran administracyjny

Na start najwygodniej dodać seed. W MainDemo robi to:

[Updater.cs](C:/Users/Programista/source/repos/MainDemo.NET.EFCore/CS/MainDemo.Module/DatabaseUpdate/Updater.cs)

Jeżeli seed działa, możesz od razu sprawdzić:

1. czy reguła zapisuje się do bazy,
2. czy trafia do cache,
3. czy jest stosowana na widoku.

## Jak to uruchomić i sprawdzić

W tym repo:

```powershell
dotnet build CS\MainDemo.NET.EFCore.sln -c Debug
dotnet test CS\MainDemo.WebAPI.Tests\MainDemo.WebAPI.Tests.csproj -c Debug --filter "DynamicAppearanceRuleTests|LocalizationTests"
dotnet run --project CS\MainDemo.Blazor.Server\MainDemo.Blazor.Server.csproj -c Debug --urls http://localhost:5115
```

Po uruchomieniu:

1. zaloguj się jako `Sam`,
2. przejdź do listy zadań,
3. znajdź zadanie po terminie,
4. sprawdź, czy pola `Subject`, `DueDate` i `AssignedTo` dostały styl z reguły.

Jeżeli w osobnym projekcie nie widzisz efektu, sprawdź w tej kolejności:

1. czy `ConditionalAppearance` jest dodany w module i hostach,
2. czy encja jest w `DbContext`,
3. czy cache jest inicjalizowany,
4. czy kontroler rzeczywiście działa dla `ObjectView`,
5. czy kryterium pasuje do danych,
6. czy `DataType` i `ObjectTypeName` odpowiadają realnemu typowi widoku.

## Co można rozbudować dalej

Ten wariant jest świadomie mały. W osobnym projekcie możesz dołożyć:

1. ekran administracyjny z lepszym układem pól,
2. walidację składni kryterium,
3. wybór `TargetItems` z listy pól zamiast ręcznego wpisywania,
4. ręczne odświeżanie cache z UI,
5. cache rozproszony lub event do synchronizacji między instancjami,
6. osobną obsługę przypadków, których standardowy `AppearanceController` nie renderuje tak, jak chcesz.

To ostatnie dotyczy szczególnie niestandardowego stylowania w Blazor Grid. W HIS osobny kontroler robił ręczne obramowanie wierszy. W MainDemo tego nie dokładałem, bo celem było wdrożenie rdzenia wzorca, a nie pełnego systemu renderowania.

## Najważniejsze ograniczenia tego wzorca

1. Cache jest lokalny dla procesu.
2. Reguły działają dobrze dla standardowego pipeline `AppearanceController`, ale nie załatwiają każdego niestandardowego przypadku renderingu.
3. Filtrowanie po `ObjectTypeName` jest proste i szybkie, ale mniej odporne niż filtrowanie po pełnej nazwie typu.
4. Sama encja nie wystarczy. Bez kontrolera i inicjalizacji nic się nie wydarzy.

## Wniosek

Jeżeli potrzebujesz appearance sterowanego danymi, nie trzeba pisać własnego systemu od zera. Wystarczy potraktować `IAppearanceRuleProperties` jako kontrakt wejściowy do `AppearanceController`, dołożyć encję, cache i kontroler.

To właśnie zostało zrobione w MainDemo. Jest małe, testowalne i nadaje się do skopiowania do osobnego projektu XAF bez ciągnięcia całej reszty repozytorium.

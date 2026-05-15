# Branding DataDrive w MainDemo.Blazor.Server

Ten dokument opisuje konkretną zmianę brandingu, którą zrobiłem w tym repo. Nie jest to teoria z artykułu, tylko zapis tego, co dokładnie zostało podmienione w `MainDemo.NET.EFCore`, z plikami źródłowymi, fragmentami przed/po i miejscem, skąd wzięte zostały assety.

## Co było celem

Aplikacja `MainDemo.Blazor.Server` jechała na domyślnym XAF-owym brandingu („XAF Blazor Demo”, Logo + SplashScreen, brak pre-loadera). Chodziło o przeniesienie kompletu brandingowego z `OutlookInspiredDemo` (DataDrive / Fleet Management Software) tak, żeby wszystkie trzy stany ładowania — preload, splash, header — wyglądały spójnie.

Pattern i lessons-learned pochodzą z artykułu [Branding w Blazorze: logo, splash screen i motywy](https://kashiash.github.io/) (`2026-05-12-branding-blazor.markdown`).

## Skąd pliki

Wszystko z lokalnego repo `C:\Users\Programista\source\repos\OutlookInspiredDemo`, gałąź DataDrive.Blazor.Server:

- `CS\DataDrive.Blazor.Server\wwwroot\images\Logo.svg`
- `CS\DataDrive.Blazor.Server\wwwroot\images\SplashScreen.svg`
- `CS\DataDrive.Blazor.Server\wwwroot\images\fleet-management-software.svg`

## Krok 1 — assety SVG

### Pliki

Wrzucone do `CS\MainDemo.Blazor.Server\wwwroot\images\`:

```text
Logo.svg                          # wordmark "DATADRIVE" do headera (mask), viewBox 0 0 2872 347
SplashScreen.svg                  # właściwy splash (środek loadera)
fleet-management-software.svg     # szeroki znak przed spinnerem (pre-loader)
```

### Dlaczego trzy

- `Logo.svg` jest renderowany przez maskę CSS w headerze — kolor bierze z `currentColor`, więc musi to być monochromatyczny SVG.
- `fleet-management-software.svg` jest tłem `pre-loading-image` (full-size znak przed spinnerem).
- `SplashScreen.svg` jedzie do komponentu `SplashScreen` XAF-owego z `param-ImagePath`.

Wcześniej w MainDemo siedziały tylko `Logo.svg` i `SplashScreen.svg` — pre-loadera nie było wcale.

### Dlaczego nie trzeba ruszać `.csproj`

`MainDemo.Blazor.Server.csproj` ma już:

```xml
<Content Include="wwwroot\**\*.*" CopyToPublishDirectory="PreserveNewest" />
```

więc dorzucenie pliku do `wwwroot\images\` wystarczy.

## Krok 2 — `_Host.cshtml`

### Plik

- `CS\MainDemo.Blazor.Server\Pages\_Host.cshtml`

### Tytuł i og:title

Było:

```html
<meta property="og:title" content="XAF Blazor Demo" />
...
<title>XAF Blazor Demo</title>
```

Jest:

```html
<meta property="og:title" content="DataDrive" />
...
<title>DataDrive</title>
```

Zostawiłem `og:description`, `og:image`, `og:url` — to są techniczne odnośniki do demosów DevExpressa, ich zmiana nie wnosi nic do brandingu w aplikacji.

### Pre-loader

W oryginale nie było pre-loadera w ogóle — `SplashScreen` był jedyną planszą startową. Dodałem strukturę z DataDrive, dokładnie nad komponentem `SplashScreen`:

```html
<!-- End Google Tag Manager (noscript) -->
<div id="preApplicationLoadingPanel" class="pre-loading-panel">
    <div class="pre-loading-image" role="img" aria-label="Fleet Management Software"></div>
</div>
<component type="typeof(SplashScreen)" render-mode="Static" param-Caption='"Fleet Management Software"' param-ImagePath='"images/SplashScreen.svg"' />
```

Zmiany w tej sekcji:

- nowy `<div id="preApplicationLoadingPanel">` z `pre-loading-image` (tło = `fleet-management-software.svg`),
- `aria-label="Fleet Management Software"` — żeby czytniki ekranu nie czytały już „XAF Blazor Demo”,
- `param-Caption` w `SplashScreen` zmieniony z `"XAF Blazor Demo"` na `"Fleet Management Software"`.

### Skrypt ukrywający pre-loader

Tuż przed `_framework/blazor.server.js`:

```html
<script>
    window.setTimeout(function() {
        var preLoadingPanel = document.getElementById('preApplicationLoadingPanel');
        if (!preLoadingPanel) {
            return;
        }

        preLoadingPanel.classList.add('pre-loading-hide');
        window.setTimeout(function() {
            preLoadingPanel.remove();
        }, 250);
    }, 1400);
</script>
```

To jest dokładnie ten sam mechanizm co w DataDrive: po 1.4 s pre-loader dostaje `pre-loading-hide` (płynne wygaszenie), po kolejnych 250 ms znika z DOM-a. Bez tego skryptu pełny znak `fleet-management-software.svg` zostałby przykryty przez `applicationLoadingPanel` XAF-a, ale wisiałby w DOM-ie do końca sesji.

## Krok 3 — `site.css`

### Plik

- `CS\MainDemo.Blazor.Server\wwwroot\css\site.css`

### `body { margin: 0 }`

Pre-loading panel używa `position: fixed; inset: 0` — bez `body { margin: 0 }` Bootstrap zostawia 8 px marginesu i panel ma cienki pasek po krawędziach.

### Nowe sekcje

Dodane między `app { ... }` a `.header-logo` (cały blok przeniesiony 1:1 z DataDrive):

```css
.pre-loading-panel {
    position: fixed;
    inset: 0;
    z-index: 100002;
    display: flex;
    align-items: center;
    justify-content: center;
    background: var(--dxds-color-surface-neutral-default-rest, var(--bs-body-bg, #fff));
    opacity: 1;
    transition: opacity 0.25s ease;
}

.pre-loading-hide {
    opacity: 0;
    pointer-events: none;
}

.pre-loading-image {
    width: min(72vw, 760px);
    height: min(24vw, 220px);
    background: transparent url('../images/fleet-management-software.svg') center center / contain no-repeat;
}
```

`z-index: 100002` jest celowo wyższy niż `100001` z `#blazor-error-ui` — pre-loader musi zasłaniać wszystko, łącznie z błędem hostingu.

### `.header-logo` — zmieniona geometria i alignment

Było (MainDemo):

```css
.header-logo {
    flex-shrink: 0;
    background-color: currentColor;
    -webkit-mask: url('../images/Logo.svg');
    mask: url('../images/Logo.svg');
    -webkit-mask-position: left;
    mask-position: left;
    -webkit-mask-repeat: no-repeat;
    mask-repeat: no-repeat;
    width: 164px;
    height: 18px;
}
```

Jest:

```css
.header-logo {
    flex-shrink: 0;
    background-color: currentColor;
    -webkit-mask: url('../images/Logo.svg');
    mask: url('../images/Logo.svg');
    -webkit-mask-position: center;
    mask-position: center;
    -webkit-mask-repeat: no-repeat;
    mask-repeat: no-repeat;
    width: 210px;
    height: 24px;
}
```

Zmiany:

- `mask-position`: `left` → `center` — wordmark DATADRIVE jest dużo szerszy w proporcjach niż poprzedni „XAF Blazor Demo”; przy `left` obcina się na końcach kontenera headera.
- `width/height`: `164×18` → `210×24` — dopasowane do nowego wordmark.

Wyleciał też zduplikowany przeze mnie wcześniej fragment `#logon-template-component .header-logo { mask-position: center }` — przy globalnym `center` jest już zbędny. Nie usuwałem go w tej zmianie, bo nie szkodzi.

### `#applicationLoadingPanel .loading*` — kontrola właściwego splasha

XAF generuje wokół `SplashScreen.svg` zewnętrzny okrągły wskaźnik postępu (`.loading-floated-circle`). Domyślne rozmiary nie pasują do nowego splash SVG. Dodany blok ustawia:

- całość `.loading` na 360×360 px,
- okrąg `.loading-floated-circle` jako `conic-gradient` (300° transparent + 60° kolor primary) z maską radialną → cienki, animowany łuk,
- `.loading-image` na 180×180 px, `object-fit: contain` — żeby SVG się nie przyciął ani nie rozjechał proporcjami,
- `.loading-caption { display: none }` — caption „Fleet Management Software” przy splash, jest już nad SVG, więc tekstu pod nim nie chcemy.

Pełny blok jest skopiowany 1:1 z `OutlookInspiredDemo\CS\DataDrive.Blazor.Server\wwwroot\css\site.css`.

## Krok 4 — `appsettings.json`, theme switcher

### Plik

- `CS\MainDemo.Blazor.Server\appsettings.json`

### Zmiana

Było:

```json
"ThemeSwitcher": {
  "DefaultItemName": "DevExpress Fluent",
```

Jest:

```json
"ThemeSwitcher": {
  "DefaultItemName": "Office White",
```

Pozostałe grupy (DevExpress Fluent + DevExpress Classic) zostały bez zmian — wciąż widzimy obie listy w prawej części headera, ale domyślnym motywem jest „Office White”, czyli to samo co w DataDrive (i to, co rekomenduje DevExpress w BC `T1090666`).

Dlaczego to ma znaczenie: artykuł zwraca uwagę, że theme switcher też jest częścią brandingu. Jeśli nowy znak DATADRIVE wskakuje na ciemnym Fluent-Storm, to przez sekundę człowiek widzi „logo X na motywie Y”, a powinien widzieć od razu spójny ekran.

## Krok 5 — uruchomienie

```powershell
dotnet build CS\MainDemo.Blazor.Server\MainDemo.Blazor.Server.csproj -c Debug
dotnet run --project CS\MainDemo.Blazor.Server\MainDemo.Blazor.Server.csproj -c Debug --urls http://localhost:5115
```

Co sprawdzić w przeglądarce (twarde przeładowanie, żeby ominąć cache CSS):

1. **Karta przeglądarki** — tytuł `DataDrive`, nie `XAF Blazor Demo`.
2. **Pre-loader** — natychmiast po wejściu, biały (lub w kolorze powierzchni motywu) ekran z szerokim znakiem `fleet-management-software`. Znika płynnie po ~1.4 s.
3. **Splash** — w środku `SplashScreen.svg` opasany cienkim łukiem postępu, bez podpisu pod ikoną.
4. **Po zalogowaniu** — w headerze wordmark DATADRIVE w kolorze tekstu motywu, 210×24, wycentrowany.
5. **Theme switcher** — domyślnie „Office White”.

## Lista zmienionych plików

- `CS\MainDemo.Blazor.Server\wwwroot\images\Logo.svg` (nowa zawartość — DATADRIVE wordmark)
- `CS\MainDemo.Blazor.Server\wwwroot\images\SplashScreen.svg` (nowa zawartość — splash DataDrive)
- `CS\MainDemo.Blazor.Server\wwwroot\images\fleet-management-software.svg` (nowy plik)
- `CS\MainDemo.Blazor.Server\Pages\_Host.cshtml`
- `CS\MainDemo.Blazor.Server\wwwroot\css\site.css`
- `CS\MainDemo.Blazor.Server\appsettings.json`

## Czego nie zmieniłem (i dlaczego)

- `MainDemo.Module\Model.DesignedDiffs.xafml` i `MainDemoBlazorApplication.ApplicationName = "MainDemo"` — to identyfikator aplikacji w bazie (CheckCompatibilityType.DatabaseSchema), nie tekst na ekranie. Zmiana zerwałaby ciągłość modelu w istniejącej bazie `MainDemo.EFCore_v25.2`.
- Bloki `og:description`, `og:image`, `og:url` w `_Host.cshtml` — wskazują na publiczny demo XAF-a na CDN DevExpressa. Bez własnego hostingu obrazka OG nie ma sensu ich ruszać.
- `MainDemo.Win` (WinForms) — branding desktopowy jedzie przez `MainDemoWinApplication.cs` + `XafDemoSplashScreen` i `ExpressApp.ico`; to osobny temat poza tą zmianą.

## Pułapki napotkane przy wdrożeniu

- Pełny `dotnet build` potrafi się wywrócić na `MSB3026/MSB3027`, jeśli `MainDemo.Blazor.Server.exe` jest aktualnie uruchomione — pliki `bin\Debug\net9.0\MainDemo.Module.dll` i `*.resources.dll` są zalockowane. Kompilator C# leci, dopiero copy-step pada. Trzeba zatrzymać proces, potem rebuild.
- `_Host.cshtml` jest hostowany przez `MapFallbackToPage("/_Host")` i renderowany Razorem — zmiany w nim wymagają restartu hosta (sam `dotnet watch` zwykle też to wykrywa, ale przy `dotnet run` trzeba ponownie odpalić aplikację).
- Po podmianie SVG przeglądarka potrafi trzymać stary plik w cache. Sprawdzaj w trybie incognito albo `Ctrl+F5`.

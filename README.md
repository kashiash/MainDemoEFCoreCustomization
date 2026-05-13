# MainDemo.NET.EFCore — wzorce wdrożeniowe

Fork publicznego projektu referencyjnego DevExpress XAF `MainDemo.NET.EFCore` (Blazor + EF Core), rozszerzony o wzorce, które dochodzą przy realnym wdrożeniu aplikacji biznesowej: lokalizację, branding, custom property editory, blokady UX i konfigurowalność z poziomu Model Editora.

Każdy temat ma własny dokument w `docs/` — opis w pełnych zdaniach, z fragmentami kodu, ścieżkami plików i checklistą wdrożeniową. Dokumenty są pisane jako referencja do implementacji w innych projektach XAF Blazor.

## Dokumentacja

### Wzorce gotowe do skopiowania

- **[Obsługa języka polskiego w MainDemo Blazor](docs/obsluga-jezyka-polskiego-w-main-demo-blazor.md)** — pełna konfiguracja PL/EN/DE: `RequestLocalizationOptions`, model differences `Model.DesignedDiffs.Localization.pl.xafml`, DevExpress JS localization w `wwwroot/js/scripts.js`, pułapki z kulturą fallback w testach raportów.
- **[Branding w MainDemo Blazor](docs/branding-w-main-demo-blazor.md)** — logo, pre-loader, splash screen, motywy CSS. Co podmienić, gdzie siedzą zasoby, jak zachować spójność między widokami.
- **[Custom DateEditor z blokadą kółka myszy](docs/custom-date-editor-mouse-wheel.md)** — globalny property editor dla `DateTime`/`DateTime?` z trójpoziomową konfiguracją (atrybut → ViewItem → IModelOptions), format-świadomym pokazywaniem sekcji czasu, JS-modułem ESM ładowanym przez kontroler XAF. Wersja minimalna (samym ViewController + inline `<script>`) plus wersja pełna sterowana z Model Editora. Żywy przykład: `Employee.Birthday` z `[DateEditMouseWheel(false)]`.

### Plany i decyzje architektoniczne

- **[Plan refaktoru DateEditora](docs/refaktor-dateeditor-plan.md)** — historia decyzji: rozbicie monolitowego `DateEditor.cs` na folder `Editors/Date/`, wyciągnięcie aliasów do projektu modułowego, mapowanie do struktury z `OutlookInspiredDemo` jako wzorca. Trzy etapy z checklistą per zadanie.
- **[Plan aktualizacji dokumentacji DateEditora](docs/plan-aktualizacji-doc-dateeditor.md)** — co dopisać, czego brakuje w bieżącym artykule, ryzyka dryfu snippetów względem kodu.

## Powiązany blog techniczny

Wzorce z tego repo są opisane też w serii blog-postów na [`kashiash.github.io`](https://kashiash.github.io):

1. [Obsługa języków: polski, angielski, niemiecki](https://kashiash.github.io/2026/05/12/obsluga-jezykow-blazor.html)
2. [Branding: logo, splash screen i motywy](https://kashiash.github.io/2026/05/12/branding-blazor.html)
3. [Globalny `DateTimePropertyEditor` z blokadą kółka myszy](https://kashiash.github.io/2026/05/12/xaf-blazor-date-editor-mouse-wheel.html)

Wersje w `docs/` i wersje blogowe pokrywają ten sam mechanizm. `docs/` jest skoncentrowane na konkretnym repo (ścieżki, nazwy klas, prawdziwe property), blog opisuje pattern w sposób przenośny.

## Quick start

Setup: `CLAUDE.md` w katalogu głównym repo. Komendy build/run/test, architektura modułów, central package management — wszystko tam.

```powershell
# Build
dotnet build CS\MainDemo.NET.EFCore.sln -c Debug

# Run Blazor + Web API na http://localhost:5115
dotnet run --project CS\MainDemo.Blazor.Server\MainDemo.Blazor.Server.csproj -c Debug --urls http://localhost:5115
```

DevExpress version: **v25.2.6**. Target framework: **net9.0** (`.Win` używa `net9.0-windows`).

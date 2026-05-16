# Branding DataDrive w MainDemo.Blazor.Server

Ten dokument pokazuje dokładnie, co zmieniłem w `MainDemo.Blazor.Server`, żeby podmienić branding na DataDrive.

## Zakres zmiany

Zmiana objęła:

1. assety SVG w `wwwroot/images`,
2. host `_Host.cshtml`,
3. style w `site.css`,
4. domyślny motyw w `appsettings.json`.

## Pliki graficzne

Do `CS/MainDemo.Blazor.Server/wwwroot/images/` trafiły:

```text
Logo.svg
SplashScreen.svg
fleet-management-software.svg
```

Role tych plików są różne:

1. `Logo.svg` idzie do nagłówka przez CSS maskę,
2. `SplashScreen.svg` idzie do komponentu `SplashScreen`,
3. `fleet-management-software.svg` idzie do preloadera przed spinnerem.

## Plik 1. `CS/MainDemo.Blazor.Server/Pages/_Host.cshtml`

To jest dokładna zawartość po zmianie:

```cshtml
@page "/"
@namespace MainDemo.Blazor.Server
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@using DevExpress.ExpressApp.Blazor.Components

<!DOCTYPE html>
<html lang="en">

<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0, shrink-to-fit=no" />
    <meta name="mobile-web-app-capable" content="yes" />
    <meta name="description" content="This demo allows you to store contacts, tasks, events, reports and other related data. It includes reusable XAF modules such Reports, Office, Scheduler, View Variants, AuditTrail, FileAttachments." />
    <meta name="keywords" content="DevExpress, Blazor, Security, Authentication, Authorization, Web, Service, API, Cloud, EF Core, Access Control, RBAC, Swagger, OData, Reporting, Dashboard, Validation, Audit Trail, Office, File Management" />
    <meta property="og:type" content="website" />
    <meta property="og:title" content="DataDrive" />
    <meta property="og:description" content="This demo allows you to store contacts, tasks, events, reports and other related data. It includes reusable XAF modules such Reports, Office, Scheduler, View Variants, AuditTrail, FileAttachments." />
    <meta property="og:image" content="https://www.devexpress.com/support/demos/i/demo-thumbs/xaf-main-demo.png" />
    <meta property="og:url" content="https://demos.devexpress.com/XAF/BlazorMainDemo" />
    <title>DataDrive</title>
    <base href="~/" />
    <!-- Google Tag Manager -->
    <script>
        (function(w, d, s, l, i) {
            w[l] = w[l] || []; w[l].push({
                'gtm.start':
                    new Date().getTime(), event: 'gtm.js'
            }); var f = d.getElementsByTagName(s)[0],
                j = d.createElement(s), dl = l != 'dataLayer' ? '&l=' + l : ''; j.async = true; j.src =
                    'https://www.googletagmanager.com/gtm.js?id=' + i + dl; f.parentNode.insertBefore(j, f);
        })(window, document, 'script', 'dataLayer', 'GTM-TP4P4KW');
    </script>
    <!-- End Google Tag Manager -->
    <component type="typeof(BootstrapThemeLink)" render-mode="Static" />
</head>
<body>
    <!-- Google Tag Manager (noscript) -->
    <noscript>
        <iframe src='https://www.googletagmanager.com/ns.html?id=GTM-TP4P4KW'
                height='0' width='0' style='display:none;visibility:hidden'></iframe>
    </noscript>
    <!-- End Google Tag Manager (noscript) -->
    <div id="preApplicationLoadingPanel" class="pre-loading-panel">
        <div class="pre-loading-image" role="img" aria-label="Fleet Management Software"></div>
    </div>
    <component type="typeof(SplashScreen)" render-mode="Static" param-Caption='"Fleet Management Software"' param-ImagePath='"images/SplashScreen.svg"' />

    <link href="_content/DevExpress.ExpressApp.Blazor/styles.css" asp-append-version="true" rel="stylesheet" />
    <link href="css/site.css" rel="stylesheet" />

    <app class="d-none">
        <component type="typeof(App)" render-mode="Server" />
    </app>

    <component type="typeof(AlertsHandler)" render-mode="Server" />

    <div id="blazor-error-ui" data-nosnippet>
        <component type="typeof(BlazorError)" render-mode="Static" />
    </div>

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
    <script src="_framework/blazor.server.js"></script>
    <script src="js/file-download.js"></script>
    <script src="js/scripts.js"></script>
</body>
</html>
```

Najważniejsze zmiany:

1. `<title>` i `og:title` zmieniły się na `DataDrive`,
2. doszedł `preApplicationLoadingPanel`,
3. `SplashScreen` dostał `param-Caption='"Fleet Management Software"'`,
4. doszedł skrypt, który wygasza i usuwa preloader.

## Plik 2. `CS/MainDemo.Blazor.Server/wwwroot/css/site.css`

To jest dokładny blok stylów, który robi branding:

```css
html, body {
    height: 100%;
}

body {
    margin: 0;
}

app {
    display: block;
    height: 100%;
}

.adsbox {
    top: 0;
}

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

#applicationLoadingPanel .loading {
    width: 360px;
    height: 360px;
}

#applicationLoadingPanel .loading-image-wrapper {
    width: 220px;
    height: 220px;
    min-width: 220px;
    min-height: 220px;
    background-color: transparent !important;
    border-radius: 50%;
}

#applicationLoadingPanel .loading-image {
    width: 180px;
    height: 180px;
    object-fit: contain;
}

#applicationLoadingPanel .loading-border {
    width: 220px !important;
    height: 220px !important;
    min-width: 220px !important;
    min-height: 220px !important;
    border-width: 8px !important;
    border-radius: 50% !important;
    box-sizing: border-box;
}

#applicationLoadingPanel .loading-floated-circle {
    width: 220px !important;
    height: 220px !important;
    min-width: 220px !important;
    min-height: 220px !important;
    border: none !important;
    border-radius: 50% !important;
    box-sizing: border-box;
    background: conic-gradient(
        from 0deg,
        transparent 0deg 300deg,
        var(--dxds-color-border-primary-default-rest, var(--bs-primary)) 300deg 360deg
    ) !important;
    -webkit-mask: radial-gradient(farthest-side, transparent calc(100% - 8px), #000 calc(100% - 8px)) !important;
    mask: radial-gradient(farthest-side, transparent calc(100% - 8px), #000 calc(100% - 8px)) !important;
}

#applicationLoadingPanel .loading-caption {
    display: none !important;
}

#logon-template-component .header-logo {
    -webkit-mask-position: center;
    mask-position: center;
}
```

Ten blok odpowiada za:

1. pełnoekranowy preloader,
2. płynne wygaszenie preloadera,
3. szerokie logo przed spinnerem,
4. geometrię logo w nagłówku,
5. rozmiary i wygląd splasha.

## Plik 3. `CS/MainDemo.Blazor.Server/appsettings.json`

Branding zahaczył też o domyślny motyw:

```json
"ThemeSwitcher": {
  "DefaultItemName": "Office White",
  "ShowSizeModeSwitcher": true,
  "Groups": [
    {
      "IsFluent": true,
      "Caption": "DevExpress Fluent",
      "Items": [
        { "Caption": "Blue", "Color": "Blue" },
        { "Caption": "Cool Blue", "Color": "CoolBlue" },
        { "Caption": "Desert", "Color": "Desert" },
        { "Caption": "Mint", "Color": "Mint" },
        { "Caption": "Moss", "Color": "Moss" },
        { "Caption": "Orchid", "Color": "Orchid" },
        { "Caption": "Purple", "Color": "Purple" },
        { "Caption": "Rose", "Color": "Rose" },
        { "Caption": "Rust", "Color": "Rust" },
        { "Caption": "Steel", "Color": "Steel" },
        { "Caption": "Storm", "Color": "Storm" }
      ]
    },
    {
      "Caption": "DevExpress Classic",
      "Items": [
        {
          "Caption": "Blazing Berry",
          "Url": "_content/DevExpress.Blazor.Themes/blazing-berry.bs5.min.css",
          "Color": "#5c2d91"
        },
        {
          "Caption": "Blazing Dark",
          "Url": "_content/DevExpress.Blazor.Themes/blazing-dark.bs5.min.css",
          "Color": "#46444a"
        },
        {
          "Caption": "Office White",
          "Url": "_content/DevExpress.Blazor.Themes/office-white.bs5.min.css",
          "Color": "#fe7109"
        },
        {
          "Caption": "Purple",
          "Url": "_content/DevExpress.Blazor.Themes/purple.bs5.min.css",
          "Color": "#7989ff"
        }
      ]
    }
  ]
}
```

Najważniejsza zmiana:

```json
"DefaultItemName": "Office White"
```

## Jak uruchomić po zmianie

```powershell
dotnet build CS\MainDemo.Blazor.Server\MainDemo.Blazor.Server.csproj -c Debug
dotnet run --project CS\MainDemo.Blazor.Server\MainDemo.Blazor.Server.csproj -c Debug --urls http://localhost:5115
```

## Co sprawdzić w przeglądarce

1. karta ma tytuł `DataDrive`,
2. na starcie widać `fleet-management-software.svg`,
3. splash używa `SplashScreen.svg`,
4. w nagłówku jest `Logo.svg`,
5. domyślny motyw to `Office White`.

## Zmienione pliki

```text
CS/MainDemo.Blazor.Server/wwwroot/images/Logo.svg
CS/MainDemo.Blazor.Server/wwwroot/images/SplashScreen.svg
CS/MainDemo.Blazor.Server/wwwroot/images/fleet-management-software.svg
CS/MainDemo.Blazor.Server/Pages/_Host.cshtml
CS/MainDemo.Blazor.Server/wwwroot/css/site.css
CS/MainDemo.Blazor.Server/appsettings.json
```

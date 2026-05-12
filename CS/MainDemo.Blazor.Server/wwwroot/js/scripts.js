window.ReportingLocalization = {
    currentCulture: null,
    resolveLocalizationCulture: function (culture) {
        if (!culture) {
            return null;
        }

        const normalizedCulture = culture.toLowerCase();
        if (normalizedCulture.startsWith("de")) {
            return "de-DE";
        }
        if (normalizedCulture.startsWith("pl")) {
            return "pl-PL";
        }

        return null;
    },
    setCurrentCulture: function (culture) {
        window.ReportingLocalization.currentCulture = culture;
    },
    onCustomizeLocalization: function (_, e) {
        const currentCulture = window.ReportingLocalization.resolveLocalizationCulture(window.ReportingLocalization.currentCulture);
        if (currentCulture) {
            e.LoadMessages($.get("js/localization/dx-analytics-core." + currentCulture + ".json"));
            e.LoadMessages($.get("js/localization/dx-reporting." + currentCulture + ".json"));
            $.get("js/localization/" + currentCulture + ".json").done(result => {
                e.WidgetLocalization.loadMessages(result);
            }).always(() => {
                e.WidgetLocalization.locale(currentCulture);
            });
        }
    }
};

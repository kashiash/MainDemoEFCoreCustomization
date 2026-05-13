window.ReportingLocalization = {
    currentCulture: null,
    loadMergedMessages: function (baseUrl, overrideUrl) {
        return $.get(baseUrl).then(baseMessages => {
            return $.get(overrideUrl)
                .then(overrideMessages => $.extend(true, {}, baseMessages, overrideMessages))
                .catch(() => baseMessages);
        });
    },
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
            const analyticsMessages = window.ReportingLocalization.loadMergedMessages(
                "js/localization/dx-analytics-core." + currentCulture + ".json",
                "js/localization/overrides/dx-analytics-core." + currentCulture + ".json"
            );
            const reportingMessages = window.ReportingLocalization.loadMergedMessages(
                "js/localization/dx-reporting." + currentCulture + ".json",
                "js/localization/overrides/dx-reporting." + currentCulture + ".json"
            );
            const widgetMessages = window.ReportingLocalization.loadMergedMessages(
                "js/localization/" + currentCulture + ".json",
                "js/localization/overrides/" + currentCulture + ".json"
            );

            e.LoadMessages(analyticsMessages);
            e.LoadMessages(reportingMessages);
            widgetMessages.done(result => {
                e.WidgetLocalization.loadMessages(result);
            }).always(() => {
                e.WidgetLocalization.locale(currentCulture);
            });
        }
    }
};

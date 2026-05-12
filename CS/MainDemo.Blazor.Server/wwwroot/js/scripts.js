window.ReportingLocalization = {
    currentCulture: null,
    setCurrentCulture: function (culture) {
        window.ReportingLocalization.currentCulture = culture;
    },
    onCustomizeLocalization: function (_, e) {
        const currentCulture = window.ReportingLocalization.currentCulture;
        if (currentCulture == "de-DE") {
            e.LoadMessages($.get("js/localization/dx-analytics-core." + currentCulture + ".json"));
            e.LoadMessages($.get("js/localization/dx-reporting." + currentCulture + ".json"));
            $.get("js/localization/" + currentCulture + ".json").done(result => {
                e.WidgetLocalization.loadMessages(result);
            }).always(() => {
                e.WidgetLocalization.locale(currentCulture);
            })
        }
    }
}
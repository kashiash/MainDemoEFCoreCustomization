using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Blazor.Editors.Adapters;
using DevExpress.ExpressApp.Blazor.Services;
using DevExpress.ExpressApp.ReportsV2.Blazor;
using Microsoft.JSInterop;

namespace MainDemo.Blazor.Server.Controllers;

public class ReportLocalizationController : ViewController<DetailView> {
    private readonly IXafCultureInfoService cultureInfoService;
    private readonly IJSRuntime jSRuntime;
    public ReportLocalizationController() { }
    [ActivatorUtilitiesConstructor]
    public ReportLocalizationController(IXafCultureInfoService cultureInfoService, IJSRuntime jSRuntime) {
        this.cultureInfoService = cultureInfoService;
        this.jSRuntime = jSRuntime;
    }
    protected override void OnActivated() {
        base.OnActivated();
        View.CustomizeViewItemControl<ReportDesignerViewItem>(this, CustomizeReportDesigner);
        View.CustomizeViewItemControl<FilterPropertyEditor>(this, CustomizeFilterPropertyEditor);
    }
    private async void CustomizeReportDesigner(ReportDesignerViewItem propertyEditor) {
        propertyEditor.CallbacksModel.CustomizeLocalization = "ReportingLocalization.onCustomizeLocalization";
        await jSRuntime.InvokeVoidAsync("ReportingLocalization.setCurrentCulture", cultureInfoService.CurrentCulture.Name);
    }
    private async void CustomizeFilterPropertyEditor(FilterPropertyEditor filterPropertyEditor) {
        filterPropertyEditor.ComponentModel.CustomizeLocalization = "ReportingLocalization.onCustomizeLocalization";
        await jSRuntime.InvokeVoidAsync("ReportingLocalization.setCurrentCulture", cultureInfoService.CurrentCulture.Name);
    }
}

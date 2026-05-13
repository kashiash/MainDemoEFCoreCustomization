using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace MainDemo.Blazor.Server.Editors.Date;

public class DateEditMouseWheelGuardController : ViewController {
    IJSRuntime jsRuntime;

    protected override void OnActivated() {
        base.OnActivated();
        jsRuntime = Application?.ServiceProvider?.GetService<IJSRuntime>();
    }

    protected override void OnViewControlsCreated() {
        base.OnViewControlsCreated();
        _ = RegisterWheelGuard();
    }

    async Task RegisterWheelGuard() {
        if (jsRuntime == null) {
            return;
        }
        try {
            var module = await jsRuntime.InvokeAsync<IJSObjectReference>(
                "import",
                "./js/maindemo-date-edit-wheel-guard.js");
            await module.InvokeVoidAsync("ensureRegistered");
            await module.DisposeAsync();
        }
        catch (JSException ex) {
            Tracing.Tracer.LogError(ex);
        }
    }
}

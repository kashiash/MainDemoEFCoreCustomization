using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Blazor.Editors.Adapters;
using DevExpress.ExpressApp.Editors;

namespace MainDemo.Blazor.Server.Controllers;

public class GlobalDateEditorTweaksController : ViewController<DetailView> {
    protected override void OnViewControlsCreated() {
        base.OnViewControlsCreated();
        foreach (var item in View.Items.OfType<DateTimePropertyEditor>()) {
            if (item.DxDateEditMaskProperties is { } maskProperties) {
                maskProperties.DateTime.CaretMode = MaskCaretMode.Advancing;
                maskProperties.DateOnly.CaretMode = MaskCaretMode.Advancing;
                maskProperties.DateTimeOffset.CaretMode = MaskCaretMode.Advancing;
            }
            Type t = item.MemberInfo.MemberType;
            if (t == typeof(DateTime) && item.Control is DxDateEditModel<DateTime> a1) {
                AppendCss(a1);
            }
            else if (t == typeof(DateTime?) && item.Control is DxDateEditModel<DateTime?> a2) {
                AppendCss(a2);
            }
        }
    }

    static void AppendCss<T>(DxDateEditModel<T> adapter) {
        const string cls = "maindemo-dateedit-wheel-blocked";
        adapter.CssClass = string.IsNullOrEmpty(adapter.CssClass) ? cls : adapter.CssClass + " " + cls;
        adapter.InputCssClass = string.IsNullOrEmpty(adapter.InputCssClass) ? cls : adapter.InputCssClass + " " + cls;
    }
}

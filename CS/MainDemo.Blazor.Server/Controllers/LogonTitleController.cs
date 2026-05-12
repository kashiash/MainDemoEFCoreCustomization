using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;

namespace MainDemo.Blazor.Server.Controllers;

public class LogonTitleController : WindowController {
    protected override void OnActivated() {
        base.OnActivated();
        WindowTemplateController controller = Frame.GetController<WindowTemplateController>();
        controller.CustomizeWindowCaption += Controller_CustomizeWindowCaption;
    }
    private void Controller_CustomizeWindowCaption(object sender, CustomizeWindowCaptionEventArgs e) {
        if(Frame.Context == TemplateContext.LogonWindow) {
            e.WindowCaption.Text = Application.Title;
        }
    }
    protected override void OnDeactivated() {
        base.OnDeactivated();
        WindowTemplateController controller = Frame.GetController<WindowTemplateController>();
        controller.CustomizeWindowCaption -= Controller_CustomizeWindowCaption;
    }
}

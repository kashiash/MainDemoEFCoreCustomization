using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.SystemModule;
using MainDemo.Module.Storages;

namespace MainDemo.Module.Controllers;

public class DynamicAppearanceRuleViewController : ObjectViewController<ObjectView, object> {
    private AppearanceController appearanceController;

    protected override void OnActivated() {
        base.OnActivated();
        appearanceController = Frame.GetController<AppearanceController>();
        if(appearanceController == null) {
            return;
        }
        appearanceController.ResetRulesCache();
        appearanceController.CollectAppearanceRules += AppearanceController_CollectAppearanceRules;
        appearanceController.Refresh();
    }

    protected override void OnDeactivated() {
        if(appearanceController != null) {
            appearanceController.CollectAppearanceRules -= AppearanceController_CollectAppearanceRules;
            appearanceController = null;
        }
        base.OnDeactivated();
    }

    private void AppearanceController_CollectAppearanceRules(object sender, CollectAppearanceRulesEventArgs e) {
        if(View?.ObjectTypeInfo?.Type == null) {
            return;
        }
        foreach(var rule in DynamicAppearanceRuleStorage.GetRules(View.ObjectTypeInfo.Type, View.Id)) {
            e.AppearanceRules.Add(rule);
        }
    }
}

using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Updating;
using DevExpress.ExpressApp.Win;
using DevExpress.ExpressApp.Win.Templates;

namespace MainDemo.Win;

[ToolboxItemFilter("Xaf.Platform.Win")]
public sealed class MainDemoWinModule : ModuleBase {
    public MainDemoWinModule() {
        Description = "MainDemo Win module";
        ResourcesExportedToModel.Add(typeof(LightStyleMainRibbonFormTemplateLocalizer));
        ResourcesExportedToModel.Add(typeof(DetailRibbonFormV2TemplateLocalizer));
        ResourcesExportedToModel.Add(typeof(DevExpress.ExpressApp.Win.Templates.MainFormTemplateLocalizer));
        ResourcesExportedToModel.Add(typeof(DevExpress.ExpressApp.Win.Templates.DetailViewFormTemplateLocalizer));
        ResourcesExportedToModel.Add(typeof(DevExpress.ExpressApp.Win.Templates.NestedFrameTemplateLocalizer));
        ResourcesExportedToModel.Add(typeof(DevExpress.ExpressApp.Win.Templates.LookupControlTemplateLocalizer));
        ResourcesExportedToModel.Add(typeof(DevExpress.ExpressApp.Win.Templates.PopupFormTemplateLocalizer));
    }

    public override void Setup(XafApplication application) {
        base.Setup(application);
        application.SetupComplete += new EventHandler<EventArgs>(application_SetupComplete);
    }
    private void application_SetupComplete(object sender, EventArgs e) {
        ((XafApplication)sender).SetupComplete -= new EventHandler<EventArgs>(application_SetupComplete);
        WinApplication application = sender as WinApplication;
        if(FrameworkSettings.DefaultSettingsCompatibilityMode < FrameworkSettingsCompatibilityMode.v23_2) {
            foreach(IModelClass modelClass in application.Model.BOModel) {
                if(modelClass.TypeInfo.Type.FullName == typeof(DevExpress.ExpressApp.Security.AuthenticationStandardLogonParameters).FullName) {
                    modelClass.DefaultDetailView = (IModelDetailView)modelClass.Application.Views["AuthenticationStandardLogonParameters_DetailView_Demo"];
                }
            }
        }
    }
}

using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Blazor.SystemModule;
using DevExpress.ExpressApp.EFCore;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Utils;
using MainDemo.Blazor.Server.Controllers;
using Microsoft.EntityFrameworkCore;

namespace MainDemo.Blazor.Server;
public class MainDemoBlazorApplication : BlazorApplication {
    class EmptySettingsStorage : SettingsStorage {
        public override string LoadOption(string optionPath, string optionName) => null;
        public override void SaveOption(string optionPath, string optionName, string optionValue) { }
    }
    public MainDemoBlazorApplication() {
        AboutInfo.Instance.Version = "Version " + AssemblyInfo.FileVersion;
        AboutInfo.Instance.Copyright = AssemblyInfo.AssemblyCopyright + " All Rights Reserved";
    }
    protected override void OnSetupStarted() {
        base.OnSetupStarted();
#if DEBUG || EASYTEST
        if(CheckCompatibilityType == CheckCompatibilityType.DatabaseSchema) {
            DatabaseUpdateMode = DatabaseUpdateMode.UpdateDatabaseAlways;
        }
#endif
    }
    protected override List<Controller> CreateLogonWindowControllers() {
        var controllers = base.CreateLogonWindowControllers();
        controllers.Add(new LogonTitleController());
        return controllers;
    }
    private void MainDemoBlazorApplication_DatabaseVersionMismatch(object sender, DatabaseVersionMismatchEventArgs e) {
        e.Updater.Update();
        e.Handled = true;
    }
    protected override SettingsStorage CreateLogonParameterStoreCore() {
        return new EmptySettingsStorage();
    }
}


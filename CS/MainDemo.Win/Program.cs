using System.Configuration;
using System.Reflection;
using Demos.Data;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Utils;
using DevExpress.ExpressApp.Win;
using DevExpress.ExpressApp.Win.EasyTest;
using DevExpress.ExpressApp.Win.Utils;
using DevExpress.Internal;
using DevExpress.Persistent.Base;
using DevExpress.XtraEditors;

namespace MainDemo.Win;

public class Program {
    private static bool ContainsArgument(string[] args, string argument) {
        return args.Any(arg => arg.TrimStart('/').TrimStart('-').ToLower() == argument.ToLower());
    }
    [STAThread]
    public static int Main(string[] arguments) {
        if(ContainsArgument(arguments, "help") || ContainsArgument(arguments, "h")) {
            Console.WriteLine("Updates the database when its version does not match the application's version.");
            Console.WriteLine();
            Console.WriteLine($"    {Assembly.GetExecutingAssembly().GetName().Name}.exe --updateDatabase [--forceUpdate --silent]");
            Console.WriteLine();
            Console.WriteLine("--forceUpdate - Marks that the database must be updated whether its version matches the application's version or not.");
            Console.WriteLine("--silent - Marks that database update proceeds automatically and does not require any interaction with the user.");
            Console.WriteLine();
            Console.WriteLine($"Exit codes: 0 - {DBUpdaterStatus.UpdateCompleted}");
            Console.WriteLine($"            1 - {DBUpdaterStatus.UpdateError}");
            Console.WriteLine($"            2 - {DBUpdaterStatus.UpdateNotNeeded}");
            return 0;
        }
        if(!System.Windows.Forms.SystemInformation.TerminalServerSession && Screen.AllScreens.Length > 1)
            DevExpress.XtraEditors.WindowsFormsSettings.SetPerMonitorDpiAware();
        else
            DevExpress.XtraEditors.WindowsFormsSettings.SetDPIAware();

        FrameworkSettings.DefaultSettingsCompatibilityMode = FrameworkSettingsCompatibilityMode.Latest;
        DevExpress.ExpressApp.Security.SecurityStrategy.AutoAssociationReferencePropertyMode = DevExpress.ExpressApp.Security.ReferenceWithoutAssociationPermissionsMode.AllMembers;
        WindowsFormsSettings.LoadApplicationSettings();
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        DevExpress.Utils.ToolTipController.DefaultController.ToolTipType = DevExpress.Utils.ToolTipType.SuperTip;


        InitializeTracing();

#if DEBUG
        EasyTestRemotingRegistration.Register();
#endif

        WinApplication winApplication = null;
        try {
            Initialization.RunSecurityServer(arguments);
            winApplication = ApplicationBuilder.BuildApplication();
            if(ContainsArgument(arguments, "updateDatabase")) {
                return 0;
            }
            winApplication.Setup();
            winApplication.Start();
        }
        catch(Exception e) {
            winApplication?.StopSplash();
            winApplication?.HandleException(e);
        }
        finally {
            Initialization.KillServerProcess();
        }
        return 0;
    }

    static void InitializeTracing() {
        if(Tracing.GetFileLocationFromSettings() == DevExpress.Persistent.Base.FileLocation.CurrentUserApplicationDataFolder) {
            Tracing.LocalUserAppDataPath = Application.LocalUserAppDataPath;
        }
        Tracing.Initialize();
    }
}

using System.Reflection;
using Demos.Data;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.AspNetCore.DesignTime;
using DevExpress.ExpressApp.Design;
using DevExpress.ExpressApp.Utils;

namespace MainDemo.MiddleTier.WebApi;
public class ApplicationFactory : IDesignTimeApplicationFactory {
    XafApplication IDesignTimeApplicationFactory.Create() {
        IHostBuilder hostBuilder = CreateHostBuilder(Array.Empty<string>());
        return DesignTimeApplicationFactoryHelper.Create(hostBuilder);
    }
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder => {
                webBuilder.UseStartup<Startup>();
            });
}

public class Program {
    private static bool ContainsArgument(string[] args, string argument) {
        return args.Any(arg => arg.TrimStart('/').TrimStart('-').ToLower() == argument.ToLower());
	}
    public static int Main(string[] args) {
        if(ContainsArgument(args, "help") || ContainsArgument(args, "h")) {
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
        }
        else {
            DevExpress.ExpressApp.FrameworkSettings.DefaultSettingsCompatibilityMode = DevExpress.ExpressApp.FrameworkSettingsCompatibilityMode.Latest;
            DevExpress.ExpressApp.Security.SecurityStrategy.AutoAssociationReferencePropertyMode = DevExpress.ExpressApp.Security.ReferenceWithoutAssociationPermissionsMode.AllMembers;
            IHost host = ApplicationFactory.CreateHostBuilder(args).Build();
            if(ContainsArgument(args, "updateDatabase")) {
                using(var serviceScope = host.Services.CreateScope()) {
                    serviceScope.ServiceProvider.GetRequiredService<DevExpress.ExpressApp.Utils.IDBUpdater>().Update(ContainsArgument(args, "forceUpdate"), ContainsArgument(args, "silent"));
                    return 0;
                }
            }
            else {
                host.Run();
            }
        }
        return 0;
    }
}

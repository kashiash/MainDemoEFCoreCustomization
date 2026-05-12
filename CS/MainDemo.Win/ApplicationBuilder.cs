using System.Configuration;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ApplicationBuilder;
using DevExpress.ExpressApp.Design;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Templates.ActionControls;
using DevExpress.ExpressApp.Updating;
using DevExpress.ExpressApp.Win;
using DevExpress.ExpressApp.Win.ApplicationBuilder;
using DevExpress.ExpressApp.Win.Templates;
using DevExpress.ExpressApp.Win.Templates.Bars.ActionControls;
using DevExpress.XtraBars;
using DevExpress.XtraBars.Ribbon;
using MainDemo.Module;
using MainDemo.Module.BusinessObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MainDemo.Win;

public class ApplicationBuilder : IDesignTimeApplicationFactory {
    public static WinApplication BuildApplication() {
        var builder = WinApplication.CreateBuilder();
        // Register custom services for Dependency Injection. For more information, refer to the following topic: https://docs.devexpress.com/eXpressAppFramework/404430/
        // builder.Services.AddScoped<CustomService>();
        // Register 3rd-party IoC containers (like Autofac, Dryloc, etc.)
        // builder.UseServiceProviderFactory(new DryIocServiceProviderFactory());
        // builder.UseServiceProviderFactory(new AutofacServiceProviderFactory());
        builder.Services.AddDistributedMemoryCache();
        builder.UseApplication<MainDemoWinApplication>();
        builder.Modules
            .AddAuditTrailEFCore()
            .AddCloning()
            .AddTreeListEditors()
            .AddConditionalAppearance()
            .AddDashboards(options => {
                options.DashboardDataType = typeof(DevExpress.Persistent.BaseImpl.EF.DashboardData);
            })
            .AddFileAttachments()
            .AddNotifications()
            .AddOffice()
            .AddReports(options => {
                options.EnableInplaceReports = true;
                options.ReportDataType = typeof(DevExpress.Persistent.BaseImpl.EF.ReportDataV2);
                options.ReportStoreMode = DevExpress.ExpressApp.ReportsV2.ReportStoreModes.XML;
            })
            .AddScheduler()
            .AddValidation(options => {
                options.AllowValidationDetailsAccess = false;
            })
            .AddViewVariants()
            .AddMainDemoModule()
            .Add<MainDemoWinModule>();
        builder.ObjectSpaceProviders
           .AddEFCore(options => options.PreFetchReferenceProperties())
               .WithDbContext<MainDemoDbContext>((application, options) => {
                   options.UseMiddleTier(application.Security);
                   options.UseChangeTrackingProxies();
                   options.UseObjectSpaceLinkProxies();
               })
           .AddNonPersistent();
        builder.Security
            .UseMiddleTierMode(options => {
                options.WaitForMiddleTierServerReady();
                options.BaseAddress = new Uri("http://localhost:5000/");
            })
            .AddPasswordAuthentication()
            .AddWindowsAuthentication();
            /* .AddAzureAD(opt => {
                 opt.ClientId = ConfigurationManager.AppSettings["azureAD_ClientId"];
                 opt.TenantId = ConfigurationManager.AppSettings["azureAD_TenantId"];
                 opt.Instance = ConfigurationManager.AppSettings["azureAD_Instance"];
                 opt.Scopes = new[] { ConfigurationManager.AppSettings["azureAD_Scopes"] };
                 opt.SchemeName = ConfigurationManager.AppSettings["middleTierServer_SchemeName"];
            }); */

        builder.AddBuildStep(application => {
            application.DatabaseUpdateMode = DatabaseUpdateMode.Never;
            ((WinApplication)application).SplashScreen = new DevExpress.ExpressApp.Win.Utils.DXSplashScreen(
                typeof(Demos.Win.XafDemoSplashScreen),
                new DefaultOverlayFormOptions());
            application.ApplicationName = "MainDemo";
            DevExpress.ExpressApp.Scheduler.Win.SchedulerListEditor.DailyPrintStyleCalendarHeaderVisible = false;


            application.DatabaseVersionMismatch += (s, e) => {
                string message = "Application cannot connect to the specified database.";
                CompatibilityDatabaseIsOldError isOldError = e.CompatibilityError as CompatibilityDatabaseIsOldError;
                if(isOldError != null && isOldError.Module != null) {
                    message = "The client application cannot connect to the Middle Tier Application Server and its database. " +
                              "To avoid this error, ensure that both the client and the server have the same modules set. Problematic module: " + isOldError.Module.Name +
                              ". For more information, see https://docs.devexpress.com/eXpressAppFramework/113439/concepts/security-system/middle-tier-security-wcf-service#troubleshooting";
                }
                if(e.CompatibilityError == null) {
                    message = "You probably tried to update the database in Middle Tier Security mode from the client side. " +
                              "In this mode, the server application updates the database automatically. " +
                              "To disable the automatic database update, set the XafApplication.DatabaseUpdateMode property to the DatabaseUpdateMode.Never value in the client application.";
                }
                throw new InvalidOperationException(message);
            };
            application.LastLogonParametersRead += (s, e) => {
                if(e.LogonObject is AuthenticationStandardLogonParameters logonParameters && string.IsNullOrEmpty(logonParameters.UserName)) {
                    logonParameters.UserName = "Sam";
                }
            };
            application.CustomizeFormattingCulture += new EventHandler<CustomizeFormattingCultureEventArgs>(winApplication_CustomizeFormattingCulture);
            application.LastLogonParametersReading += new EventHandler<LastLogonParametersReadingEventArgs>(winApplication_LastLogonParametersReading);
            application.CustomizeTemplate += new EventHandler<CustomizeTemplateEventArgs>(WinApplication_CustomizeTemplate);
        });

        return builder.Build();
    }

    private static void WinApplication_CustomizeTemplate(object sender, CustomizeTemplateEventArgs e) {
        if(e.Context == TemplateContext.ApplicationWindow || e.Context == TemplateContext.View) {
            var ribbonForm = e.Template as RibbonForm;
            var actionControlsSite = ribbonForm as IActionControlsSite;
            if((ribbonForm != null) && (actionControlsSite != null)) {
                var filtersActionControlContainer = actionControlsSite.ActionContainers.FirstOrDefault<IActionControlContainer>(x => x.ActionCategory == "Filters");
                if(filtersActionControlContainer is BarLinkActionControlContainer) {
                    BarLinkActionControlContainer barFiltersActionControlContainer = (BarLinkActionControlContainer)filtersActionControlContainer;
                    BarLinkContainerItem barFiltersItem = barFiltersActionControlContainer.BarContainerItem;
                    RibbonControl ribbonControl = ribbonForm.Ribbon;
                    foreach(RibbonPage page in ribbonControl.Pages) {
                        foreach(RibbonPageGroup group in page.Groups) {
                            var barFiltersItemLink = group.ItemLinks.FirstOrDefault<BarItemLink>(x => x.Item == barFiltersItem);
                            if(barFiltersItemLink != null) {
                                group.ItemLinks.Remove(barFiltersItemLink);
                            }
                        }
                    }
                    ribbonForm.Ribbon.PageHeaderItemLinks.Add(barFiltersItem);
                }

            }
        }
        else if((e.Context == TemplateContext.LookupControl) || (e.Context == TemplateContext.LookupWindow)) {
            var lookupControlTemplate = e.Template as LookupControlTemplate;
            if(lookupControlTemplate == null && e.Template is LookupForm) {
                lookupControlTemplate = ((LookupForm)e.Template).FrameTemplate;
            }
            if(lookupControlTemplate != null) {
                lookupControlTemplate.ObjectsCreationContainer.ContainerId = "LookupNew";
                lookupControlTemplate.SearchActionContainer.ContainerId = "LookupFullTextSearch";
            }
        }
    }

    private static void winApplication_CustomizeFormattingCulture(object sender, CustomizeFormattingCultureEventArgs e) {
        e.FormattingCulture = CultureInfo.GetCultureInfo("en-US");
    }

    private static void winApplication_LastLogonParametersReading(object sender, LastLogonParametersReadingEventArgs e) {
        if(string.IsNullOrWhiteSpace(e.SettingsStorage.LoadOption("", "UserName"))) {
            e.SettingsStorage.SaveOption("", "UserName", "Sam");
        }
    }

    XafApplication IDesignTimeApplicationFactory.Create() {
        DevExpress.EntityFrameworkCore.Security.MiddleTier.ClientServer.MiddleTierClientSecurity.DesignModeUserType = typeof(MainDemo.Module.BusinessObjects.ApplicationUser);
        DevExpress.EntityFrameworkCore.Security.MiddleTier.ClientServer.MiddleTierClientSecurity.DesignModeRoleType = typeof(DevExpress.Persistent.BaseImpl.EF.PermissionPolicy.PermissionPolicyRole);
        return BuildApplication();
    }
}

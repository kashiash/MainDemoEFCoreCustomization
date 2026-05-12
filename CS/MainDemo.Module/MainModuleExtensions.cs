using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ApplicationBuilder;
using DevExpress.ExpressApp.Security;
using DevExpress.Persistent.BaseImpl.EF.PermissionPolicy;
using MainDemo.Module.BusinessObjects;
using MainDemo.NonPersistent;
using Microsoft.Extensions.DependencyInjection;

namespace MainDemo.Module;

public static class MainDemoModuleExtensions {
    public static IModuleBuilder<TBuilder> AddMainDemoModule<TBuilder>(
            this IModuleBuilder<TBuilder> builder)
            where TBuilder : IXafApplicationBuilder<TBuilder> {

        builder.Add<MainDemoModule>();

        IServiceCollection services = builder.Context.Services;
        services.ConfigureSecurity();
        services.ConfigureNonPersistentDataProvider();

        return builder;
    }

    static IServiceCollection ConfigureSecurity(this IServiceCollection services) {
        services.PostConfigure<SecurityOptions>(options => {
            options.Lockout.Enabled = true;
            options.Lockout.MaxFailedAccessAttempts = 3;

            options.RoleType = typeof(PermissionPolicyRole);
            options.UserType = typeof(ApplicationUser);
            options.UserTokenType = typeof(UserToken);
            options.UserLoginInfoType = typeof(ApplicationUserLoginInfo);
            options.SupportNavigationPermissionsForTypes = false;
            options.Events.OnSecurityStrategyCreated += securityStrategy => {
                // Use the 'PermissionsReloadMode.NoCache' option to load the most recent permissions from the database
                // once for every DbContext instance when secured data is accessed through this instance for the first time.
                // Use the 'PermissionsReloadMode.CacheOnFirstAccess' option to reduce the number of database queries.
                // In this case, permission requests are loaded and cached when secured data is accessed for the first time.
                // and used until the current user logs out. 
                // See the following article for more details: https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.Security.SecurityStrategy.PermissionsReloadMode.
                ((SecurityStrategy)securityStrategy).PermissionsReloadMode = PermissionsReloadMode.CacheOnFirstAccess;
            };
        });
        return services;
    }

    static IServiceCollection ConfigureNonPersistentDataProvider(this IServiceCollection services) {
        services.AddSingleton<NonPersistentGlobalObjectStorage>();

        services.PostConfigure<ObjectSpaceProviderOptions>(options => {
            options.Events.OnObjectSpaceCreated += context => {
                if(context.ObjectSpace is NonPersistentObjectSpace nonPersistentObjectSpace) {
                    new NonPersistentObjectSpaceExtender(context.ServiceProvider, nonPersistentObjectSpace);
                }
            };
        });
        return services;
    }
}


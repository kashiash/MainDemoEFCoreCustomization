using System.Security.Claims;
using DevExpress.ExpressApp.Core;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Security.Authentication.Internal;
using MainDemo.Module.BusinessObjects;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MainDemo.WebAPI.TestInfrastructure {
    public static class XafSecurity_ServiceProviderExtensions {
        public static void Authenticate(this IServiceProvider serviceProvider, string userName) {
            var signInManager = serviceProvider.GetRequiredService<SignInManager>();
            var result = signInManager.SignInByPassword(userName, "");
            if(!result.Succeeded) {
                throw result.Error;
            }

            var securityStrategy = serviceProvider.GetRequiredService<ISecurityStrategyBase>();
            Assert.Equal(securityStrategy.UserName, userName);
        }
    }
}

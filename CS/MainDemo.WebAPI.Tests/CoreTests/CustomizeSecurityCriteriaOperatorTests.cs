using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Security;
using MainDemo.Module.BusinessObjects;
using MainDemo.WebAPI.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MainDemo.WebAPI.Tests {
    public class CustomizeSecurityCriteriaOperatorTests : BaseWebApiTest {
        public CustomizeSecurityCriteriaOperatorTests(SharedTestHostHolder fixture) : base(fixture) { }

        [Fact]
        public void Load_user_object_using_CurrentUserId_custom_function() {
            using var scope = fixture.Host.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();
            scope.ServiceProvider.Authenticate("John");


            var security = scope.ServiceProvider.GetRequiredService<ISecurityProvider>().GetSecurity();
            var user_fromSecurity = (ApplicationUser)security.User;

            using var os_Secured = scope.ServiceProvider.GetRequiredService<IObjectSpaceFactory>().CreateObjectSpace<ApplicationUser>();
            var user_fromSecuredOS = os_Secured.FindObject<ApplicationUser>(CriteriaOperator.Parse("ID=CurrentUserId()"));

            using var os_NonSecured = scope.ServiceProvider.GetRequiredService<INonSecuredObjectSpaceFactory>().CreateNonSecuredObjectSpace<ApplicationUser>();
            var user_fromNonSecuredOS = os_NonSecured.FindObject<ApplicationUser>(CriteriaOperator.Parse("ID=CurrentUserId()"));

            Assert.Equal(user_fromSecurity.ID, user_fromSecuredOS.ID);
            Assert.Equal(user_fromSecurity.ID, user_fromNonSecuredOS.ID);
        }

        [Fact]
        public void Load_user_object_using_Custom_function() {
            using var scope = fixture.Host.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();
            scope.ServiceProvider.Authenticate("John");


            var security = scope.ServiceProvider.GetRequiredService<ISecurityProvider>().GetSecurity();
            var user_fromSecurity = (ApplicationUser)security.User;

            using var os_Secured = scope.ServiceProvider.GetRequiredService<IObjectSpaceFactory>().CreateObjectSpace<ApplicationUser>();
            var user_fromSecuredOS = os_Secured.FindObject<ApplicationUser>(CriteriaOperator.Parse("ID=MyCustomCurrentUserId()"));

            using var os_NonSecured = scope.ServiceProvider.GetRequiredService<INonSecuredObjectSpaceFactory>().CreateNonSecuredObjectSpace<ApplicationUser>();
            var user_fromNonSecuredOS = os_NonSecured.FindObject<ApplicationUser>(CriteriaOperator.Parse("ID=MyCustomCurrentUserId()"));

            Assert.Equal(user_fromSecurity.ID, user_fromSecuredOS.ID);
            Assert.Equal(user_fromSecurity.ID, user_fromNonSecuredOS.ID);
        }
    }
}

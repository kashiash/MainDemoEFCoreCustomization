using DevExpress.ExpressApp;
using MainDemo.Module.BusinessObjects;
using MainDemo.Module.Storages;
using MainDemo.WebAPI.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MainDemo.WebAPI.Tests;

public class DynamicAppearanceRuleTests : BaseWebApiTest {
    public DynamicAppearanceRuleTests(SharedTestHostHolder fixture) : base(fixture) { }

    [Fact]
    public void Seeded_dynamic_appearance_rule_exists_in_database() {
        using var scope = fixture.Host.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();
        scope.ServiceProvider.Authenticate("Sam");
        using var objectSpace = scope.ServiceProvider
            .GetRequiredService<IObjectSpaceFactory>()
            .CreateObjectSpace<DynamicAppearanceRule>();

        var rule = objectSpace.FirstOrDefault<DynamicAppearanceRule>(x => x.Name == "Highlight overdue tasks");

        Assert.NotNull(rule);
        Assert.Equal(typeof(DemoTask), rule.DataType);
        Assert.Equal("Subject;DueDate;AssignedTo", rule.TargetItems);
        Assert.Equal("Any", rule.Context);
        Assert.Equal("ViewItem", rule.AppearanceItemType);
        Assert.Equal(System.Drawing.Color.Firebrick, rule.FontColor);
    }

    [Fact]
    public void Storage_returns_rules_only_for_matching_type() {
        using var scope = fixture.Host.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();
        scope.ServiceProvider.Authenticate("Sam");
        using var objectSpace = scope.ServiceProvider
            .GetRequiredService<IObjectSpaceFactory>()
            .CreateObjectSpace<DynamicAppearanceRule>();
        DynamicAppearanceRuleStorage.Initialize(objectSpace.GetObjects<DynamicAppearanceRule>());

        var taskRules = DynamicAppearanceRuleStorage.GetRules(typeof(DemoTask), "AnyView");
        Assert.Contains(taskRules, rule => rule.DeclaringType == typeof(DemoTask));

        var employeeRules = DynamicAppearanceRuleStorage.GetRules(typeof(Employee), "AnyView");
        Assert.DoesNotContain(employeeRules, rule => rule.DeclaringType == typeof(DemoTask));
    }
}

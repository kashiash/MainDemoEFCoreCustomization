using DevExpress.ExpressApp.ConditionalAppearance;
using MainDemo.Module.BusinessObjects;

namespace MainDemo.Module.Storages;

public static class DynamicAppearanceRuleStorage {
    private static readonly Lock SyncRoot = new();
    private static List<DynamicAppearanceRule> rules = new();

    public static void Initialize(IEnumerable<DynamicAppearanceRule> sourceRules) {
        lock(SyncRoot) {
            rules = sourceRules
                .Where(rule => rule != null)
                .ToList();
        }
    }

    public static IReadOnlyList<DynamicAppearanceRule> GetRules() {
        lock(SyncRoot) {
            return rules.ToList();
        }
    }

    public static IReadOnlyList<IAppearanceRuleProperties> GetRules(Type objectType, string viewId) {
        lock(SyncRoot) {
            return rules
                .Where(rule => rule.Matches(objectType, viewId))
                .Cast<IAppearanceRuleProperties>()
                .ToList();
        }
    }

    public static void Put(DynamicAppearanceRule rule) {
        if(rule == null) {
            return;
        }
        lock(SyncRoot) {
            var index = rules.FindIndex(existing => existing.ID == rule.ID);
            if(index >= 0) {
                rules[index] = rule;
            }
            else {
                rules.Add(rule);
            }
        }
    }

    public static void Remove(DynamicAppearanceRule rule) {
        if(rule == null) {
            return;
        }
        lock(SyncRoot) {
            rules.RemoveAll(existing => existing.ID == rule.ID);
        }
    }
}

using DevExpress.Persistent.BaseImpl.EF.PermissionPolicy;
using DevExpress.Persistent.Validation;

namespace MainDemo.Module.CodeRules;
[CodeRule]
internal class RuleMemberPermissionsCriteria : RuleCriteriaValidationBase {
    public RuleMemberPermissionsCriteria() : base("RuleMemberPermissionsCriteria", "Save", typeof(PermissionPolicyMemberPermissionsObject)) { }
    public RuleMemberPermissionsCriteria(IRuleBaseProperties properties) : base(properties) { }
    protected override string TargetPropertyName => nameof(PermissionPolicyMemberPermissionsObject.Criteria);
}

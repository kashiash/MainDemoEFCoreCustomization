using System;
using System.Collections.Generic;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;

namespace MainDemo.Blazor.Server;

[PropertyEditor(typeof(Type), true)]
public class CustomTypePropertyEditor : TypePropertyEditor {
    private readonly static ICollection<Type> suitableTypes = new HashSet<Type>() {
          typeof(MainDemo.Module.BusinessObjects.Employee),
          typeof(MainDemo.Module.BusinessObjects.Department),
          typeof(DevExpress.Persistent.BaseImpl.EF.PermissionPolicy.PermissionPolicyMemberPermissionsObject),
          typeof(DevExpress.Persistent.BaseImpl.EF.PermissionPolicy.PermissionPolicyObjectPermissionsObject),
          typeof(MainDemo.Module.BusinessObjects.Paycheck),
          typeof(MainDemo.Module.BusinessObjects.Position),
          typeof(MainDemo.Module.BusinessObjects.Resume),
          typeof(DevExpress.Persistent.BaseImpl.EF.PermissionPolicy.PermissionPolicyRole),
          typeof(DevExpress.Persistent.BaseImpl.EF.Event),
          typeof(MainDemo.Module.BusinessObjects.DemoTask),
          typeof(DevExpress.Persistent.BaseImpl.EF.PermissionPolicy.PermissionPolicyTypePermissionObject),
          typeof(DevExpress.Persistent.BaseImpl.EF.PermissionPolicy.PermissionPolicyUser),
          typeof(MainDemo.Module.BusinessObjects.ApplicationUser)
        };
    public CustomTypePropertyEditor(Type objectType, IModelMemberViewItem model) : base(objectType, model) { }
    protected override bool IsSuitableType(Type type) {
        return base.IsSuitableType(type) && suitableTypes.Contains(type);
    }
}

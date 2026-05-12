using System.Collections.ObjectModel;
using System.ComponentModel;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace MainDemo.Module.BusinessObjects;

[DefaultClassOptions]
[DefaultProperty(nameof(Position.Title))]
public class Position : BaseObject {

    [RuleRequiredField("RuleRequiredField for Position.Title", DefaultContexts.Save)]
    public virtual string Title { get; set; }

    public virtual IList<Department> Departments { get; set; } = new ObservableCollection<Department>();

    public virtual IList<Employee> Employees { get; set; } = new ObservableCollection<Employee>();
}

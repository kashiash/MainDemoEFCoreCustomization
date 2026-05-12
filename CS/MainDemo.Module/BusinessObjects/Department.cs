using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Base.General;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using Microsoft.EntityFrameworkCore;

namespace MainDemo.Module.BusinessObjects;

[DefaultClassOptions]
[DefaultProperty(nameof(Department.Title))]
[RuleCriteria("Department_PositionsIsNotEmpty", DefaultContexts.Save, "Positions.Count > 0", CustomMessageTemplate = "The Department must contain at least one position.")]
[RuleCriteria("Department_EmployeesIsNotEmpty", DefaultContexts.Save, "Employees.Count > 0", CustomMessageTemplate = "The Department must contain at least one employee.")]
public class Department : BaseObject, ITreeNode {

    [RuleRequiredField]
    public virtual string Title { get; set; }

    public virtual string Office { get; set; }

    public virtual IList<Position> Positions { get; set; } = new ObservableCollection<Position>();

    public virtual IList<Employee> Employees { get; set; } = new ObservableCollection<Employee>();

    public virtual IList<Department> Departments { get; set; } = new ObservableCollection<Department>();

    public virtual string Location { get; set; }

    [StringLength(4096)]
    public virtual string Description { get; set; }

    [HideInUI(HideInUI.All)]
    public virtual Department Parent { get; set; }

    [DataSourceProperty("Employees", DataSourcePropertyIsNullMode.SelectAll)]
    [RuleRequiredField]
    public virtual Employee DepartmentHead { get; set; }

    #region ITreeNode
    string ITreeNode.Name => Title;
    ITreeNode ITreeNode.Parent => Parent;
    IBindingList childrenBindingList;
    IBindingList ITreeNode.Children {
        get {
            if(childrenBindingList == null) {
                if(Departments is ObservableCollection<Department> observable) {
                    childrenBindingList = observable.ToBindingList();
                }
                else if(Departments is not null) {
                    childrenBindingList = new BindingList<Department>(Departments);
                }
            }
            return childrenBindingList;
        }
    }
    #endregion
}

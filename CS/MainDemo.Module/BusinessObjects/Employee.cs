using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Filtering;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.BaseImpl.EFCore.AuditTrail;
using DevExpress.Persistent.Validation;

namespace MainDemo.Module.BusinessObjects;

[DefaultClassOptions]
[RuleIsReferenced("", DefaultContexts.Delete, typeof(Employee), "Manager", InvertResult = true,
   CriteriaEvaluationBehavior = CriteriaEvaluationBehavior.BeforeTransaction,
   MessageTemplateMustBeReferenced = "The {TargetObject} object must not be referenced.")]
[RuleIsReferenced("", DefaultContexts.Delete, typeof(Department), "DepartmentHead", InvertResult = true,
   CriteriaEvaluationBehavior = CriteriaEvaluationBehavior.BeforeTransaction,
   MessageTemplateMustBeReferenced = "The {TargetObject} object must not be referenced.")]
[DefaultProperty(nameof(FullName))]
[ImageName("BO_Person")]
public class Employee : BaseObject {
    public virtual String FirstName { get; set; }

    public virtual String LastName { get; set; }

    public virtual String MiddleName { get; set; }

    public virtual DateTime? Birthday { get; set; }

    [FieldSize(255)]
    public virtual String Email { get; set; }

    [SearchMemberOptions(SearchMemberMode.Exclude)]
    [JsonIgnore]
    [PersistentAlias("Concat(FirstName, ' ', MiddleName, Iif(Len([MiddleName]) > 0, ' ', ''), LastName)")]
    public String FullName {
        get => EvaluateAlias<String>();
    }

    [ImageEditor]
    public virtual Byte[] Photo { get; set; }

    [Aggregated]
    public virtual IList<PhoneNumber> PhoneNumbers { get; set; } = new ObservableCollection<PhoneNumber>();

    [Aggregated, ExpandObjectMembers(ExpandObjectMembers.Never)]
    public virtual Address Address1 { get; set; }

    [Aggregated, ExpandObjectMembers(ExpandObjectMembers.Never)]
    public virtual Address Address2 { get; set; }

    [RuleRegularExpression(@"(((http|https)\://)[a-zA-Z0-9\-\.]+\.[a-zA-Z]{2,3}(:[a-zA-Z0-9]*)?/?([a-zA-Z0-9\-\._\?\,\'/\\\+&amp;amp;%\$#\=~])*)|([a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,6})", CustomMessageTemplate = @"Invalid ""Web Page Address"".")]
    public virtual string WebPageAddress { get; set; }

    public virtual string NickName { get; set; }

    public virtual string SpouseName { get; set; }

    public virtual DateTime? Anniversary { get; set; }

    [StringLength(4096)]
    public virtual string Notes { get; set; }

    private Department department;

    [ImmediatePostData]
    public virtual Department Department {
        get { return department; }
        set {
            if(department != value) {
                department = value;
                Position = null;
                if(Manager != null && Manager.Department != value) {
                    Manager = null;
                }
            }
        }
    }

    public virtual Position Position { get; set; }

    [Aggregated]
    public virtual IList<Resume> Resumes { get; set; } = new ObservableCollection<Resume>();

    [Aggregated]
    public virtual IList<Paycheck> Paychecks { get; set; } = new ObservableCollection<Paycheck>();

    [DataSourceProperty("Department.Employees", DataSourcePropertyIsNullMode.SelectAll), DataSourceCriteria("Position.Title = 'Manager' AND ID != '@This.ID'")]
    public virtual Employee Manager { get; set; }

    public virtual IList<DemoTask> Tasks { get; set; } = new ObservableCollection<DemoTask>();

    public virtual TitleOfCourtesy TitleOfCourtesy { get; set; }

    [ExpandObjectMembers(ExpandObjectMembers.Never), VisibleInListView(false)]
    public virtual Location Location { get; set; }    

    [CollectionOperationSet(AllowAdd = false, AllowRemove = false)]
    [NotMapped]
    [JsonIgnore]
    public virtual IList<AuditDataItemPersistent> ChangeHistory {
        get {
            IObjectSpace objectSpace = ((IObjectSpaceLink)this).ObjectSpace;
            return AuditDataItemPersistent.GetAuditTrail(objectSpace, this);
        }
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TitleOfCourtesy {
    Dr,
    Miss,
    Mr,
    Mrs,
    Ms
};

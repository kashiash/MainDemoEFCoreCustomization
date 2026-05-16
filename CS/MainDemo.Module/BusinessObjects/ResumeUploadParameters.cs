using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using MainDemo.Module.Editors;

namespace MainDemo.Module.BusinessObjects;

[DomainComponent]
public class ResumeUploadParameters {
    [EditorAlias(EditorAliases.ResumeUploadAreaPropertyEditor)]
    public DocumentUploadArea UploadArea { get; set; } = new();

    [VisibleInDetailView(false)]
    [VisibleInListView(false)]
    [VisibleInLookupListView(false)]
    public Guid EmployeeId { get; set; }
}

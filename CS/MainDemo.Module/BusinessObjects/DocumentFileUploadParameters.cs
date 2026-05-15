using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using MainDemo.Module.Editors;

namespace MainDemo.Module.BusinessObjects;

[DomainComponent]
public class DocumentFileUploadParameters {
    [ImmediatePostData]
    public DocumentFileType Type { get; set; }

    [ImmediatePostData]
    public string Description { get; set; }

    [EditorAlias(EditorAliases.DocumentUploadAreaPropertyEditor)]
    public DocumentUploadArea UploadArea { get; set; } = new();

    [VisibleInDetailView(false)]
    [VisibleInListView(false)]
    [VisibleInLookupListView(false)]
    public string OwnerObjectType { get; set; }

    [VisibleInDetailView(false)]
    [VisibleInListView(false)]
    [VisibleInLookupListView(false)]
    public Guid OwnerObjectId { get; set; }
}

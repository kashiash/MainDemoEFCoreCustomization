using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using EditorAliases = MainDemo.Module.Editors.EditorAliases;

namespace MainDemo.Module.BusinessObjects;

[ImageName("BO_FileAttachment")]
[XafDefaultProperty(nameof(DisplayName))]
public class DocumentFile : BaseObject {
    [RuleRequiredField]
    [EditorAlias(DevExpress.ExpressApp.Editors.EditorAliases.FileDataPropertyEditor)]
    public virtual FileData File { get; set; }

    public virtual DocumentFileType Type { get; set; }

    [MaxLength(500)]
    public virtual string Description { get; set; }

    public virtual DateTime UploadedAtUtc { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual DemoTask DemoTask { get; set; }

    [NotMapped]
    [EditorAlias(EditorAliases.DocumentPreviewPropertyEditor)]
    public virtual DocumentFilePreview PreviewFile => new(File);

    [NotMapped]
    public virtual string DisplayName => string.IsNullOrWhiteSpace(File?.FileName)
        ? Type?.Name ?? "Document"
        : File.FileName;

    public override void OnCreated() {
        base.OnCreated();
        UploadedAtUtc = DateTime.UtcNow;
    }
}

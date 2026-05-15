using DevExpress.Persistent.Base;

namespace MainDemo.Module.BusinessObjects;

public sealed class DocumentFilePreview {
    public DocumentFilePreview(IFileData fileData) {
        FileData = fileData;
    }

    public IFileData FileData { get; }
}

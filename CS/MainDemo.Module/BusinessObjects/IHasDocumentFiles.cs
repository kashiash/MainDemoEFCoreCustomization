namespace MainDemo.Module.BusinessObjects;

public interface IHasDocumentFiles {
    IList<DocumentFile> DocumentFiles { get; }
}

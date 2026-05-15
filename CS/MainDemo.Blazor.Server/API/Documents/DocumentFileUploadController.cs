using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;
using MainDemo.Module.BusinessObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MainDemo.Blazor.Server.API.Documents;

[ApiController]
[Authorize]
[Route("api/document-files")]
public class DocumentFileUploadController : ControllerBase {
    private readonly INonSecuredObjectSpaceFactory objectSpaceFactory;

    public DocumentFileUploadController(INonSecuredObjectSpaceFactory objectSpaceFactory) {
        this.objectSpaceFactory = objectSpaceFactory;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(100_000_000)]
    public async Task<IActionResult> Upload(
        [FromForm] List<IFormFile> files,
        [FromForm] string ownerObjectType,
        [FromForm] Guid ownerObjectId,
        [FromForm] Guid? typeId,
        [FromForm] string description) {

        files ??= Request.Form.Files.ToList();
        if (files.Count == 0) {
            return BadRequest("Nie przesłano żadnego pliku.");
        }

        using IObjectSpace objectSpace = objectSpaceFactory.CreateNonSecuredObjectSpace(typeof(DocumentFile));
        DocumentFileType documentType = ResolveDocumentType(objectSpace, typeId);
        List<object> result = new();

        foreach (var formFile in files.Where(item => item.Length > 0)) {
            ValidateExtension(formFile.FileName);

            var documentFile = objectSpace.CreateObject<DocumentFile>();
            var fileData = objectSpace.CreateObject<FileData>();

            await using var stream = formFile.OpenReadStream();
            fileData.LoadFromStream(formFile.FileName, stream);

            documentFile.File = fileData;
            documentFile.Type = documentType;
            documentFile.Description = description;
            AttachToOwner(objectSpace, documentFile, ownerObjectType, ownerObjectId);

            result.Add(new {
                documentFile.ID,
                formFile.FileName
            });
        }

        objectSpace.CommitChanges();
        return Ok(result);
    }

    private static DocumentFileType ResolveDocumentType(IObjectSpace objectSpace, Guid? typeId) {
        if (typeId.HasValue) {
            return objectSpace.GetObjectByKey<DocumentFileType>(typeId.Value);
        }

        return objectSpace.FirstOrDefault<DocumentFileType>(item => item.Code == "OTHER");
    }

    private static void AttachToOwner(IObjectSpace objectSpace, DocumentFile documentFile, string ownerObjectType, Guid ownerObjectId) {
        switch (ownerObjectType) {
            case nameof(Employee):
                documentFile.Employee = objectSpace.GetObjectByKey<Employee>(ownerObjectId);
                break;
            case nameof(DemoTask):
                documentFile.DemoTask = objectSpace.GetObjectByKey<DemoTask>(ownerObjectId);
                break;
            default:
                throw new InvalidOperationException($"Nieobsługiwany typ właściciela: {ownerObjectType}");
        }
    }

    private static void ValidateExtension(string fileName) {
        string extension = Path.GetExtension(fileName).ToLowerInvariant();
        string[] allowedExtensions = [".pdf", ".jpg", ".jpeg", ".png", ".gif", ".docx", ".xlsx"];
        if (!allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase)) {
            throw new InvalidOperationException($"Rozszerzenie {extension} nie jest dozwolone.");
        }
    }
}

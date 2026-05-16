using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;
using MainDemo.Module.BusinessObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MainDemo.Blazor.Server.API.Documents;

[ApiController]
[Authorize]
[Route("api/resumes")]
public class ResumeUploadController : ControllerBase {
    private readonly INonSecuredObjectSpaceFactory objectSpaceFactory;

    public ResumeUploadController(INonSecuredObjectSpaceFactory objectSpaceFactory) {
        this.objectSpaceFactory = objectSpaceFactory;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(100_000_000)]
    public async Task<IActionResult> Upload([FromForm] List<IFormFile> files, [FromForm] Guid employeeId) {
        files ??= Request.Form.Files.ToList();
        if (files.Count == 0) {
            return BadRequest("Nie przesłano żadnego pliku.");
        }

        using IObjectSpace objectSpace = objectSpaceFactory.CreateNonSecuredObjectSpace(typeof(Resume));
        var employee = objectSpace.GetObjectByKey<Employee>(employeeId)
            ?? throw new InvalidOperationException($"Nie znaleziono pracownika o kluczu {employeeId}.");

        List<object> result = new();
        foreach (var formFile in files.Where(item => item.Length > 0)) {
            ValidateExtension(formFile.FileName);

            var resume = objectSpace.CreateObject<Resume>();
            var fileData = objectSpace.CreateObject<FileData>();

            await using var stream = formFile.OpenReadStream();
            fileData.LoadFromStream(formFile.FileName, stream);

            resume.Employee = employee;
            resume.File = fileData;

            result.Add(new {
                resume.ID,
                formFile.FileName
            });
        }

        objectSpace.CommitChanges();
        return Ok(result);
    }

    private static void ValidateExtension(string fileName) {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase)) {
            throw new InvalidOperationException($"Rozszerzenie {extension} nie jest dozwolone dla CV.");
        }
    }
}

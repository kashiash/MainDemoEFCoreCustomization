using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using MainDemo.Module.BusinessObjects;
using EditorAliases = MainDemo.Module.Editors.EditorAliases;
using Microsoft.AspNetCore.Components;

namespace MainDemo.Blazor.Server.Editors.Documents;

[PropertyEditor(typeof(DocumentUploadArea), EditorAliases.ResumeUploadAreaPropertyEditor, false)]
public class ResumeUploadAreaPropertyEditor(Type objectType, IModelMemberViewItem model)
    : BlazorPropertyEditorBase(objectType, model) {
    protected override RenderFragment CreateViewComponentCore(object dataContext) {
        var parameters = dataContext as ResumeUploadParameters;
        return builder => {
            builder.OpenComponent<ResumeUploadAreaRenderer>(0);
            builder.AddAttribute(1, nameof(ResumeUploadAreaRenderer.EmployeeId), parameters?.EmployeeId ?? Guid.Empty);
            builder.CloseComponent();
        };
    }

    protected override IComponentModel CreateComponentModel() => new StaticTextComponentModel();
}

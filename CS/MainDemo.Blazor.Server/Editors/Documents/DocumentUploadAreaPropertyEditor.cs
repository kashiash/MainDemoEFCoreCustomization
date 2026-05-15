using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using MainDemo.Module.BusinessObjects;
using EditorAliases = MainDemo.Module.Editors.EditorAliases;
using Microsoft.AspNetCore.Components;

namespace MainDemo.Blazor.Server.Editors.Documents;

[PropertyEditor(typeof(DocumentUploadArea), EditorAliases.DocumentUploadAreaPropertyEditor, false)]
public class DocumentUploadAreaPropertyEditor(Type objectType, IModelMemberViewItem model)
    : BlazorPropertyEditorBase(objectType, model) {
    protected override RenderFragment CreateViewComponentCore(object dataContext) {
        var parameters = dataContext as DocumentFileUploadParameters;
        return builder => {
            builder.OpenComponent<DocumentUploadAreaRenderer>(0);
            builder.AddAttribute(1, nameof(DocumentUploadAreaRenderer.OwnerObjectType), parameters?.OwnerObjectType ?? string.Empty);
            builder.AddAttribute(2, nameof(DocumentUploadAreaRenderer.OwnerObjectId), parameters?.OwnerObjectId ?? Guid.Empty);
            builder.AddAttribute(3, nameof(DocumentUploadAreaRenderer.TypeId), parameters?.Type?.ID);
            builder.AddAttribute(4, nameof(DocumentUploadAreaRenderer.Description), parameters?.Description ?? string.Empty);
            builder.CloseComponent();
        };
    }

    protected override IComponentModel CreateComponentModel() => new StaticTextComponentModel();
}

using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using MainDemo.Module.BusinessObjects;
using EditorAliases = MainDemo.Module.Editors.EditorAliases;
using Microsoft.AspNetCore.Components;

namespace MainDemo.Blazor.Server.Editors.Documents;

[PropertyEditor(typeof(DocumentFilePreview), EditorAliases.DocumentPreviewPropertyEditor, false)]
public class DocumentPreviewPropertyEditor(Type objectType, IModelMemberViewItem model)
    : BlazorPropertyEditorBase(objectType, model) {
    protected override RenderFragment CreateViewComponentCore(object dataContext) {
        var documentFile = dataContext as DocumentFile;
        return builder => {
            builder.OpenComponent<DocumentPreviewRenderer>(0);
            builder.AddAttribute(1, nameof(DocumentPreviewRenderer.FileData), documentFile?.File);
            builder.CloseComponent();
        };
    }

    protected override IComponentModel CreateComponentModel() => new StaticTextComponentModel();
}

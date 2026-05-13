using DevExpress.Blazor;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Blazor.Editors.Adapters;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using EditorAliases = MainDemo.Module.Editors.EditorAliases;

namespace MainDemo.Blazor.Server.Editors.Date;

[PropertyEditor(typeof(DateTime), EditorAliases.MainDemoDateTimeEditor, true)]
public class MainDemoDateTimeEditor(Type objectType, IModelMemberViewItem model)
    : DateTimePropertyEditor(objectType, model) {
    protected override void OnControlCreated() {
        base.OnControlCreated();
        if (Control is DxDateEditModel<DateTime> adapter) {
            ConfigureMaskCaretMode();
            MainDemoDateTimeEditorConfigurator.Configure(adapter, Model);
        }
    }

    void ConfigureMaskCaretMode() {
        // Uwaga: DxDateEditMaskProperties.*.CaretMode to GLOBALNY statyczny stan DevExpress.
        // Dziś źródłem jest jeden IModelOptions.DateEditMaskCaretMode, więc nieszkodliwe.
        // Jeśli kiedyś pojawi się tryb per-View, ostatnio otwarty widok wygra dla wszystkich.
        MaskCaretMode caretMode = MainDemoDateTimeEditorConfigurator.GetMaskCaretMode(Model);
        DxDateEditMaskProperties.DateTime.CaretMode = caretMode;
        DxDateEditMaskProperties.DateOnly.CaretMode = caretMode;
        DxDateEditMaskProperties.DateTimeOffset.CaretMode = caretMode;
    }
}

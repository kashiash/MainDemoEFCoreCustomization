using System.ComponentModel;
using DevExpress.Blazor;
using DevExpress.ExpressApp.Blazor.Components;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Blazor.Editors.Adapters;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace MainDemo.Blazor.Server.Editors;

public static class CustomEditorAliases {
    public const string DateEditor = "DateEditor";
    public const string DateEditorNullable = "DateEditorNullable";
    public const string MouseWheelBlockerCssClass = "maindemo-wheel-blocked";
}

public interface IModelMemberViewItemMouseWheel : IModelMemberViewItem {
    [Category("Behavior")]
    [Description("When true, scrolling the mouse wheel inside this date editor will not change the value.")]
    [DefaultValue(true)]
    bool BlockMouseWheel { get; set; }
}

[PropertyEditor(typeof(DateTime), CustomEditorAliases.DateEditor, false)]
public class DateEditor(Type objectType, IModelMemberViewItem model) : DateTimePropertyEditor(objectType, model) {
    protected override void OnControlCreated() {
        base.OnControlCreated();
        if (Control is DxDateEditModel<DateTime> adapter) {
            DxDateEditMaskProperties.DateTime.CaretMode = MaskCaretMode.Advancing;
            DxDateEditMaskProperties.DateOnly.CaretMode = MaskCaretMode.Advancing;
            DxDateEditMaskProperties.DateTimeOffset.CaretMode = MaskCaretMode.Advancing;

            adapter.Format = "dd.MM.yyyy";
            adapter.DisplayFormat = "dd.MM.yyyy";
            adapter.Mask = "dd.MM.yyyy";
            ApplyMouseWheelBlocker(adapter);
            adapter.Buttons = CreateButton();
        }
    }

    void ApplyMouseWheelBlocker<T>(DxDateEditModel<T> adapter) {
        if (Model is IModelMemberViewItemMouseWheel m && !m.BlockMouseWheel) {
            return;
        }
        adapter.CssClass = string.IsNullOrEmpty(adapter.CssClass)
            ? CustomEditorAliases.MouseWheelBlockerCssClass
            : adapter.CssClass + " " + CustomEditorAliases.MouseWheelBlockerCssClass;
    }

    protected override RenderFragment CreateViewComponentCore(object dataContext) {
        var displayTextModel = new DisplayTextModel();
        var propertyValue = this.GetPropertyValue(dataContext);
        if (propertyValue is null) displayTextModel.DisplayText = NullText;
        else displayTextModel.DisplayText = ((DateTime)propertyValue).ToString("dd.MM.yyyy");
        return DisplayTextRenderer.Create(displayTextModel);
    }

    private RenderFragment CreateButton() {
        return builder => {
            builder.OpenComponent<DxEditorButton>(0);
            builder.AddAttribute(1, "IconCssClass", "dx-icon-clock");
            builder.AddAttribute(2, "Position", EditorButtonPosition.Right);
            builder.AddAttribute(3, "Click", EventCallback.Factory.Create<MouseEventArgs>(this, () => PropertyValue = DateTime.Now));
            builder.CloseComponent();
        };
    }
}

[PropertyEditor(typeof(DateTime?), CustomEditorAliases.DateEditorNullable, false)]
public class DateEditorNullable(Type objectType, IModelMemberViewItem model) : DateTimePropertyEditor(objectType, model) {
    protected override void OnControlCreated() {
        base.OnControlCreated();
        if (Control is DxDateEditModel<DateTime?> adapter) {
            DxDateEditMaskProperties.DateTime.CaretMode = MaskCaretMode.Advancing;
            DxDateEditMaskProperties.DateOnly.CaretMode = MaskCaretMode.Advancing;
            DxDateEditMaskProperties.DateTimeOffset.CaretMode = MaskCaretMode.Advancing;

            adapter.Format = "dd.MM.yyyy";
            adapter.DisplayFormat = "dd.MM.yyyy";
            adapter.Mask = "dd.MM.yyyy";
            ApplyMouseWheelBlocker(adapter);
            adapter.Buttons = CreateButton();
        }
    }

    void ApplyMouseWheelBlocker<T>(DxDateEditModel<T> adapter) {
        if (Model is IModelMemberViewItemMouseWheel m && !m.BlockMouseWheel) {
            return;
        }
        adapter.CssClass = string.IsNullOrEmpty(adapter.CssClass)
            ? CustomEditorAliases.MouseWheelBlockerCssClass
            : adapter.CssClass + " " + CustomEditorAliases.MouseWheelBlockerCssClass;
    }

    protected override RenderFragment CreateViewComponentCore(object dataContext) {
        var displayTextModel = new DisplayTextModel();
        var propertyValue = this.GetPropertyValue(dataContext);
        if (propertyValue is null) displayTextModel.DisplayText = NullText;
        else displayTextModel.DisplayText = ((DateTime?)propertyValue)?.ToString("dd.MM.yyyy");
        return DisplayTextRenderer.Create(displayTextModel);
    }

    private RenderFragment CreateButton() {
        return builder => {
            builder.OpenComponent<DxEditorButton>(0);
            builder.AddAttribute(1, "IconCssClass", "dx-icon-clock");
            builder.AddAttribute(2, "Position", EditorButtonPosition.Right);
            builder.AddAttribute(3, "Click", EventCallback.Factory.Create<MouseEventArgs>(this, () => PropertyValue = DateTime.Now));
            builder.CloseComponent();
        };
    }
}

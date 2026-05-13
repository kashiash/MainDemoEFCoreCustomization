using DevExpress.Blazor;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Model;
using MainDemo.Module.Editors;

namespace MainDemo.Blazor.Server.Editors.Date;

internal static class MainDemoDateTimeEditorConfigurator {
    static readonly HashSet<char> TimeFormatTokens = new() { 'H', 'h', 's', 't', 'f', 'F', 'K', 'z' };
    static readonly HashSet<string> DateTimeStandardFormats = new(StringComparer.Ordinal) {
        "f", "F", "g", "G", "o", "O", "r", "R", "s", "t", "T", "u", "U"
    };

    public static MaskCaretMode GetMaskCaretMode(IModelMemberViewItem model) {
        if (model?.Application?.Options is IModelOptionsDateEditMouseWheel options) {
            return options.DateEditMaskCaretMode;
        }
        return MaskCaretMode.Advancing;
    }

    public static void Configure<T>(DxDateEditModel<T> adapter, IModelMemberViewItem model) {
        string editMask = NormalizeModelFormat(model?.EditMask);
        string displayFormat = NormalizeModelFormat(model?.DisplayFormat);

        if (!string.IsNullOrWhiteSpace(displayFormat)) {
            adapter.Format = displayFormat;
            adapter.DisplayFormat = displayFormat;
        }

        if (!string.IsNullOrWhiteSpace(editMask)) {
            adapter.Mask = editMask;
        }

        string effectiveFormat = editMask ?? displayFormat;
        bool hasTime = IncludesTimeSection(effectiveFormat);
        adapter.TimeSectionVisible = hasTime;
        if (hasTime) {
            adapter.TimeSectionScrollPickerFormat = "H m";
        }

        ApplyMouseWheelBehavior(adapter, model);
    }

    static void ApplyMouseWheelBehavior<T>(DxDateEditModel<T> adapter, IModelMemberViewItem model) {
        bool shouldBlock = ShouldBlockMouseWheel(model);
        AppendCssClass(adapter, shouldBlock
            ? DateEditorCssAliases.MouseWheelBlocked
            : DateEditorCssAliases.MouseWheelAllowed);
    }

    static bool ShouldBlockMouseWheel(IModelMemberViewItem model) {
        if (model == null) return true;

        var attribute = model.ModelMember?.MemberInfo?.FindAttribute<DateEditMouseWheelAttribute>();
        if (attribute != null) {
            return attribute.BlockMouseWheel;
        }

        if (model is IModelMemberViewItemMouseWheel { BlockMouseWheel: bool viewItemValue }) {
            return viewItemValue;
        }

        if (model.Application?.Options is IModelOptionsDateEditMouseWheel options) {
            return options.BlockDateEditMouseWheelByDefault;
        }

        return true;
    }

    static void AppendCssClass<T>(DxDateEditModel<T> adapter, string cssClass) {
        adapter.CssClass = string.IsNullOrWhiteSpace(adapter.CssClass)
            ? cssClass
            : adapter.CssClass + " " + cssClass;
        adapter.InputCssClass = string.IsNullOrWhiteSpace(adapter.InputCssClass)
            ? cssClass
            : adapter.InputCssClass + " " + cssClass;
    }

    static string NormalizeModelFormat(string format) {
        if (string.IsNullOrWhiteSpace(format)) return null;
        string normalized = format.Trim();
        if (normalized.StartsWith("{0:", StringComparison.Ordinal) && normalized.EndsWith("}")) {
            normalized = normalized.Substring(3, normalized.Length - 4);
        }
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    static bool IncludesTimeSection(string format) {
        if (string.IsNullOrWhiteSpace(format)) return false;
        string normalized = NormalizeModelFormat(format) ?? string.Empty;
        if (DateTimeStandardFormats.Contains(normalized)) return true;

        string maskWithoutLiterals = RemoveQuotedAndEscapedLiterals(normalized);
        for (int i = 0; i < maskWithoutLiterals.Length; i++) {
            char token = maskWithoutLiterals[i];
            if (TimeFormatTokens.Contains(token)) return true;
            if (token == 'm' && normalized.Length > 1) return true;
        }
        return false;
    }

    static string RemoveQuotedAndEscapedLiterals(string value) {
        var result = new System.Text.StringBuilder(value.Length);
        bool inSingle = false, inDouble = false;
        for (int i = 0; i < value.Length; i++) {
            char current = value[i];
            if (current == '\\') { i++; continue; }
            if (current == '\'' && !inDouble) { inSingle = !inSingle; continue; }
            if (current == '"' && !inSingle) { inDouble = !inDouble; continue; }
            if (!inSingle && !inDouble) result.Append(current);
        }
        return result.ToString();
    }
}

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.Editors;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using MainDemo.Module.Storages;

namespace MainDemo.Module.BusinessObjects;

[DefaultClassOptions]
[DefaultProperty(nameof(Name))]
[ImageName("BO_Condition")]
public class DynamicAppearanceRule : BaseObject, IAppearanceRuleProperties {
    private const string DefaultCriteria = "True";
    private const string DefaultTargetItems = "*";
    private const string DefaultContext = "Any";
    private const string DefaultAppearanceItemType = "ViewItem";

    [StringLength(256)]
    public virtual string Name { get; set; }

    [Browsable(false)]
    [StringLength(512)]
    public virtual string ObjectTypeFullName { get; set; }

    [Browsable(false)]
    [StringLength(256)]
    public virtual string ObjectTypeName { get; set; }

    [NotMapped]
    [ImmediatePostData]
    public virtual Type DataType {
        get => string.IsNullOrWhiteSpace(ObjectTypeFullName) ? null : Type.GetType(ObjectTypeFullName);
        set {
            ObjectTypeFullName = value?.AssemblyQualifiedName;
            ObjectTypeName = value?.Name;
        }
    }

    [Column(TypeName = "nvarchar(max)")]
    public virtual string Criteria {
        get;
        set;
    } = DefaultCriteria;

    [StringLength(512)]
    public virtual string TargetItems {
        get;
        set;
    } = DefaultTargetItems;

    [StringLength(128)]
    public virtual string Context {
        get;
        set;
    } = DefaultContext;

    [StringLength(128)]
    public virtual string AppearanceItemType {
        get;
        set;
    } = DefaultAppearanceItemType;

    [StringLength(256)]
    public virtual string ViewId { get; set; }

    public virtual int Priority { get; set; }

    public virtual ViewItemVisibility? Visibility { get; set; }

    public virtual bool? Enabled { get; set; }

    [StringLength(64)]
    [Browsable(false)]
    public virtual string FontColorCss { get; set; }

    [StringLength(64)]
    [Browsable(false)]
    public virtual string BackColorCss { get; set; }

    [StringLength(128)]
    public virtual string CssClass { get; set; }

    [StringLength(128)]
    public virtual string Method { get; set; }

    public virtual DevExpress.Drawing.DXFontStyle? FontStyle { get; set; }

    [NotMapped]
    [Browsable(false)]
    public Type DeclaringType => DataType;

    [NotMapped]
    public Color? FontColor {
        get => ParseColor(FontColorCss);
        set => FontColorCss = ToCssColor(value);
    }

    [NotMapped]
    public Color? BackColor {
        get => ParseColor(BackColorCss);
        set => BackColorCss = ToCssColor(value);
    }

    public override void OnSaving() {
        base.OnSaving();
        var objectSpace = ((IObjectSpaceLink)this).ObjectSpace;
        if(objectSpace != null && objectSpace.IsDeletedObject(this)) {
            DynamicAppearanceRuleStorage.Remove(this);
        }
        else {
            DynamicAppearanceRuleStorage.Put(this);
        }
    }

    public bool Matches(Type objectType, string viewId) {
        if(objectType == null) {
            return false;
        }
        var currentTypeName = NormalizeTypeName(objectType.Name);
        if(!string.Equals(ObjectTypeName, currentTypeName, StringComparison.Ordinal)) {
            return false;
        }
        return string.IsNullOrWhiteSpace(ViewId) || string.Equals(ViewId, viewId, StringComparison.Ordinal);
    }

    internal static string NormalizeTypeName(string typeName) {
        const string proxySuffix = "Proxy";
        if(string.IsNullOrWhiteSpace(typeName)) {
            return typeName;
        }
        return typeName.EndsWith(proxySuffix, StringComparison.Ordinal)
            ? typeName[..^proxySuffix.Length]
            : typeName;
    }

    private static Color? ParseColor(string cssColor) {
        if(string.IsNullOrWhiteSpace(cssColor)) {
            return null;
        }
        try {
            return ColorTranslator.FromHtml(cssColor);
        }
        catch {
            return null;
        }
    }

    private static string ToCssColor(Color? color) {
        if(color == null) {
            return null;
        }
        return ColorTranslator.ToHtml(color.Value);
    }
}

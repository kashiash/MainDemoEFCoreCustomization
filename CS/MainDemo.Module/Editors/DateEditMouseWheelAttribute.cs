namespace MainDemo.Module.Editors;

[AttributeUsage(AttributeTargets.Property)]
public sealed class DateEditMouseWheelAttribute(bool blockMouseWheel) : Attribute {
    public bool BlockMouseWheel { get; } = blockMouseWheel;
}

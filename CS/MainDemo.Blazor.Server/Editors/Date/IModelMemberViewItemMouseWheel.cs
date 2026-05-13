using System.ComponentModel;
using DevExpress.ExpressApp.Model;

namespace MainDemo.Blazor.Server.Editors.Date;

public interface IModelMemberViewItemMouseWheel : IModelMemberViewItem {
    [Category("Behavior")]
    [Description("Opcjonalne ustawienie dla konkretnego pola. Null oznacza: użyj wartości z Options.BlockDateEditMouseWheelByDefault.")]
    bool? BlockMouseWheel { get; set; }
}

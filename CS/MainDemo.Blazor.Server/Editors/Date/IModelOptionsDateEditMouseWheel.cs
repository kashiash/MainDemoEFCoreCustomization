using System.ComponentModel;
using DevExpress.Blazor;

namespace MainDemo.Blazor.Server.Editors.Date;

public interface IModelOptionsDateEditMouseWheel {
    [Category("Behavior")]
    [Description("Globalne ustawienie domyślne. Gdy True, przewijanie kółkiem myszy wewnątrz edytorów daty nie zmienia wartości pola.")]
    [DefaultValue(true)]
    bool BlockDateEditMouseWheelByDefault { get; set; }

    [Category("Behavior")]
    [Description("Globalny tryb przesuwania kursora w maskach edytorów daty. Advancing oznacza, że kursor sam przeskakuje do następnej sekcji po wpisaniu maksymalnej liczby znaków.")]
    [DefaultValue(MaskCaretMode.Advancing)]
    MaskCaretMode DateEditMaskCaretMode { get; set; }
}

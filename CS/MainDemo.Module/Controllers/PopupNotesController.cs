using System.Text.RegularExpressions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using MainDemo.Module.BusinessObjects;

namespace MainDemo.Module.Controllers;

public class PopupNotesController : ObjectViewController<DetailView, DemoTask> {
    private PopupWindowShowAction showNotesAction;
    public PopupNotesController() : base() {
        showNotesAction = new PopupWindowShowAction(this, "ShowNotesAction", DevExpress.Persistent.Base.PredefinedCategory.Edit) {
            Caption = "Show Notes",
            ImageName = "BO_Note"
        };
        showNotesAction.Execute += ShowNotesAction_Execute;
        showNotesAction.CustomizePopupWindowParams += ShowNotesAction_CustomizePopupWindowParams;
    }
    void ShowNotesAction_Execute(object sender, PopupWindowShowActionExecuteEventArgs args) {
        var task = (DemoTask)View.CurrentObject;
        foreach(Note note in args.PopupWindowViewSelectedObjects) {
            if(!string.IsNullOrEmpty(task.Description)) {
                task.Description += Environment.NewLine;
            }
            task.Description += StripHTML(note.Text);
        }
        if(View.ViewEditMode == ViewEditMode.View) {
            View.ObjectSpace.CommitChanges();
        }
    }
    void ShowNotesAction_CustomizePopupWindowParams(object sender, CustomizePopupWindowParamsEventArgs args) {
        IObjectSpace objectSpace = Application.CreateObjectSpace(typeof(Note));
        string noteListViewId = Application.FindLookupListViewId(typeof(Note));
        CollectionSourceBase collectionSource = Application.CreateCollectionSource(objectSpace, typeof(Note), noteListViewId);
        args.View = Application.CreateListView(noteListViewId, collectionSource, true);
    }
    string StripHTML(string HTMLText) {
        if(!String.IsNullOrEmpty(HTMLText)) {
            return Regex.Replace(HTMLText, "<[^>]+>", string.Empty).Replace("&nbsp;", "").Replace("&nbsp", "").Replace(System.Environment.NewLine, "").Replace("\t", "");
        }
        else {
            return String.Empty;
        }

    }
}

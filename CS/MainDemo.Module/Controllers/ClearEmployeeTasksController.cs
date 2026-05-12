using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using DevExpress.Persistent.Base;
using MainDemo.Module.BusinessObjects;

namespace MainDemo.Module.Controllers;

public class ClearEmployeeTasksController : ObjectViewController<DetailView, Employee> {
    private SimpleAction clearTasksAction;
    public ClearEmployeeTasksController() {
        clearTasksAction = new SimpleAction(this, "ClearTasksAction", PredefinedCategory.RecordEdit) {
            Caption = "Clear Tasks",
            ConfirmationMessage = "Are you sure you want to clear the Tasks list?",
            ImageName = "Action_Clear"
        };
        clearTasksAction.Execute += ClearTasksAction_Execute;

        TargetViewNesting = Nesting.Root;
    }

    void ClearTasksAction_Execute(Object sender, SimpleActionExecuteEventArgs e) {
        while(((Employee)View.CurrentObject).Tasks.Count > 0) {
            ((Employee)View.CurrentObject).Tasks.Remove(((Employee)View.CurrentObject).Tasks[0]);
        }
        ObjectSpace.SetModified(View.CurrentObject);
    }

    void ClearTasksController_Activated(object sender, EventArgs e) {
        clearTasksAction.Enabled.SetItemValue("EditMode", View.ViewEditMode == ViewEditMode.Edit);
        View.ViewEditModeChanged += new EventHandler<EventArgs>(ClearTasksController_ViewEditModeChanged);
    }
    void ClearTasksController_ViewEditModeChanged(object sender, EventArgs e) {
        clearTasksAction.Enabled.SetItemValue("EditMode", View.ViewEditMode == ViewEditMode.Edit);
    }
}

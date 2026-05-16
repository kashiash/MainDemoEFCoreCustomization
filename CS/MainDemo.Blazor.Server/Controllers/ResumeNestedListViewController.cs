using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using MainDemo.Module.BusinessObjects;

namespace MainDemo.Blazor.Server.Controllers;

public class ResumeNestedListViewController : ObjectViewController<ListView, Resume> {
    private readonly PopupWindowShowAction addResumesAction;

    public ResumeNestedListViewController() {
        TargetViewNesting = Nesting.Nested;

        addResumesAction = new PopupWindowShowAction(this, "AddEmployeeResumes", PredefinedCategory.RecordEdit) {
            Caption = "Dodaj CV",
            ImageName = "BO_Resume",
            AcceptButtonCaption = "Zamknij"
        };

        addResumesAction.CustomizePopupWindowParams += AddResumesAction_CustomizePopupWindowParams;
        addResumesAction.Execute += AddResumesAction_Execute;
    }

    protected override void OnActivated() {
        base.OnActivated();
        addResumesAction.Active["EmployeeOwner"] = GetOwner() is Employee;
    }

    protected override void OnDeactivated() {
        addResumesAction.CustomizePopupWindowParams -= AddResumesAction_CustomizePopupWindowParams;
        addResumesAction.Execute -= AddResumesAction_Execute;
        base.OnDeactivated();
    }

    private void AddResumesAction_CustomizePopupWindowParams(object sender, CustomizePopupWindowParamsEventArgs e) {
        if (GetOwner() is not Employee employee) {
            throw new UserFriendlyException("Brak pracownika dla listy CV.");
        }

        var popupObjectSpace = Application.CreateObjectSpace(typeof(ResumeUploadParameters));
        var parameters = popupObjectSpace.CreateObject<ResumeUploadParameters>();
        parameters.EmployeeId = employee.ID;

        e.View = Application.CreateDetailView(popupObjectSpace, "ResumeUploadParameters_DetailView", true, parameters);
        e.DialogController.SaveOnAccept = false;
        e.Maximized = true;
    }

    private void AddResumesAction_Execute(object sender, PopupWindowShowActionExecuteEventArgs e) {
        View.ObjectSpace.Refresh();
        View.Refresh();
    }

    private object GetOwner() {
        if (View?.CollectionSource is PropertyCollectionSource propertyCollectionSource) {
            return propertyCollectionSource.MasterObject;
        }

        return null;
    }
}

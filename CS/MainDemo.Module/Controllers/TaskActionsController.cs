using System.Collections;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Utils;
using MainDemo.Module.BusinessObjects;

namespace MainDemo.Module.Controllers;

public class TaskActionsController : ObjectViewController<ObjectView, DemoTask> {
    private SingleChoiceAction setTaskAction;
    private ChoiceActionItem setPriorityItem;
    private ChoiceActionItem setStatusItem;

    public TaskActionsController() {
        setTaskAction = new SingleChoiceAction(this, "SetTaskAction", DevExpress.Persistent.Base.PredefinedCategory.Edit) {
            Caption = "Set Task",
            SelectionDependencyType = SelectionDependencyType.RequireMultipleObjects,
            ImageName = "Task",
            ItemType = SingleChoiceActionItemType.ItemIsOperation
        };
        setTaskAction.Execute += SetTaskAction_Execute;

        setPriorityItem = new ChoiceActionItem(CaptionHelper.GetMemberCaption(typeof(DemoTask), nameof(DemoTask.Priority)), null);
        setTaskAction.Items.Add(setPriorityItem);
        FillItemWithEnumValues(setPriorityItem, typeof(Priority));
        setStatusItem = new ChoiceActionItem(CaptionHelper.GetMemberCaption(typeof(DemoTask), nameof(DemoTask.Status)), null);
        setTaskAction.Items.Add(setStatusItem);
        FillItemWithEnumValues(setStatusItem, typeof(BusinessObjects.TaskStatus));
    }

    void UpdateSetTaskActionState() {
        bool isGranted = true;

        IRequestSecurity security = (IRequestSecurity)Application.Security;
        foreach(object selectedObject in View.SelectedObjects) {
            bool isPriorityGranted = security.IsGranted(new PermissionRequest(ObjectSpace, typeof(DemoTask), SecurityOperations.Write, selectedObject, nameof(DemoTask.Priority)));
            bool isStatusGranted = security.IsGranted(new PermissionRequest(ObjectSpace, typeof(DemoTask), SecurityOperations.Write, selectedObject, nameof(DemoTask.Status)));
            if(!isPriorityGranted || !isStatusGranted) {
                isGranted = false;
            }
        }
        setTaskAction.Enabled.SetItemValue("SecurityAllowance", isGranted);
    }
    void FillItemWithEnumValues(ChoiceActionItem parentItem, Type enumType) {
        EnumDescriptor ed = new EnumDescriptor(enumType);
        foreach(object current in Enum.GetValues(enumType)) {
            ChoiceActionItem item = new ChoiceActionItem(ed.GetCaption(current), current);
            item.ImageName = ImageLoader.Instance.GetEnumValueImageName(current);
            parentItem.Items.Add(item);
        }
    }
    protected override void OnActivated() {
        View.SelectionChanged += new EventHandler(View_SelectionChanged);
        UpdateSetTaskActionState();
    }

    void View_SelectionChanged(object sender, EventArgs e) {
        UpdateSetTaskActionState();
    }
    DemoTask GetObject(DemoTask obj, IObjectSpace objectSpace, IObjectSpace newObjectSpace, ref int newObjectsCount) {
        if(objectSpace.IsNewObject(obj)) {
            newObjectsCount++;
            return obj;
        }
        return (DemoTask)newObjectSpace.GetObject(obj);
    }
    void SetTaskAction_Execute(object sender, SingleChoiceActionExecuteEventArgs args) {
        IObjectSpace objectSpace = View is ListView ? Application.CreateObjectSpace(typeof(DemoTask)) : View.ObjectSpace;
        int newObjectsCount = 0;
        ArrayList objectsToProcess = new ArrayList(args.SelectedObjects);
        if(args.SelectedChoiceActionItem.ParentItem == setPriorityItem) {
            foreach(Object obj in objectsToProcess) {
                DemoTask objInNewObjectSpace = GetObject((DemoTask)obj, View.ObjectSpace, objectSpace, ref newObjectsCount);
                objInNewObjectSpace.Priority = (Priority)args.SelectedChoiceActionItem.Data;
            }
        }
        else if(args.SelectedChoiceActionItem.ParentItem == setStatusItem) {
            foreach(Object obj in objectsToProcess) {
                DemoTask objInNewObjectSpace = GetObject((DemoTask)obj, View.ObjectSpace, objectSpace, ref newObjectsCount);
                objInNewObjectSpace.Status = (BusinessObjects.TaskStatus)args.SelectedChoiceActionItem.Data;
            }
        }
        if(View is DetailView && ((DetailView)View).ViewEditMode == ViewEditMode.View) {
            objectSpace.CommitChanges();
        }
        if((View is ListView) && (newObjectsCount != objectsToProcess.Count)) {
            objectSpace.CommitChanges();
            View.ObjectSpace.Refresh();
        }
    }
}

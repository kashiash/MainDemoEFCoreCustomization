
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.Persistent.BaseImpl.EF;

namespace MainDemo.Win.Controllers;
public class EFDetailViewController : ViewController<DetailView> {

    void ObjectSpace_ObjectChanged(object sender, ObjectChangedEventArgs e) {
        if(
            (e.MemberInfo != null)
            &&
            !e.MemberInfo.IsPersistent
            &&
            (
                (e.MemberInfo.MemberType.IsEnum && e.MemberInfo.Owner.IsPersistent)
                ||
                typeof(Image).IsAssignableFrom(e.MemberInfo.MemberType)
                ||
                typeof(Color).IsAssignableFrom(e.MemberInfo.MemberType)
            )
        ) {
            ObjectSpace.SetModified(null);
        }
    }
    void ObjectSpace_Committed(object sender, EventArgs e) {
        if(View.CurrentObject is Event) {
            LinkToListViewController linkToListViewController = Frame.GetController<LinkToListViewController>();
            if((linkToListViewController != null) && (linkToListViewController.Link != null)
                    && (linkToListViewController.Link.ListView != null) && linkToListViewController.Link.ListView.IsRoot) {
                linkToListViewController.Link.ListView.ObjectSpace.Refresh();
            }
        }
    }

    protected override void OnActivated() {
        base.OnActivated();
        ObjectSpace.ObjectChanged += new EventHandler<ObjectChangedEventArgs>(ObjectSpace_ObjectChanged);
        ObjectSpace.Committed += new EventHandler(ObjectSpace_Committed);
    }
    protected override void OnDeactivated() {
        base.OnDeactivated();
        ObjectSpace.ObjectChanged -= new EventHandler<ObjectChangedEventArgs>(ObjectSpace_ObjectChanged);
        ObjectSpace.Committed -= new EventHandler(ObjectSpace_Committed);
    }
}

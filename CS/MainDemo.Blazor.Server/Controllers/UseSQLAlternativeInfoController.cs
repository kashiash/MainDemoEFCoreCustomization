using Demos.Data;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;

namespace MainDemo.Blazor.Server.Controllers;
public class UseSQLAlternativeInfoController : ObjectViewController<DetailView, UseSQLAlternativeInfo> {
    public UseSQLAlternativeInfoController() : base() { }
    protected override void OnActivated() {
        base.OnActivated();
        View.CustomizeViewItemControl<StaticTextViewItem>(this, SetStaticItem);

        StaticTextViewItem sqlIssueItem = (StaticTextViewItem)View.FindItem("SQLIssueText");
        sqlIssueItem.Text = string.Format("<b><font size=+1>{0}</size></b>", ViewCurrentObject.SQLIssue);

        StaticTextViewItem alternativeItem = (StaticTextViewItem)View.FindItem("AlternativeStaticText");
        alternativeItem.Text = string.Format("<b>{0}</b> will be used instead.", ViewCurrentObject.Alternative);

        StaticTextViewItem noteItem = (StaticTextViewItem)View.FindItem("NoteStaticText");
        noteItem.Text = string.Format("<b>Note:</b> {0}", ViewCurrentObject.Restrictions);
    }
    private void SetStaticItem(StaticTextViewItem staticText) {
        staticText.ComponentModel.UseMarkupString = true;
    }
}

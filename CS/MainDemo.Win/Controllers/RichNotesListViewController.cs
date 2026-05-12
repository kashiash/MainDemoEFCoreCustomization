using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Win.Editors;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraRichEdit;
using MainDemo.Module.BusinessObjects;

namespace MainDemo.Win.Controllers;
public class RichNotesListViewController : ObjectViewController<DevExpress.ExpressApp.ListView, Note> {
    static RichNotesListViewController() {
        RichEditControlCompatibility.DefaultFontName = "Tahoma";
        RichEditControlCompatibility.DefaultFontSize = 8;
    }
    protected override void OnViewControlsCreated() {
        base.OnViewControlsCreated();
        var gridListEditor = View.Editor as GridListEditor;
        if(gridListEditor != null && gridListEditor.GridView != null) {
            foreach(GridColumn column in gridListEditor.GridView.Columns) {
                var editor = column.ColumnEdit as RepositoryItemRichTextEdit;
                if(editor != null) {
                    editor.VerticalIndent = 2;
                }
                else {
                    column.AppearanceCell.Font = new Font("Tahoma", 8);
                }
            }
        }
    }
}

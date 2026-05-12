using System;
using DevExpress.ExpressApp.DC;

namespace Demos.Data {

[DomainComponent]
public sealed class UseSQLAlternativeInfo {
    public string SQLIssue { get; set; }
    public string Alternative { get; set; }
    public string Restrictions { get; set; }
}
public sealed class UseSQLAlternativeInfoSingleton {
    public static UseSQLAlternativeInfoSingleton Instance { get; } = new UseSQLAlternativeInfoSingleton();
    public UseSQLAlternativeInfo Info { get; } = new UseSQLAlternativeInfo();
    public bool UseAlternative { get; private set; }
    public void FillFields(string sqlIssue, string alternativeName, string restrictions) {
        if(!UseAlternative) {
            UseAlternative = true;
            Info.SQLIssue = sqlIssue;
            Info.Alternative = alternativeName;
            Info.Restrictions = restrictions;
        }
        else if(!Info.Alternative.Contains(alternativeName)) {
            AddAlternative(alternativeName, restrictions);
        }
    }
    private void AddAlternative(string alternativeName, string restrictions) {
        Info.Alternative += " and " + alternativeName;
        Info.Restrictions += Environment.NewLine + restrictions;
    }
}
}

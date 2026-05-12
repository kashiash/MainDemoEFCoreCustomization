using System.ComponentModel;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;

namespace MainDemo.Module.BusinessObjects;

[DefaultProperty(nameof(Latitude))]
public class Location : BaseObject, IMapsMarker {
    public virtual Guid EmployeeID { get; set; }

    [Browsable(false)]
    public virtual Employee Employee { get; set; }

    public string Title {
        get { return Employee.FullName; }
    }

    public virtual double Latitude { get; set; }

    public virtual double Longitude { get; set; }

    public override string ToString() {
        string latitudePrefix = Latitude > 0 ? "N" : "S";
        string longitudePrefix = Longitude > 0 ? "E" : "W";
        return string.Format("{0}{1:0.###}, {2}{3:0.###}", latitudePrefix, Math.Abs(Latitude), longitudePrefix, Math.Abs(Longitude));
    }
}

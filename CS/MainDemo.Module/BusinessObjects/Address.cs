using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.ExpressApp.DC;

namespace MainDemo.Module.BusinessObjects;

[DefaultProperty(nameof(FullAddress))]
public class Address : BaseObject {
    public virtual String Street { get; set; }

    public virtual String City { get; set; }

    public virtual String StateProvince { get; set; }

    public virtual String ZipPostal { get; set; }

    public virtual Country Country { get; set; }

    [InverseProperty(nameof(Employee.Address1)), Browsable(false)]
    public virtual IList<Employee> Parties1 { get; set; } = new ObservableCollection<Employee>();

    [InverseProperty(nameof(Employee.Address2)), Browsable(false)]
    public virtual IList<Employee> Parties2 { get; set; } = new ObservableCollection<Employee>();

    [PersistentAlias("Concat(Country.Name, Iif([Country.Name] == '', '', '; '), StateProvince, Iif([StateProvince] == '', '', '; '), City, Iif([City] == '', '', '; '), Street, Iif([Street] == '', '', '; '), ZipPostal)")]
    public String FullAddress {
        get => EvaluateAlias<String>();
    }
}

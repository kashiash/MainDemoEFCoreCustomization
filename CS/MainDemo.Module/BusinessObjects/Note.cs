using System;
using System.ComponentModel;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;

namespace MainDemo.Module.BusinessObjects;

[DefaultProperty(nameof(Text))]
[ImageName("BO_Note")]
public class Note : BaseObject {
    public virtual String Author { get; set; }

    public virtual DateTime? DateTime { get; set; }

    [FieldSize(FieldSizeAttribute.Unlimited)]
    public virtual String Text { get; set; }
}

using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using MainDemo.NonPersistent;

namespace MainDemo.Module.BusinessObjects.NonPersistent;

[DomainComponent]
public class CustomNonPersistentObject : NonPersistentObjectCloneable {
    private string name;

    public CustomNonPersistentObject() : base() { }
    public CustomNonPersistentObject(Guid oid) : base(oid) { }

    public string Name {
        get { return name; }
        set {
            if(name != value) {
                name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
    }

    public override NonPersistentObjectCloneable Clone(IObjectMap map) {
        var result = (CustomNonPersistentObject)base.Clone(map);
        result.name = name;
        return result;
    }
}

public abstract class NonPersistentObjectCloneable : NonPersistentBaseObject {
    public NonPersistentObjectCloneable() : base() { }
    public NonPersistentObjectCloneable(Guid oid) : base(oid) { }
    public virtual NonPersistentObjectCloneable Clone(IObjectMap map) {
        var clone = (NonPersistentObjectCloneable)Activator.CreateInstance(this.GetType(), Oid);
        map.AcceptObject(clone);
        return clone;
    }
}

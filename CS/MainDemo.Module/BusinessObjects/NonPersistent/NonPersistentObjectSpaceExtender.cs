using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Core;
using MainDemo.Module.BusinessObjects.NonPersistent;
using Microsoft.Extensions.DependencyInjection;

namespace MainDemo.NonPersistent;
public interface IObjectMap {
    object GetObject(object obj);
    void AcceptObject(object obj);
}
public class NonPersistentObjectSpaceExtender : IObjectMap {
    readonly NonPersistentObjectSpace objectSpace;
    readonly NonPersistentGlobalObjectStorage globalObjects;
    readonly Dictionary<Guid, NonPersistentObjectCloneable> localObjects;


    public NonPersistentObjectSpaceExtender(IServiceProvider serviceProvider, NonPersistentObjectSpace nonPersistentObjectSpace) {
        objectSpace = nonPersistentObjectSpace;
        globalObjects = serviceProvider.GetRequiredService<NonPersistentGlobalObjectStorage>();
        localObjects = new Dictionary<Guid, NonPersistentObjectCloneable>();

        objectSpace.Committing += ObjectSpace_Committing;
        objectSpace.ObjectsGetting += ObjectSpace_ObjectsGetting;
        objectSpace.ObjectByKeyGetting += ObjectSpace_ObjectByKeyGetting;
        objectSpace.ObjectGetting += ObjectSpace_ObjectGetting;
        objectSpace.Reloaded += ObjectSpace_Reloaded;
        objectSpace.Disposed += ObjectSpace_Disposed;
        objectSpace.ModifiedChanging += NonPersistentObjectSpace_ModifiedChanging;

        var objectSpaceProviderService = serviceProvider.GetRequiredService<IObjectSpaceProviderService>();
        var objectSpaceCustomizerService = serviceProvider.GetRequiredService<IObjectSpaceCustomizerService>();
        objectSpace.PopulateAdditionalObjectSpaces(objectSpaceProviderService, objectSpaceCustomizerService);
    }

    object IObjectMap.GetObject(object obj) {
        return objectSpace.GetObject(obj);
    }
    void IObjectMap.AcceptObject(object obj) {
        var keyObj = obj as NonPersistentObjectCloneable;
        if(keyObj != null) {
            localObjects.Add(keyObj.Oid, keyObj);
        }
    }
    private NonPersistentObjectCloneable GetObject(NonPersistentObjectCloneable obj) {
        if(!objectSpace.IsNewObject(obj)) {
            return GetObjectByKey(obj.Oid);
        }
        else {
            return obj;
        }
    }
    private NonPersistentObjectCloneable GetObjectByKey(Guid key) {
        NonPersistentObjectCloneable obj;
        if(!localObjects.TryGetValue(key, out obj)) {
            obj = LoadObject(key);
        }
        return obj;
    }
    private NonPersistentObjectCloneable LoadObject(Guid key) {
        var obj = globalObjects.FindObject(key);
        if(obj != null) {
            var clone = obj.Clone(this);
            ((IObjectSpaceLink)clone).ObjectSpace = objectSpace;
            return clone;
        }
        return null;
    }
    private void ObjectSpace_ObjectsGetting(Object sender, ObjectsGettingEventArgs e) {
        if(typeof(NonPersistentObjectCloneable).IsAssignableFrom(e.ObjectType)) {
            var objects = new BindingList<NonPersistentObjectCloneable>();
            objects.AllowNew = true;
            objects.AllowEdit = true;
            objects.AllowRemove = true;
            foreach(NonPersistentObjectCloneable obj in globalObjects.Objects) {
                if(e.ObjectType.IsAssignableFrom(obj.GetType())) {
                    objects.Add(GetObject(obj));
                }
            }
            e.Objects = objects;
        }
    }
    private void ObjectSpace_ObjectByKeyGetting(Object sender, ObjectByKeyGettingEventArgs e) {
        if(typeof(NonPersistentObjectCloneable).IsAssignableFrom(e.ObjectType) && e.Key is Guid) {
            e.Object = GetObjectByKey((Guid)e.Key);
        }
    }
    private void ObjectSpace_ObjectGetting(object sender, ObjectGettingEventArgs e) {
        var obj = e.SourceObject as NonPersistentObjectCloneable;
        if(obj != null) {
            e.TargetObject = GetObject(obj);
        }
    }
    private void ObjectSpace_Committing(Object sender, CancelEventArgs e) {
        var objectSpace = (NonPersistentObjectSpace)sender;
        foreach(Object obj in objectSpace.ModifiedObjects) {
            NonPersistentObjectCloneable baseObj = obj as NonPersistentObjectCloneable;
            if(baseObj != null) {
                if(objectSpace.IsDeletedObject(baseObj)) {
                    globalObjects.DeleteObject(baseObj);
                }
                else {
                    globalObjects.SaveObject(baseObj);
                }
            }
        }
    }
    private void ObjectSpace_Reloaded(object sender, EventArgs e) {
        localObjects.Clear();
    }
    private void NonPersistentObjectSpace_ModifiedChanging(object sender, ObjectSpaceModificationEventArgs e) {
        if(e.Object is NonPersistentObjectCloneable) {
            e.Cancel = false;
        }
    }
    private void ObjectSpace_Disposed(object sender, EventArgs e) {
        var objectSpace = (NonPersistentObjectSpace)sender;
        objectSpace.ObjectsGetting -= ObjectSpace_ObjectsGetting;
        objectSpace.ObjectByKeyGetting -= ObjectSpace_ObjectByKeyGetting;
        objectSpace.ObjectGetting -= ObjectSpace_ObjectGetting;
        objectSpace.Committing -= ObjectSpace_Committing;
        objectSpace.Reloaded -= ObjectSpace_Reloaded;
        objectSpace.ModifiedChanging -= NonPersistentObjectSpace_ModifiedChanging;
    }
}

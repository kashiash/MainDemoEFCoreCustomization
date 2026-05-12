using System.Collections.Concurrent;
using MainDemo.Module.BusinessObjects.NonPersistent;

namespace MainDemo.NonPersistent;

public class NonPersistentGlobalObjectStorage : IObjectMap {
    public ConcurrentDictionary<Guid, NonPersistentObjectCloneable> objectsCache { get; } = new();
    public IEnumerable<NonPersistentObjectCloneable> Objects { get { return objectsCache.Values; } }

    public NonPersistentGlobalObjectStorage() {
        CreateObject<CustomNonPersistentObject>("A");
        CreateObject<CustomNonPersistentObject>("B");
        CreateObject<CustomNonPersistentObject>("C");
    }
    private NonPersistentObjectCloneable CreateObject<T>(string value) where T : NonPersistentObjectCloneable, new() {
        T result = new T();
        if(result is CustomNonPersistentObject custom) {
            custom.Name = value;
        }
        objectsCache.TryAdd(result.Oid, result);
        return result;
    }

    public void Add(NonPersistentObjectCloneable obj) {
        objectsCache.TryAdd(obj.Oid, obj);
    }
    public NonPersistentObjectCloneable FindObject(Guid key) {
        NonPersistentObjectCloneable result;
        objectsCache.TryGetValue(key, out result);
        return result;
    }
    public void SaveObject(NonPersistentObjectCloneable obj) {
        var found = FindObject(obj.Oid);
        var clone = obj.Clone(this);
        if(found != null) {
            NonPersistentObjectCloneable value;
            objectsCache.TryRemove(found.Oid, out value);
        }
        objectsCache.TryAdd(clone.Oid, clone);
    }
    public void DeleteObject(NonPersistentObjectCloneable obj) {
        var found = FindObject(obj.Oid);
        if(found != null) {
            NonPersistentObjectCloneable value;
            objectsCache.TryRemove(found.Oid, out value);
        }
    }
    object IObjectMap.GetObject(object obj) {
        var keyObj = obj as NonPersistentObjectCloneable;
        if(keyObj != null) {
            return FindObject(keyObj.Oid) ?? throw new KeyNotFoundException(keyObj.Oid.ToString());
        }
        else {
            return obj;
        }
    }
    void IObjectMap.AcceptObject(object obj) {
    }
}


using MainDemo.Module.BusinessObjects.NonPersistent;
using MainDemo.WebAPI.TestInfrastructure;
using Xunit;

namespace MainDemo.WebAPI.Tests {
    public class CRUDNonPersistentTests : BaseWebApiTest {
        public CRUDNonPersistentTests(SharedTestHostHolder fixture) : base(fixture) { }

        [Fact]
        public async System.Threading.Tasks.Task GetBasic() {
            await WebApiClient.AuthenticateAsync("John", "");
            var items = await WebApiClient.GetAllAsync<CustomNonPersistentObject>();
            Assert.True(items.Length > 0);
        }
        [Fact]
        public async System.Threading.Tasks.Task Create() {
            await WebApiClient.AuthenticateAsync("Sam", "");
            var newNonPersistentObject = await WebApiClient.PostAsync(new CustomNonPersistentObject() {
                Name = "Test Data"
            });

            Assert.NotNull(newNonPersistentObject);
            Assert.Equal("Test Data", newNonPersistentObject.Name);

            var loadedObject = await WebApiClient.GetByKeyAsync<CustomNonPersistentObject>(newNonPersistentObject.Oid.ToString());
            Assert.Equal(newNonPersistentObject.Name, loadedObject.Name);
            Assert.Equal(newNonPersistentObject.Oid, loadedObject.Oid);

            var deletedObjectKey = (await WebApiClient.DeleteAsync<CustomNonPersistentObject>(newNonPersistentObject.Oid.ToString())).Oid;
            Assert.Equal(newNonPersistentObject.Oid, deletedObjectKey);
        }

        [Fact]
        public async System.Threading.Tasks.Task Update() {
            await WebApiClient.AuthenticateAsync("Sam", "");
            var newNonPersistentObject = await WebApiClient.PostAsync(new CustomNonPersistentObject() {
                Name = "Test Data2"
            });

            Assert.NotNull(newNonPersistentObject);
            Assert.Equal("Test Data2", newNonPersistentObject.Name);

            var loadedObject = await WebApiClient.GetByKeyAsync<CustomNonPersistentObject>(newNonPersistentObject.Oid.ToString());
            Assert.Equal(newNonPersistentObject.Name, loadedObject.Name);
            Assert.Equal(newNonPersistentObject.Oid, loadedObject.Oid);

            await WebApiClient.PatchAsync(newNonPersistentObject.Oid.ToString(), new CustomNonPersistentObject {
                Name = "Updated Value",
                Oid = newNonPersistentObject.Oid
            });

            loadedObject = await WebApiClient.GetByKeyAsync<CustomNonPersistentObject>(newNonPersistentObject.Oid.ToString());
            Assert.Equal("Updated Value", loadedObject.Name);
            Assert.Equal(newNonPersistentObject.Oid, loadedObject.Oid);

            var deletedObjectKey = (await WebApiClient.DeleteAsync<CustomNonPersistentObject>(newNonPersistentObject.Oid.ToString())).Oid;
            Assert.Equal(newNonPersistentObject.Oid, deletedObjectKey);
        }
    }
}

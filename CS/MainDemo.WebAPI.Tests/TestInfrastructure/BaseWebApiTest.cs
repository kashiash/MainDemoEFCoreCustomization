using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Microsoft.OData.UriParser;
using Xunit;

namespace MainDemo.WebAPI.TestInfrastructure {
    [Collection("SharedTestHost")]
    public abstract class BaseWebApiTest : IDisposable, IAsyncLifetime {
        protected readonly SharedTestHostHolder fixture;

        public BaseWebApiTest(SharedTestHostHolder fixture) {
            this.fixture = fixture;
        }

        private WebApiClient webApiClient;
        private HttpClient httpClient;

        protected WebApiClient WebApiClient {
            get {
                ArgumentNullException.ThrowIfNull(webApiClient);
                return webApiClient;
            }
            private set => webApiClient = value;
        }
        protected HttpClient HttpClient {
            get {
                ArgumentNullException.ThrowIfNull(httpClient);
                return httpClient;
            }
            private set => httpClient = value;
        }

        protected Task<HttpClient> CreateHttpClientAsync(IHost host) =>
            Task.FromResult(host.GetTestClient());

        protected Task<WebApiClient> CreateWebApiClientAsync(HttpClient httpClient) =>
            Task.FromResult(new WebApiClient(httpClient, true, null));


        public virtual async ValueTask InitializeAsync() {
            HttpClient = await CreateHttpClientAsync(fixture.Host);
            WebApiClient = await CreateWebApiClientAsync(HttpClient);
        }

        public virtual ValueTask DisposeAsync() {
            httpClient?.Dispose();
            return ValueTask.CompletedTask;
        }

        public virtual void Dispose() {
            httpClient?.Dispose();
        }
    }
}

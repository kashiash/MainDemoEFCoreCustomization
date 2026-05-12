using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MainDemo.WebAPI.TestInfrastructure {

    public class WebApiClient {
        readonly HttpClient httpClient;
        readonly bool useAuthorization;
        static readonly JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
        };

        private AuthenticationHeaderValue authorizationToken;

        public AuthenticationHeaderValue AuthorizationToken => authorizationToken;


        public WebApiClient(HttpClient httpClient, bool useAuthorization, string authToken = null) {
            this.httpClient = httpClient;
            this.useAuthorization = useAuthorization;
            Authenticate(authToken);
        }

        #region Inner Methods
        public virtual string GetTypeUri<T>(params string[] expandProperties) => GetTypeUri(typeof(T), expandProperties);
        public virtual string GetTypeUri(Type type, params string[] expandProperties) => GetTypeUri(type, null, expandProperties);
        public virtual string GetTypeUri<T>(string key, params string[] expandProperties) => GetTypeUri(typeof(T), key, expandProperties);
        public virtual string GetTypeUri(Type type, string key, params string[] expandProperties) {
            string uri = "/api/odata/" + type.Name + (key == default ? string.Empty : $"/{key}");
            if(expandProperties.Length > 0) {
                uri += "?$expand=";
                foreach(string property in expandProperties) {
                    uri += property;
                    uri += ',';
                }
            }
            return uri;
        }

        public static HttpRequestMessage SerializeToHttpRequest(HttpMethod method, string uri, Type type, object value) {
            var request = new HttpRequestMessage(method, uri);

            var json = JsonSerializer.Serialize(value, type, jsonSerializerOptions);

            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            Console.WriteLine($"{method} {uri} -> {json}");
            return request;
        }

        public HttpRequestMessage SerializeToHttpRequest<T>(HttpMethod method, string uri, T value) {
            return SerializeToHttpRequest(method, uri, typeof(T), value!);
        }

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, bool preventAuthorization = false) {
            if(useAuthorization && !preventAuthorization && authorizationToken != null) {
                request.Headers.Authorization = authorizationToken;
            }
            var httpResponse = await httpClient.SendAsync(request);

            if(!httpResponse.IsSuccessStatusCode) {
                using(var stream = await httpResponse.Content.ReadAsStreamAsync()) {
                    using(StreamReader reader = new StreamReader(stream)) {
                        throw new HttpRequestException(httpResponse.ReasonPhrase + " : " + reader.ReadToEnd(), null, httpResponse.StatusCode);
                    }
                }
            }

            return httpResponse;
        }

        public async Task<JsonElement> RequestAsync(HttpRequestMessage request, bool preventAuthorization = false) {
            using var httpResponse = await SendAsync(request, preventAuthorization);
            if(httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound) {
                throw new HttpRequestException($"{request.Method} request has no JSON! Code {(int)httpResponse.StatusCode}, '{httpResponse.ReasonPhrase}'");
            }
            if(httpResponse.StatusCode == System.Net.HttpStatusCode.NoContent) {
                return new JsonElement();
            }
            else {
                return await httpResponse.Content.ReadFromJsonAsync<JsonElement>();
            }
        }

        public async Task<object> RequestAsync(Type type, HttpRequestMessage request, bool preventAuthorization = false) {
            var json = await RequestAsync(request, preventAuthorization);

            return json.Deserialize(type)
                ?? throw new NullReferenceException();
        }

        public async Task<T> RequestAsync<T>(HttpRequestMessage request, bool preventAuthorization = false) {
            return (T)await RequestAsync(typeof(T), request, preventAuthorization);
        }
        #endregion

        #region Generic API Methods
        public Task<T[]> GetAllAsync<T>(params string[] expandProperties) {
            return GetAllAsync<T>(false, expandProperties);
        }
        public async Task<T[]> GetFilteredAsync<T>(string filter, bool preventAuthorization = false) {
            string url = string.Format("{0}?$filter={1}", GetTypeUri<T>(), filter);
            var json = await RequestAsync(
                new HttpRequestMessage(HttpMethod.Get, url), preventAuthorization);

            return json.GetProperty("value").Deserialize<T[]>()
                ?? throw new NullReferenceException();
        }

        public async Task<T[]> GetAllAsync<T>(bool preventAuthorization = false, params string[] expandProperties) {
            var json = await RequestAsync(
                new HttpRequestMessage(HttpMethod.Get, GetTypeUri<T>(expandProperties)), preventAuthorization);

            return json.GetProperty("value").Deserialize<T[]>()
                ?? throw new NullReferenceException();
        }

        public Task<T> GetByKeyAsync<T>(string key, params string[] expandProperties)
            => RequestAsync<T>(
                new HttpRequestMessage(HttpMethod.Get, GetTypeUri<T>(key, expandProperties)));
        public async Task<byte[]> DownloadStream<T>(string key, string propertyName) {
            var httpResponse = await SendAsync(
               new HttpRequestMessage(HttpMethod.Get, $"/api/MediaFile/DownloadStream?objectType={typeof(T).Name}&objectKey={key}&propertyName={propertyName}"));
            return await httpResponse.Content.ReadAsByteArrayAsync();
        }
        public Task<T> DeleteAsync<T>(string key)
            => RequestAsync<T>(
                new HttpRequestMessage(HttpMethod.Delete, GetTypeUri<T>(key)));

        public Task<T> PostAsync<T>(T value) where T : notnull
            => RequestAsync<T>(
                SerializeToHttpRequest(HttpMethod.Post, GetTypeUri<T>(), value));

        public Task PutAsync<T>(string key, T value) where T : notnull
            => SendAsync(
                SerializeToHttpRequest(HttpMethod.Put, GetTypeUri<T>(key), value));

        public Task PatchAsync<T>(string key, T value) where T : notnull
            => SendAsync(
                SerializeToHttpRequest(HttpMethod.Patch, GetTypeUri<T>(key), value));
        public async Task<U[]> GetRefAsync<T, U>(string key, string navigationProperty) {
            var json = await RequestAsync(
               new HttpRequestMessage(HttpMethod.Get, $"{GetTypeUri<T>(key)}/{navigationProperty}/$ref"));
            return json.GetProperty("value").Deserialize<U[]>()
                ?? throw new NullReferenceException();
        }
        public async Task<U> GetRefSingleAsync<T, U>(string key, string navigationProperty) where U : class {
            var json = await RequestAsync(
               new HttpRequestMessage(HttpMethod.Get, $"{GetTypeUri<T>(key)}/{navigationProperty}/$ref"));
            if(json.ValueKind == JsonValueKind.Undefined) {
                return default(U);
            }
            return json.Deserialize<U>()
              ?? throw new NullReferenceException();
        }
        public async Task CreateRefAsync<T, U>(string key, string navigationProperty, string relatedKey) {
            var body = new Dictionary<string, object>();
            body["@odata.id"] = new Uri(httpClient.BaseAddress, GetTypeUri<U>(relatedKey));

            await SendAsync(
                SerializeToHttpRequest(HttpMethod.Post, $"{GetTypeUri<T>(key)}/{navigationProperty}/$ref", body));
        }
        public async Task DeleteRefAsync<T, U>(string key, string navigationProperty, string relatedKey = null) {
            string uri = relatedKey != null ?
                $"{GetTypeUri<T>(key)}/{navigationProperty}/{relatedKey}/$ref" :
                $"{GetTypeUri<T>(key)}/{navigationProperty}/$ref";
            await SendAsync(
               new HttpRequestMessage(HttpMethod.Delete, uri));
        }
        #endregion

        #region Non-generic API Methods

        public async Task<object[]> GetAllAsync(Type type, bool preventAuthorization = false) {
            var json = await RequestAsync(
                new HttpRequestMessage(HttpMethod.Get, GetTypeUri(type)), preventAuthorization);

            return (object[])(json.GetProperty("value").Deserialize(type.MakeArrayType())
                ?? throw new NullReferenceException());
        }

        public Task<object> GetByKeyAsync(Type type, string key)
            => RequestAsync(type,
                new HttpRequestMessage(HttpMethod.Get, GetTypeUri(type, key)));

        public Task<object> DeleteAsync(Type type, string key)
            => RequestAsync(type,
                new HttpRequestMessage(HttpMethod.Delete, GetTypeUri(type, key)));

        public Task<object> PostAsync(Type type, object value)
            => RequestAsync(type,
                SerializeToHttpRequest(HttpMethod.Post, GetTypeUri(type, Array.Empty<string>()), type, value));

        public Task PutAsync(Type type, string key, object value)
            => SendAsync(
                SerializeToHttpRequest(HttpMethod.Put, GetTypeUri(type, key), type, value));

        public Task PatchAsync(Type type, string key, object value)
            => SendAsync(
                SerializeToHttpRequest(HttpMethod.Patch, GetTypeUri(type, key), type, value));

        public async Task<string> AuthenticateAsync(string userName, string password) {
            return Authenticate(await GetUserTokenAsync(userName, password));
        }
        public string Authenticate(string tokenString) {
            if(!string.IsNullOrEmpty(tokenString)) {
                this.authorizationToken = new AuthenticationHeaderValue("Bearer", tokenString);
            }
            else {
                this.authorizationToken = null;
            }
            return tokenString;
        }
        public Task<string> GetUserTokenAsync(string userName, string password) {
            return GetUserTokenAsync(userName, password, "/api/Authentication/Authenticate");
        }
        public async Task<string> GetUserTokenAsync(string userName, string password, string requestPath) {
            var request = new HttpRequestMessage(HttpMethod.Post, requestPath);
            request.Content = new StringContent(
                $"{{ \"userName\": \"{userName}\", \"password\": \"{password}\" }}", Encoding.UTF8, "application/json");

            var httpResponse = await httpClient.SendAsync(request);
            if(!httpResponse.IsSuccessStatusCode) {
                throw new UnauthorizedAccessException($"Authorization request failed! Code {(int)httpResponse.StatusCode}, '{httpResponse.ReasonPhrase}'");
            }
            var tokenString = await httpResponse.Content.ReadAsStringAsync();
            return tokenString;
        }

        #endregion

    }
}

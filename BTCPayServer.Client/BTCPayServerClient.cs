using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BTCPayServer.Client
{
    public partial class BTCPayServerClient
    {
        private readonly string _apiKey;
        private readonly Uri _btcpayHost;
        private readonly HttpClient _httpClient;

        public string APIKey => _apiKey;

        private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public BTCPayServerClient(Uri btcpayHost, string APIKey, HttpClient httpClient = null)
        {
            _apiKey = APIKey;
            _btcpayHost = btcpayHost;
            _httpClient = httpClient ?? new HttpClient();
        }

        protected void HandleResponse(HttpResponseMessage message)
        {
            message.EnsureSuccessStatusCode();
        }

        protected async Task<T> HandleResponse<T>(HttpResponseMessage message)
        {
            HandleResponse(message);
            return JsonSerializer.Deserialize<T>(await message.Content.ReadAsStringAsync(), _serializerOptions);
        }

        protected virtual HttpRequestMessage CreateHttpRequest(string path,
            Dictionary<string, object> queryPayload = null,
            HttpMethod method = null)
        {
            UriBuilder uriBuilder = new UriBuilder(_btcpayHost) {Path = path};
            if (queryPayload != null && queryPayload.Any())
            {
                AppendPayloadToQuery(uriBuilder, queryPayload);
            }

            var httpRequest = new HttpRequestMessage(method ?? HttpMethod.Get, uriBuilder.Uri);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("token", _apiKey);


            return httpRequest;
        }

        protected virtual HttpRequestMessage CreateHttpRequest<T>(string path,
            Dictionary<string, object> queryPayload = null,
            T bodyPayload = default, HttpMethod method = null)
        {
            var request = CreateHttpRequest(path, queryPayload, method);
            if (typeof(T).IsPrimitive || !EqualityComparer<T>.Default.Equals(bodyPayload, default(T)))
            {
                request.Content = new StringContent(JsonSerializer.Serialize(bodyPayload, _serializerOptions), Encoding.UTF8, "application/json");
            }

            return request;
        }

        private static void AppendPayloadToQuery(UriBuilder uri, Dictionary<string, object> payload)
        {
            if (uri.Query.Length > 1)
                uri.Query += "&";
            foreach (KeyValuePair<string, object> keyValuePair in payload)
            {
                UriBuilder uriBuilder = uri;
                if (keyValuePair.Value.GetType().GetInterfaces().Contains((typeof(IEnumerable))))
                {
                    foreach (var item in (IEnumerable)keyValuePair.Value)
                    {
                        uriBuilder.Query = uriBuilder.Query + Uri.EscapeDataString(keyValuePair.Key) + "=" +
                                           Uri.EscapeDataString(item.ToString()) + "&";
                    }
                }
                else
                {
                    uriBuilder.Query = uriBuilder.Query + Uri.EscapeDataString(keyValuePair.Key) + "=" +
                                       Uri.EscapeDataString(keyValuePair.Value.ToString()) + "&";
                }
            }

            uri.Query = uri.Query.Trim('&');
        }
    }
}

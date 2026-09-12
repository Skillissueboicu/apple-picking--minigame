using System;
using System.Text;
using System.Threading.Tasks;
using FarmerQuest.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine.Networking;

namespace FarmerQuest.Net
{
    /// <summary>
    /// HTTP API failure with status code and response body/message
    /// </summary>
    public sealed class ApiException : Exception
    {
        public long StatusCode { get; }
        public ApiException(long statusCode, string message) : base(message) => StatusCode = statusCode;
    }

    // UnityWebRequest JSON client with bearer auth
    public sealed class ApiClient
    {
        private static readonly JsonSerializerSettings JsonSettings = new()
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
        };

        private readonly AuthSession _auth;

        public ApiClient(AuthSession auth) => _auth = auth;

        private static string Combine(string path) =>
            $"{AppConfig.ApiBaseUrl}/{path.TrimStart('/')}";

        public Task<T> GetAsync<T>(string path) => SendAsync<T>(UnityWebRequest.kHttpVerbGET, path, null);
        public Task<T> PostAsync<T>(string path, object body) => SendAsync<T>(UnityWebRequest.kHttpVerbPOST, path, body);

        // Sends a JSON request and deserializes the response body
        public async Task<T> SendAsync<T>(string method, string path, object body)
        {
            using var req = new UnityWebRequest(Combine(path), method)
            {
                downloadHandler = new DownloadHandlerBuffer(),
            };

            if (body != null)
            {
                string json = JsonConvert.SerializeObject(body, JsonSettings);
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                req.SetRequestHeader("Content-Type", "application/json");
            }

            req.SetRequestHeader("Accept", "application/json");
            if (!string.IsNullOrEmpty(_auth?.Token))
                req.SetRequestHeader("Authorization", $"Bearer {_auth.Token}");

            // Bridge UnityWebRequest completion to async/await
            var tcs = new TaskCompletionSource<bool>();
            var op = req.SendWebRequest();
            op.completed += _ => tcs.TrySetResult(true);
            await tcs.Task;

            if (req.result != UnityWebRequest.Result.Success)
            {
                string err = req.downloadHandler?.text;
                if (string.IsNullOrWhiteSpace(err)) err = req.error;
                throw new ApiException(req.responseCode, err);
            }

            string text = req.downloadHandler?.text;
            if (string.IsNullOrEmpty(text) || typeof(T) == typeof(object))
                return default;

            return JsonConvert.DeserializeObject<T>(text, JsonSettings);
        }
    }
}

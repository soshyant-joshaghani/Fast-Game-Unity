using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace FastGame
{
    public sealed class FastGameHttp
    {
        readonly FastGameConfig _config;
        string _accessToken;

        public FastGameHttp(FastGameConfig config)
        {
            _config = config ?? new FastGameConfig();
            _config.ApiBaseUrl = FastGameConfig.NormalizeApiBaseUrl(_config.ApiBaseUrl);
            Debug.Log("FastGame: ApiBaseUrl=" + _config.ApiBaseUrl);
        }

        public string AccessToken
        {
            get => _accessToken;
            set => _accessToken = value;
        }

        public string ApiBaseUrl => _config.ApiBaseUrl.TrimEnd('/');

        public async Task<string> RequestRawAsync(
            string method,
            string path,
            string body = null,
            string contentType = "application/json",
            bool formUrlEncoded = false)
        {
            var url = path.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? path
                : ApiBaseUrl + (path.StartsWith("/") ? path : "/" + path);

            using var req = new UnityWebRequest(url, method);
            if (!string.IsNullOrEmpty(body))
            {
                var bytes = Encoding.UTF8.GetBytes(body);
                req.uploadHandler = new UploadHandlerRaw(bytes);
            }
            req.downloadHandler = new DownloadHandlerBuffer();
            if (!string.IsNullOrEmpty(contentType))
                req.SetRequestHeader("Content-Type", contentType);
            if (!string.IsNullOrEmpty(_accessToken))
                req.SetRequestHeader("Authorization", "Bearer " + _accessToken);

            var op = req.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

#if UNITY_2020_2_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
            {
                throw new FastGameException($"{(int)req.responseCode}: {req.downloadHandler?.text ?? req.error}");
            }

            return req.downloadHandler?.text ?? "";
        }

        public async Task<T> GetJsonAsync<T>(string path)
        {
            var text = await RequestRawAsync("GET", path);
            if (string.IsNullOrEmpty(text)) return default;
            return JsonUtility.FromJson<T>(WrapIfArray<T>(text));
        }

        public async Task<T> PostJsonAsync<T>(string path, object body)
        {
            var json = body == null ? "{}" : JsonUtility.ToJson(body);
            var text = await RequestRawAsync("POST", path, json);
            if (string.IsNullOrEmpty(text)) return default;
            return JsonUtility.FromJson<T>(WrapIfArray<T>(text));
        }

        public async Task<T> PutJsonAsync<T>(string path, object body)
        {
            var json = body == null ? "{}" : JsonUtility.ToJson(body);
            var text = await RequestRawAsync("PUT", path, json);
            if (string.IsNullOrEmpty(text)) return default;
            return JsonUtility.FromJson<T>(WrapIfArray<T>(text));
        }

        public async Task<string> PostFormAsync(string path, Dictionary<string, string> fields)
        {
            var parts = new List<string>();
            foreach (var kv in fields)
                parts.Add(UnityWebRequest.EscapeURL(kv.Key) + "=" + UnityWebRequest.EscapeURL(kv.Value ?? ""));
            var body = string.Join("&", parts);
            return await RequestRawAsync(
                "POST",
                path,
                body,
                "application/x-www-form-urlencoded");
        }

        /// <summary>
        /// Append foxg-back style <c>lang</c> / <c>expand_i18n</c> for resolved labels / full maps.
        /// </summary>
        public static string AppendI18nQuery(string path, string lang = null, bool expandI18n = false)
        {
            if (string.IsNullOrEmpty(lang) && !expandI18n) return path ?? "";
            var needAmp = (path ?? "").Contains("?");
            void Add(ref string p, string key, string value)
            {
                p += needAmp ? "&" : "?";
                needAmp = true;
                p += UnityWebRequest.EscapeURL(key) + "=" + UnityWebRequest.EscapeURL(value ?? "");
            }
            var outPath = path ?? "";
            if (!string.IsNullOrEmpty(lang)) Add(ref outPath, "lang", lang);
            if (expandI18n) Add(ref outPath, "expand_i18n", "true");
            return outPath;
        }

        /// <summary>
        /// JsonUtility cannot deserialize top-level arrays; wrap as {"items":[...]} when needed.
        /// Prefer <see cref="FastGameJson"/> helpers for lists.
        /// </summary>
        static string WrapIfArray<T>(string text)
        {
            var t = text.TrimStart();
            if (t.StartsWith("["))
                return "{\"items\":" + text + "}";
            return text;
        }
    }

    public sealed class FastGameException : Exception
    {
        public FastGameException(string message) : base(message) { }
    }
}

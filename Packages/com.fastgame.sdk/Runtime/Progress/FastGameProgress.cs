using System.Collections.Generic;
using System.Threading.Tasks;

namespace FastGame
{
    /// <summary>Progress.Get / Progress.Save — official user_progress (A5 / B5).</summary>
    public sealed class FastGameProgress
    {
        readonly FastGameHttp _http;

        public FastGameProgress(FastGameHttp http)
        {
            _http = http;
        }

        string Base(string gameCode) => $"/apps/games/progress/{Escape(gameCode)}";

        /// <summary>GET progress for game (+ optional map scope).</summary>
        public async Task<Dictionary<string, object>> GetAsync(string gameCode, string mapId = "")
        {
            var path = Base(gameCode);
            if (!string.IsNullOrEmpty(mapId))
                path += $"?map_id={Escape(mapId)}";
            var text = await _http.RequestRawAsync("GET", path);
            return FastGameJson.ParseObject(text) ?? new Dictionary<string, object>();
        }

        /// <summary>
        /// POST validated progress event. Client must not send score/win/finished.
        /// </summary>
        public async Task<Dictionary<string, object>> SaveAsync(
            string gameCode,
            string eventType,
            string mapId = "",
            Dictionary<string, object> payload = null)
        {
            var body = new Dictionary<string, object>
            {
                ["event_type"] = eventType ?? "",
                ["map_id"] = mapId ?? "",
                ["payload"] = payload ?? new Dictionary<string, object>(),
            };
            var text = await _http.RequestRawAsync(
                "POST", $"{Base(gameCode)}/events", FastGameJson.Stringify(body));
            return FastGameJson.ParseObject(text) ?? new Dictionary<string, object>();
        }

        static string Escape(string s) =>
            string.IsNullOrEmpty(s) ? "" : System.Uri.EscapeDataString(s);
    }
}

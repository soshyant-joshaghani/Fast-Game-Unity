using System.Collections.Generic;
using System.Threading.Tasks;
using FastGame.Models;

namespace FastGame
{
    public sealed class FastGameCatalog
    {
        readonly FastGameHttp _http;

        public FastGameCatalog(FastGameHttp http)
        {
            _http = http;
        }

        /// <param name="lang">BCP-47 tag for resolved labels (e.g. fa). Empty = server default / Accept-Language.</param>
        /// <param name="expandI18n">When true, responses include full translations maps.</param>
        public async Task<List<GameCatalog>> ListGamesAsync(
            bool availableOnly = false,
            string lang = null,
            bool expandI18n = false)
        {
            var q = availableOnly ? "true" : "false";
            var path = FastGameHttp.AppendI18nQuery(
                $"/apps/games/catalog/?available_only={q}", lang, expandI18n);
            var text = await _http.RequestRawAsync("GET", path);
            var arr = FastGameJson.ParseArray(text) ?? new List<object>();
            var list = new List<GameCatalog>();
            foreach (var item in arr)
            {
                var g = FastGameDto.ParseGame(item as Dictionary<string, object>);
                if (g != null) list.Add(g);
            }
            return list;
        }

        public async Task<GameCatalogDetail> GetGameAsync(
            string gameId,
            string lang = null,
            bool expandI18n = false)
        {
            var path = FastGameHttp.AppendI18nQuery(
                $"/apps/games/catalog/{Escape(gameId)}", lang, expandI18n);
            var text = await _http.RequestRawAsync("GET", path);
            return FastGameDto.ParseGameDetail(FastGameJson.ParseObject(text));
        }

        /// <summary>Public auth gates for new-user OTP (no login required).</summary>
        public async Task<(bool VerifyPhone, bool VerifyEmail)> GetAuthRequirementsAsync(string gameId)
        {
            var text = await _http.RequestRawAsync(
                "GET", $"/apps/games/catalog/{Escape(gameId)}/auth-requirements");
            var o = FastGameJson.ParseObject(text);
            return (
                FastGameJson.GetBool(o, "verify_phone"),
                FastGameJson.GetBool(o, "verify_email"));
        }

        /// <summary>
        /// Legacy public WS URL. Prefer <see cref="FastGameRealtime.JoinMapAsync"/> /
        /// seat <c>game_server_url</c> for online join.
        /// </summary>
        public async Task<GameServerInfo> GetGameServerAsync()
        {
            var text = await _http.RequestRawAsync("GET", "/utils/game-server/");
            var o = FastGameJson.ParseObject(text);
            return new GameServerInfo { Url = FastGameJson.GetString(o, "url") };
        }

        static string Escape(string s) => UnityEngine.Networking.UnityWebRequest.EscapeURL(s ?? "");
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;
using FastGame.Models;

namespace FastGame
{
    public sealed class FastGameContent
    {
        readonly FastGameHttp _http;
        readonly FastGameCatalog _catalog;

        public FastGameContent(FastGameHttp http, FastGameCatalog catalog)
        {
            _http = http;
            _catalog = catalog;
        }

        string Base(string gameId) => $"/apps/games/content/{Escape(gameId)}";

        public async Task<List<Character>> ListCharactersAsync(
            string gameId,
            string role = null,
            string lang = null,
            bool expandI18n = false)
        {
            var path = $"{Base(gameId)}/characters";
            if (!string.IsNullOrEmpty(role))
                path += $"?role={Escape(role)}";
            path = FastGameHttp.AppendI18nQuery(path, lang, expandI18n);
            var text = await _http.RequestRawAsync("GET", path);
            var arr = FastGameJson.ParseArray(text) ?? new List<object>();
            var list = new List<Character>();
            foreach (var item in arr)
            {
                var c = FastGameDto.ParseCharacter(item as Dictionary<string, object>);
                if (c != null) list.Add(c);
            }
            return list;
        }

        public async Task<Dictionary<string, object>> ClaimEventAsync(string gameId, string eventId)
        {
            var text = await _http.RequestRawAsync(
                "POST",
                $"{Base(gameId)}/events/{Escape(eventId)}/claim");
            return FastGameJson.ParseObject(text) ?? new Dictionary<string, object>();
        }

        public async Task<List<Cosmetic>> ListCosmeticsAsync(string gameId, string characterId)
        {
            var text = await _http.RequestRawAsync(
                "GET",
                $"{Base(gameId)}/characters/{Escape(characterId)}/cosmetics");
            var arr = FastGameJson.ParseArray(text) ?? new List<object>();
            var list = new List<Cosmetic>();
            foreach (var item in arr)
            {
                var o = item as Dictionary<string, object>;
                if (o == null) continue;
                list.Add(new Cosmetic
                {
                    Id = FastGameJson.GetString(o, "id"),
                    CosmeticId = FastGameJson.GetString(o, "cosmetic_id"),
                    Slot = FastGameJson.GetString(o, "slot"),
                    Label = FastGameJson.GetString(o, "label"),
                    Availability = FastGameJson.GetString(o, "availability"),
                    Price = FastGameJson.GetInt(o, "price"),
                    AssetRef = FastGameJson.GetString(o, "asset_ref"),
                });
            }
            return list;
        }

        public async Task<List<Ability>> ListAbilitiesAsync(string gameId, string characterId)
        {
            var text = await _http.RequestRawAsync(
                "GET",
                $"{Base(gameId)}/characters/{Escape(characterId)}/abilities");
            var arr = FastGameJson.ParseArray(text) ?? new List<object>();
            var list = new List<Ability>();
            foreach (var item in arr)
            {
                var o = item as Dictionary<string, object>;
                if (o == null) continue;
                list.Add(new Ability
                {
                    Id = FastGameJson.GetString(o, "id"),
                    AbilityId = FastGameJson.GetString(o, "ability_id"),
                    Label = FastGameJson.GetString(o, "label"),
                    Kind = FastGameJson.GetString(o, "kind"),
                    Params = FastGameJson.GetObject(o, "params") ?? new Dictionary<string, object>(),
                });
            }
            return list;
        }

        public async Task<Dictionary<string, object>> GetMapRuntimeAsync(
            string gameId,
            string mapId,
            string lang = null,
            bool expandI18n = false)
        {
            var path = FastGameHttp.AppendI18nQuery(
                $"{Base(gameId)}/maps/{Escape(mapId)}/runtime", lang, expandI18n);
            var text = await _http.RequestRawAsync("GET", path);
            return FastGameJson.ParseObject(text) ?? new Dictionary<string, object>();
        }

        public async Task<Dictionary<string, object>> ResolveSpawnAsync(
            string gameId,
            string mapId,
            string modeId = null,
            string preferredSpawnId = null,
            string lang = null,
            bool expandI18n = false)
        {
            var body = new Dictionary<string, object> { { "map_id", mapId } };
            if (!string.IsNullOrEmpty(modeId)) body["mode_id"] = modeId;
            if (!string.IsNullOrEmpty(preferredSpawnId)) body["preferred_spawn_id"] = preferredSpawnId;
            var path = FastGameHttp.AppendI18nQuery(
                $"{Base(gameId)}/players/me/spawn", lang, expandI18n);
            var text = await _http.RequestRawAsync(
                "POST",
                path,
                FastGameJson.Stringify(body));
            return FastGameJson.ParseObject(text) ?? new Dictionary<string, object>();
        }

        public async Task<Loadout> GetLoadoutAsync(string gameId)
        {
            var text = await _http.RequestRawAsync("GET", $"{Base(gameId)}/players/me/loadout");
            return FastGameDto.ParseLoadout(FastGameJson.ParseObject(text));
        }

        public async Task<Loadout> SetLoadoutAsync(
            string gameId,
            string characterId = null,
            Dictionary<string, string> equippedCosmetics = null,
            Dictionary<string, string> modularParts = null)
        {
            var body = new Dictionary<string, object>();
            if (characterId != null) body["character_id"] = characterId;
            if (equippedCosmetics != null)
            {
                var cos = new Dictionary<string, object>();
                foreach (var kv in equippedCosmetics) cos[kv.Key] = kv.Value;
                body["equipped_cosmetics"] = cos;
            }
            if (modularParts != null)
            {
                var parts = new Dictionary<string, object>();
                foreach (var kv in modularParts) parts[kv.Key] = kv.Value;
                body["modular_parts"] = parts;
            }
            var text = await _http.RequestRawAsync(
                "PUT",
                $"{Base(gameId)}/players/me/loadout",
                FastGameJson.Stringify(body));
            return FastGameDto.ParseLoadout(FastGameJson.ParseObject(text));
        }

        public async Task<Dictionary<string, object>> ClaimPickupAsync(
            string gameId,
            string mapId,
            string pickupId,
            string placementId = null)
        {
            var body = new Dictionary<string, object>
            {
                { "map_id", mapId },
                { "pickup_id", pickupId },
            };
            if (!string.IsNullOrEmpty(placementId)) body["placement_id"] = placementId;
            var text = await _http.RequestRawAsync(
                "POST",
                $"{Base(gameId)}/players/me/pickup-claim",
                FastGameJson.Stringify(body));
            return FastGameJson.ParseObject(text) ?? new Dictionary<string, object>();
        }

        /// <summary>
        /// Catalog + map runtime + spawn. Does not connect Colyseus.
        /// </summary>
        public async Task<PreparedSession> PrepareSessionAsync(
            string gameId,
            string modeId,
            string mapId,
            string lang = null,
            bool expandI18n = false)
        {
            var game = await _catalog.GetGameAsync(gameId, lang, expandI18n);
            var runtime = await GetMapRuntimeAsync(gameId, mapId, lang, expandI18n);
            var spawn = await ResolveSpawnAsync(gameId, mapId, modeId, null, lang, expandI18n);
            return new PreparedSession
            {
                Game = game,
                MapRuntime = runtime,
                Spawn = spawn,
                GameId = gameId,
                ModeId = modeId,
                MapId = mapId,
                ColyseusRoom = game?.ColyseusRoom,
            };
        }

        static string Escape(string s) => UnityEngine.Networking.UnityWebRequest.EscapeURL(s ?? "");
    }
}

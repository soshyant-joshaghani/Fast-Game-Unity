using System.Collections.Generic;
using FastGame.Models;

namespace FastGame
{
    internal static class FastGameDto
    {
        public static GameCatalog ParseGame(Dictionary<string, object> o)
        {
            if (o == null) return null;
            return new GameCatalog
            {
                Id = FastGameJson.GetString(o, "id"),
                GameId = FastGameJson.GetString(o, "game_id"),
                Label = FastGameJson.GetString(o, "label"),
                Description = FastGameJson.GetString(o, "description"),
                ColyseusRoom = FastGameJson.GetString(o, "colyseus_room"),
                Available = FastGameJson.GetBool(o, "available", true),
                Translations = ParseTranslations(FastGameJson.GetObject(o, "translations")),
            };
        }

        public static GameCatalogDetail ParseGameDetail(Dictionary<string, object> o)
        {
            var g = ParseGame(o);
            if (g == null) return null;
            var d = new GameCatalogDetail
            {
                Id = g.Id,
                GameId = g.GameId,
                Label = g.Label,
                Description = g.Description,
                ColyseusRoom = g.ColyseusRoom,
                Available = g.Available,
            };
            var authReq = FastGameJson.GetObject(o, "auth_requirements");
            if (authReq != null)
            {
                d.AuthVerifyPhone = FastGameJson.GetBool(authReq, "verify_phone");
                d.AuthVerifyEmail = FastGameJson.GetBool(authReq, "verify_email");
            }
            foreach (var item in FastGameJson.GetArray(o, "modes") ?? new List<object>())
            {
                var m = item as Dictionary<string, object>;
                if (m == null) continue;
                d.Modes.Add(new GameMode
                {
                    Id = FastGameJson.GetString(m, "id"),
                    ModeId = FastGameJson.GetString(m, "mode_id"),
                    Topology = FastGameJson.GetString(m, "topology"),
                    WinKind = FastGameJson.GetString(m, "win_kind"),
                    MinPlayers = FastGameJson.GetInt(m, "min_players"),
                    MaxPlayers = FastGameJson.GetInt(m, "max_players"),
                    Kind = FastGameJson.GetString(m, "kind"),
                });
            }
            foreach (var item in FastGameJson.GetArray(o, "maps") ?? new List<object>())
            {
                var m = item as Dictionary<string, object>;
                if (m == null) continue;
                var map = new GameMap
                {
                    Id = FastGameJson.GetString(m, "id"),
                    MapId = FastGameJson.GetString(m, "map_id"),
                    Label = FastGameJson.GetString(m, "label"),
                    Purchasable = FastGameJson.GetBool(m, "purchasable"),
                    Price = FastGameJson.GetInt(m, "price"),
                    Translations = ParseTranslations(FastGameJson.GetObject(m, "translations")),
                };
                foreach (var sm in FastGameJson.GetArray(m, "supported_modes") ?? new List<object>())
                    map.SupportedModes.Add(sm?.ToString());
                d.Maps.Add(map);
            }
            foreach (var item in FastGameJson.GetArray(o, "asset_packs") ?? new List<object>())
            {
                var p = item as Dictionary<string, object>;
                if (p == null) continue;
                d.AssetPacks.Add(ParsePack(p));
            }
            return d;
        }

        public static AssetPack ParsePack(Dictionary<string, object> p)
        {
            return new AssetPack
            {
                Id = FastGameJson.GetString(p, "id"),
                PackId = FastGameJson.GetString(p, "pack_id"),
                Label = FastGameJson.GetString(p, "label"),
                Revision = FastGameJson.GetInt(p, "revision"),
                Version = FastGameJson.GetString(p, "version"),
                Url = FastGameJson.GetString(p, "url"),
                Hash = FastGameJson.GetString(p, "hash"),
            };
        }

        public static Character ParseCharacter(Dictionary<string, object> o)
        {
            var stats = FastGameJson.GetObject(o, "stats") ?? new Dictionary<string, object>();
            return new Character
            {
                Id = FastGameJson.GetString(o, "id"),
                CharacterId = FastGameJson.GetString(o, "character_id"),
                Label = FastGameJson.GetString(o, "label"),
                Role = FastGameJson.GetString(o, "role") ?? "player",
                BodyKind = FastGameJson.GetString(o, "body_kind"),
                Stats = stats,
                Translations = ParseTranslations(FastGameJson.GetObject(o, "translations")),
                SortOrder = FastGameJson.GetInt(o, "sort_order"),
            };
        }

        /// <summary>
        /// Locale codes en|fa|ar → name/description. See docs/entity-locales.md.
        /// Resolve: locale → en → fallbackLabel.
        /// </summary>
        public static Dictionary<string, LocaleCopy> ParseTranslations(Dictionary<string, object> o)
        {
            var map = new Dictionary<string, LocaleCopy>();
            if (o == null) return map;
            foreach (var code in new[] { "en", "fa", "ar" })
            {
                var entry = FastGameJson.GetObject(o, code);
                if (entry == null) continue;
                var name = FastGameJson.GetString(entry, "name") ?? "";
                var description = FastGameJson.GetString(entry, "description") ?? "";
                if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(description))
                    continue;
                map[code] = new LocaleCopy { Name = name, Description = description };
            }
            return map;
        }

        public static string DisplayName(
            Dictionary<string, LocaleCopy> translations,
            string fallbackLabel = "",
            string locale = "en")
        {
            if (translations != null)
            {
                foreach (var code in new[] { locale, "en" })
                {
                    if (translations.TryGetValue(code, out var copy) &&
                        !string.IsNullOrWhiteSpace(copy?.Name))
                        return copy.Name;
                }
            }
            return fallbackLabel ?? "";
        }

        public static ShopLine ParseShopLine(Dictionary<string, object> o)
        {
            return new ShopLine
            {
                GameCode = FastGameJson.GetString(o, "game_code"),
                SkuKind = FastGameJson.GetString(o, "sku_kind"),
                SkuId = FastGameJson.GetString(o, "sku_id"),
                Label = FastGameJson.GetString(o, "label"),
                Price = FastGameJson.GetInt(o, "price"),
                Owned = FastGameJson.GetBool(o, "owned"),
                Meta = FastGameJson.GetObject(o, "meta") ?? new Dictionary<string, object>(),
            };
        }

        public static Loadout ParseLoadout(Dictionary<string, object> o)
        {
            var loadout = new Loadout
            {
                UserId = FastGameJson.GetString(o, "user_id"),
                GameCode = FastGameJson.GetString(o, "game_code"),
                CharacterId = FastGameJson.GetString(o, "character_id"),
                Level = FastGameJson.GetInt(o, "level"),
                Xp = FastGameJson.GetInt(o, "xp"),
            };
            var cos = FastGameJson.GetObject(o, "equipped_cosmetics");
            if (cos != null)
                foreach (var kv in cos)
                    loadout.EquippedCosmetics[kv.Key] = kv.Value?.ToString();
            var parts = FastGameJson.GetObject(o, "modular_parts");
            if (parts != null)
                foreach (var kv in parts)
                    loadout.ModularParts[kv.Key] = kv.Value?.ToString();
            return loadout;
        }

        public static Advertisement ParseAdvertisement(Dictionary<string, object> o)
        {
            if (o == null) return null;
            var media = FastGameJson.GetObject(o, "media") ?? new Dictionary<string, object>();
            var click = FastGameJson.GetObject(o, "click") ?? new Dictionary<string, object>();
            var tracking = FastGameJson.GetObject(o, "tracking") ?? new Dictionary<string, object>();
            var meta = FastGameJson.GetObject(o, "meta") ?? new Dictionary<string, object>();
            return new Advertisement
            {
                Id = FastGameJson.GetString(o, "id"),
                CampaignId = FastGameJson.GetString(o, "campaign_id"),
                Media = new AdvertisementMedia
                {
                    Type = FastGameJson.GetString(media, "type"),
                    Url = FastGameJson.GetString(media, "url"),
                    Width = FastGameJson.GetInt(media, "width"),
                    Height = FastGameJson.GetInt(media, "height"),
                },
                Click = new AdvertisementClick
                {
                    Enabled = FastGameJson.GetBool(click, "enabled"),
                    Url = FastGameJson.GetString(click, "url"),
                },
                Tracking = new AdvertisementTracking
                {
                    ImpressionUrl = FastGameJson.GetString(tracking, "impression_url"),
                    ClickUrl = FastGameJson.GetString(tracking, "click_url"),
                },
                Meta = meta,
                Title = FastGameJson.GetString(meta, "title"),
                Body = FastGameJson.GetString(meta, "body"),
                BackgroundUrl = FastGameJson.GetString(meta, "background_url"),
                BackgroundColor = FastGameJson.GetString(meta, "background_color"),
            };
        }
    }
}

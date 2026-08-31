using System;
using System.Collections.Generic;

namespace FastGame.Models
{
    /// <summary>Unified row for ITEMS_THUMB / INSPECT_CANVAS across menu, shop, collectibles, user.</summary>
    [Serializable]
    public sealed class FastGameMenuItem
    {
        public string Id;
        public string Code;
        public string Label;
        public string Description;
        public string ImageUrl;
        public bool Locked;
        public bool Owned;
        public bool Purchasable;
        public int Price;

        /// <summary>map | mode | character | shop | achievement | avatar | title | friend | placeholder</summary>
        public string Kind = "placeholder";

        /// <summary>Shop unlock, map id, friend user id, etc.</summary>
        public string SkuKind;
        public string SkuId;

        public Dictionary<string, object> Meta = new Dictionary<string, object>();

        public static FastGameMenuItem FromCollectible(CollectibleDef c, string kind)
        {
            if (c == null) return null;
            return new FastGameMenuItem
            {
                Id = c.Id,
                Code = c.Code,
                Label = c.Label ?? c.Code,
                Description = c.Code,
                ImageUrl = c.ImageUrl,
                Locked = c.Locked,
                Kind = kind,
            };
        }

        public static FastGameMenuItem FromMap(GameMap m)
        {
            if (m == null) return null;
            return new FastGameMenuItem
            {
                Id = m.Id,
                Code = m.MapId,
                Label = m.Label ?? m.MapId,
                Description = m.MapId,
                Purchasable = m.Purchasable,
                Price = m.Price,
                Kind = "map",
                SkuKind = "map",
                SkuId = m.MapId,
            };
        }

        public static FastGameMenuItem FromCharacter(Character c)
        {
            if (c == null) return null;
            return new FastGameMenuItem
            {
                Id = c.Id,
                Code = c.CharacterId,
                Label = c.Label ?? c.CharacterId,
                Description = c.Role ?? c.CharacterId,
                Kind = "character",
                SkuKind = "character",
                SkuId = c.CharacterId,
            };
        }

        public static FastGameMenuItem FromMode(GameMode m)
        {
            if (m == null) return null;
            return new FastGameMenuItem
            {
                Id = m.Id,
                Code = m.ModeId,
                Label = m.ModeId,
                Description = $"{m.Topology} · {m.MinPlayers}-{m.MaxPlayers} players",
                Kind = "mode",
                SkuId = m.ModeId,
            };
        }

        public static FastGameMenuItem FromShopLine(ShopLine line)
        {
            if (line == null) return null;
            return new FastGameMenuItem
            {
                Code = line.SkuId,
                Label = line.Label ?? line.SkuId,
                Description = line.Price > 0 ? line.Price.ToString() : line.SkuKind,
                Owned = line.Owned,
                Price = line.Price,
                Kind = "shop",
                SkuKind = line.SkuKind,
                SkuId = line.SkuId,
                Meta = line.Meta ?? new Dictionary<string, object>(),
            };
        }
    }

    [Serializable]
    public struct FastGameInspectActionSlot
    {
        public string Label;
        public bool Visible;
    }
}

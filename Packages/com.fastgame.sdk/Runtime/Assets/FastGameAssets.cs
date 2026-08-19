using System.Collections.Generic;
using FastGame.Models;

namespace FastGame
{
    public sealed class FastGameAssets
    {
        public List<AssetPack> ListPacksFromGame(GameCatalogDetail detail)
        {
            if (detail?.AssetPacks == null) return new List<AssetPack>();
            return new List<AssetPack>(detail.AssetPacks);
        }

        public List<AssetPack> ListPacksFromRuntime(Dictionary<string, object> mapRuntime)
        {
            var list = new List<AssetPack>();
            var arr = FastGameJson.GetArray(mapRuntime, "asset_packs");
            if (arr == null) return list;
            foreach (var item in arr)
            {
                var o = item as Dictionary<string, object>;
                if (o == null) continue;
                list.Add(FastGameDto.ParsePack(o));
            }
            return list;
        }
    }
}

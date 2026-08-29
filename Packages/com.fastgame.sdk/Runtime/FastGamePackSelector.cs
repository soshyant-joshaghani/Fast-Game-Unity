using System;
using System.Collections.Generic;
using FastGame.Models;

namespace FastGame
{
    public sealed class FastGameDownloadContext
    {
        public string QualityClass = "mobile";
        public string RuntimeOs = "android";
        public string PreferredLanguage = "en";
        public bool SkipSplashPacks = true;
    }

    /// <summary>
    /// Filter published tip pack index for DOWNLOAD (quality × platform × language).
    /// </summary>
    public static class FastGamePackSelector
    {
        public static List<AssetPack> ListForDownload(
            IEnumerable<AssetPack> index,
            FastGameDownloadContext ctx)
        {
            var outList = new List<AssetPack>();
            if (index == null || ctx == null)
                return outList;

            foreach (var pack in index)
            {
                if (pack == null)
                    continue;
                if (ctx.SkipSplashPacks && IsSplashPack(pack))
                    continue;
                if (!MatchesTagList(pack.Quality, ctx.QualityClass))
                    continue;
                if (!MatchesTagList(pack.Platforms, ctx.RuntimeOs))
                    continue;
                if (!MatchesTagList(pack.Languages, ctx.PreferredLanguage))
                    continue;
                outList.Add(pack);
            }
            return outList;
        }

        public static bool MatchesTagList(IReadOnlyList<string> tags, string value)
        {
            if (tags == null || tags.Count == 0)
                return true;
            if (string.IsNullOrWhiteSpace(value))
                return false;
            foreach (var tag in tags)
            {
                if (string.IsNullOrWhiteSpace(tag))
                    continue;
                if (tag == "*" || string.Equals(tag, value, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        static bool IsSplashPack(AssetPack pack)
        {
            var id = pack.PackId ?? "";
            var kind = pack.Kind ?? "";
            return id.Equals("splash", StringComparison.OrdinalIgnoreCase)
                || kind.Equals("splash", StringComparison.OrdinalIgnoreCase);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FastGame.Models;
using UnityEngine;
using UnityEngine.Networking;

namespace FastGame
{
    /// <summary>Resolve pack URLs from tip index or pack tip parts; write to persistent cache.</summary>
    public static class FastGamePackDownload
    {
        public static bool HasDownloadUrl(AssetPack pack) =>
            !string.IsNullOrWhiteSpace(pack?.Url);

        public static string PickPartUrl(Dictionary<string, object> packTip)
        {
            if (packTip == null)
                return null;

            var parts = FastGameJson.GetArray(packTip, "parts");
            if (parts == null || parts.Count == 0)
                return null;

            string zipUrl = null;
            string packUrl = null;
            string anyUrl = null;

            foreach (var item in parts)
            {
                if (item is not Dictionary<string, object> part)
                    continue;
                var status = FastGameJson.GetString(part, "status");
                if (!string.Equals(status, "ready", StringComparison.OrdinalIgnoreCase))
                    continue;
                var url = FastGameJson.GetString(part, "public_url");
                if (string.IsNullOrWhiteSpace(url))
                    continue;
                anyUrl ??= url;
                var partId = FastGameJson.GetString(part, "part_id");
                var kind = FastGameJson.GetString(part, "kind");
                if (string.Equals(partId, "pack", StringComparison.OrdinalIgnoreCase))
                    packUrl = url;
                if (string.Equals(kind, "zip", StringComparison.OrdinalIgnoreCase))
                    zipUrl = url;
            }

            return packUrl ?? zipUrl ?? anyUrl;
        }

        public static async Task<string> ResolveDownloadUrlAsync(
            FastGameContent content,
            string gameCode,
            AssetPack pack)
        {
            if (pack == null)
                return null;
            if (!string.IsNullOrWhiteSpace(pack.Url))
                return pack.Url;

            var tip = await content.GetPackTipAsync(gameCode, pack.PackId);
            return PickPartUrl(tip);
        }

        public static async Task<bool> DownloadToCacheAsync(AssetPack pack, string url)
        {
            if (pack == null || string.IsNullOrWhiteSpace(url))
                return false;

            var dir = Path.Combine(
                Application.persistentDataPath,
                FastGameLocalData.DownloadCacheFolder,
                "packs",
                SanitizeFileName(pack.PackId ?? pack.Id ?? "pack"));
            Directory.CreateDirectory(dir);

            var fileName = SanitizeFileName(
                !string.IsNullOrWhiteSpace(pack.Hash)
                    ? pack.Hash
                    : $"rev_{Math.Max(1, pack.Revision)}");
            var path = Path.Combine(dir, fileName);

            if (File.Exists(path))
                return true;

            using var req = UnityWebRequest.Get(url);
            req.downloadHandler = new DownloadHandlerBuffer();
            var op = req.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

#if UNITY_2020_2_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
                throw new InvalidOperationException(req.error ?? "download failed");

            File.WriteAllBytes(path, req.downloadHandler.data);
            return true;
        }

        static string SanitizeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "file";
            foreach (var c in Path.GetInvalidFileNameChars())
                value = value.Replace(c, '_');
            return value;
        }
    }
}

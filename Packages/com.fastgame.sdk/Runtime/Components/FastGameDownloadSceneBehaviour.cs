using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FastGame.Models;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace FastGame
{
    /// <summary>
    /// DOWNLOAD — fetches game packs from tip JSON, downloads to persistent storage, then loads
    /// <see cref="FastGameSceneFlowBehaviour.NextScene"/> (default MENU).
    /// </summary>
    [AddComponentMenu("Fast Game/Scenes/Download")]
    public sealed class FastGameDownloadSceneBehaviour : FastGameSceneFlowBehaviour
    {
        [Header("Download")]
        [Tooltip("Start pack fetch/download automatically on scene enter.")]
        public bool AutoStart = true;

        [Tooltip("When no packs or API unavailable, advance immediately.")]
        public bool AdvanceWhenNothingToDownload = true;

        [Tooltip("Skip splash packs (already handled on SPLASH scene).")]
        public bool SkipSplashPacks = true;

        [Header("UI (optional)")]
        public Slider ProgressSlider;
        public Component StatusLabel;

        [Header("Timing")]
        [Tooltip("Minimum seconds to show download UI before advancing (0 = immediate after work).")]
        public float MinDisplaySeconds;

        float _startedAt;
        bool _completed;

        void Awake()
        {
            if (string.IsNullOrWhiteSpace(NextScene)
                || NextScene == FastGameSceneNames.Language
                || NextScene == FastGameSceneNames.Splash)
                NextScene = FastGameSceneNames.Menu;
        }

        async void Start()
        {
            _startedAt = Time.unscaledTime;
            if (!AutoStart)
                return;
            await RunDownloadAsync();
        }

        public async Task RunDownloadAsync()
        {
            SetProgress(0f, "Preparing…");

            var host = FastGameClientBehaviour.Instance;
            if (host?.Client == null)
            {
                if (AdvanceWhenNothingToDownload)
                    await FinishAsync();
                else
                    SetProgress(0f, "Client not ready");
                return;
            }

            List<AssetPack> packs;
            try
            {
                var game = await host.Client.Content.GetGameConfigAsync(host.GameCode);
                packs = FilterPacks(new FastGameAssets().ListPacksFromRuntime(game));
            }
            catch (Exception e)
            {
                Debug.LogWarning("FastGame download: " + e.Message);
                if (AdvanceWhenNothingToDownload)
                {
                    await FinishAsync();
                    return;
                }
                SetProgress(0f, "Download failed");
                return;
            }

            if (packs.Count == 0)
            {
                SetProgress(1f, "Ready");
                await FinishAsync();
                return;
            }

            for (var i = 0; i < packs.Count; i++)
            {
                var pack = packs[i];
                var label = string.IsNullOrWhiteSpace(pack.Label) ? pack.PackId : pack.Label;
                SetProgress((float)i / packs.Count, $"Downloading {label}…");
                try
                {
                    await DownloadPackAsync(pack);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"FastGame download pack {pack.PackId}: {e.Message}");
                }
                SetProgress((float)(i + 1) / packs.Count, $"Downloaded {label}");
            }

            SetProgress(1f, "Complete");
            await FinishAsync();
        }

        List<AssetPack> FilterPacks(List<AssetPack> packs)
        {
            var outList = new List<AssetPack>();
            foreach (var pack in packs)
            {
                if (pack == null || string.IsNullOrWhiteSpace(pack.Url))
                    continue;
                if (SkipSplashPacks && IsSplashPack(pack))
                    continue;
                outList.Add(pack);
            }
            return outList;
        }

        static bool IsSplashPack(AssetPack pack)
        {
            var id = pack.PackId ?? "";
            var label = pack.Label ?? "";
            return id.Equals("splash", StringComparison.OrdinalIgnoreCase)
                || label.Equals("splash", StringComparison.OrdinalIgnoreCase);
        }

        static async Task DownloadPackAsync(AssetPack pack)
        {
            var dir = Path.Combine(
                Application.persistentDataPath,
                "fastgame",
                "packs",
                SanitizeFileName(pack.PackId ?? pack.Id ?? "pack"));
            Directory.CreateDirectory(dir);

            var fileName = SanitizeFileName(
                string.IsNullOrWhiteSpace(pack.Hash)
                    ? pack.Version ?? "data"
                    : pack.Hash);
            var path = Path.Combine(dir, fileName);

            if (File.Exists(path))
                return;

            using var req = UnityWebRequest.Get(pack.Url);
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
            await Task.CompletedTask;
        }

        static string SanitizeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "file";
            foreach (var c in Path.GetInvalidFileNameChars())
                value = value.Replace(c, '_');
            return value;
        }

        void SetProgress(float normalized, string message)
        {
            if (ProgressSlider != null)
                ProgressSlider.normalizedValue = Mathf.Clamp01(normalized);
            if (StatusLabel != null)
                FastGameUiText.WriteLabel(StatusLabel, message ?? "");
        }

        async Task FinishAsync()
        {
            if (_completed)
                return;

            var wait = MinDisplaySeconds - (Time.unscaledTime - _startedAt);
            if (wait > 0f)
            {
                var ms = (int)(wait * 1000f);
                await Task.Delay(ms);
            }

            _completed = true;
            CompleteScene();
        }
    }
}

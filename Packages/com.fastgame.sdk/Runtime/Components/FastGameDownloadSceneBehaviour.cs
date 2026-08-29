using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FastGame.Models;
using UnityEngine;
using UnityEngine.UI;

namespace FastGame
{
    /// <summary>
    /// DOWNLOAD — fetches published tip pack index, filters quality × platform × language,
    /// downloads to persistent storage, then loads <see cref="FastGameSceneFlowBehaviour.NextScene"/>.
    /// </summary>
    [AddComponentMenu("Fast Game/Scenes/Download")]
    public sealed class FastGameDownloadSceneBehaviour : FastGameSceneFlowBehaviour
    {
        [Header("Download")]
        [Tooltip("Start pack fetch/download automatically on scene enter.")]
        public bool AutoStart = true;

        [Tooltip("When no matching packs or tip unpublished, advance immediately.")]
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

            Dictionary<string, object> gameTip;
            try
            {
                gameTip = await host.Client.Content.GetGameConfigAsync(host.GameCode);
            }
            catch (FastGameException e)
            {
                if (IsTipNotPublished(e))
                {
                    Debug.LogWarning(
                        "FastGame download: tip not published — publish tip in panel before DOWNLOAD.");
                    SetProgress(0f, "Tip not published — Publish tip in panel");
                }
                else
                {
                    Debug.LogWarning("FastGame download: " + e.Message);
                    SetProgress(0f, "Download failed");
                }

                if (AdvanceWhenNothingToDownload)
                    await FinishAsync();
                return;
            }
            catch (Exception e)
            {
                Debug.LogWarning("FastGame download: " + e.Message);
                if (AdvanceWhenNothingToDownload)
                    await FinishAsync();
                else
                    SetProgress(0f, "Download failed");
                return;
            }

            var allPacks = new FastGameAssets().ListPacksFromRuntime(gameTip);
            var ctx = BuildDownloadContext(host);
            ctx.SkipSplashPacks = SkipSplashPacks;
            var packs = FastGamePackSelector.ListForDownload(allPacks, ctx);

            if (packs.Count == 0)
            {
                SetProgress(1f, "No packs for this device / language");
                await FinishAsync();
                return;
            }

            var downloadable = new List<AssetPack>();
            foreach (var pack in packs)
            {
                if (FastGamePackDownload.HasDownloadUrl(pack))
                {
                    downloadable.Add(pack);
                    continue;
                }

                try
                {
                    var url = await FastGamePackDownload.ResolveDownloadUrlAsync(
                        host.Client.Content, host.GameCode, pack);
                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        pack.Url = url;
                        downloadable.Add(pack);
                    }
                    else
                        Debug.LogWarning($"FastGame download: no URL for pack {pack.PackId}");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"FastGame download: resolve {pack.PackId}: {e.Message}");
                }
            }

            if (downloadable.Count == 0)
            {
                SetProgress(1f, "No downloadable packs");
                await FinishAsync();
                return;
            }

            for (var i = 0; i < downloadable.Count; i++)
            {
                var pack = downloadable[i];
                var label = string.IsNullOrWhiteSpace(pack.Label) ? pack.PackId : pack.Label;
                SetProgress((float)i / downloadable.Count, $"Downloading {label}…");
                try
                {
                    var url = pack.Url;
                    if (string.IsNullOrWhiteSpace(url))
                        url = await FastGamePackDownload.ResolveDownloadUrlAsync(
                            host.Client.Content, host.GameCode, pack);
                    await FastGamePackDownload.DownloadToCacheAsync(pack, url);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"FastGame download pack {pack.PackId}: {e.Message}");
                }
                SetProgress((float)(i + 1) / downloadable.Count, $"Downloaded {label}");
            }

            SetProgress(1f, "Complete");
            await FinishAsync();
        }

        static FastGameDownloadContext BuildDownloadContext(FastGameClientBehaviour host)
        {
            var os = FastGameRuntimePlatform.GetRuntimeOs();
            var storeOs = FastGameRuntimePlatform.StorePlatformToOs(host?.StorePlatform);
            if (!string.IsNullOrWhiteSpace(storeOs)
                && !string.Equals(os, storeOs, StringComparison.OrdinalIgnoreCase)
                && Application.isEditor)
            {
                os = storeOs;
            }

            return new FastGameDownloadContext
            {
                QualityClass = FastGameRuntimePlatform.GetQualityClass(os),
                RuntimeOs = os,
                PreferredLanguage = FastGameLocalePrefs.Get("en"),
            };
        }

        static bool IsTipNotPublished(FastGameException e)
        {
            var msg = e.Message ?? "";
            return msg.Contains("404", StringComparison.Ordinal)
                || msg.Contains("Tip not published", StringComparison.OrdinalIgnoreCase);
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

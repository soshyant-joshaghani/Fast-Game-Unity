using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace FastGame
{
    public enum FastGameSplashLocalPriority
    {
        PreferVideo,
        PreferImage,
        ImageOnly,
        VideoOnly
    }

    /// <summary>
    /// Main splash controller — place on <b>SPLASH_CANVAS</b>.
    /// Wires splash views and loads <see cref="NextScene"/> when done.
    /// </summary>
    [AddComponentMenu("Fast Game/Splash")]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-50)]
    public sealed class FastGameSplashBehaviour : MonoBehaviour
    {
        [Header("Splash views")]
        [Tooltip("SPLASH_BG child with FastGameSplashBackgroundView (optional).")]
        public FastGameSplashBackgroundView SplashBackground;
        [Tooltip("SPLASH_IMAGE child with FastGameSplashImageView.")]
        public FastGameSplashImageView SplashImage;
        [Tooltip("SPLASH_VIDEO child with FastGameSplashVideoView (optional, local only).")]
        public FastGameSplashVideoView SplashVideo;

        [Header("Source")]
        [Tooltip("When true, fetch splash image from fast-game (online = image only, no video).")]
        public bool FetchOnline;

        [Header("Local priority (when Fetch Online is off)")]
        public FastGameSplashLocalPriority LocalPriority = FastGameSplashLocalPriority.PreferVideo;

        [Header("Timing")]
        [Tooltip("Seconds to show image splash (local sprite or online download). Not used for video.")]
        public float ImageDisplaySeconds = 2f;

        [Tooltip("Used only when video module/clip is missing but video was requested.")]
        public float VideoFallbackSeconds = 30f;

        [Header("Next scene")]
        [Tooltip("Scene to load when splash finishes.")]
        public string NextScene = FastGameSceneNames.Language;

        [Tooltip("Load NextScene automatically when splash finishes.")]
        public bool AutoLoadNextOnComplete = true;

        public FastGameSceneCompleteEvent OnSceneComplete;

        bool _advanced;

        void Awake()
        {
            ResolveReferences();
            ShowBackground();
            HideAllViews();
        }

        async void Start()
        {
            ResolveReferences();
            ShowBackground();
            HideAllViews();

            if (FetchOnline)
            {
                if (await TryFetchOnlineAsync())
                {
                    ScheduleImageAdvance();
                    return;
                }
            }

            if (TryShowLocal(out var mode))
            {
                if (mode == SplashDisplayMode.Video)
                {
                    if (VideoFallbackSeconds > 0f)
                        Invoke(nameof(Advance), VideoFallbackSeconds);
                    return;
                }
                ScheduleImageAdvance();
                return;
            }

            Advance();
        }

        void ResolveReferences()
        {
            if (SplashBackground == null)
                SplashBackground = GetComponentInChildren<FastGameSplashBackgroundView>(true);
            if (SplashImage == null)
                SplashImage = GetComponentInChildren<FastGameSplashImageView>(true);
            if (SplashVideo == null)
                SplashVideo = GetComponentInChildren<FastGameSplashVideoView>(true);
        }

        void ShowBackground()
        {
            SplashBackground?.Show();
        }

        void HideAllViews()
        {
            SplashImage?.Hide();
            SplashVideo?.Hide();
        }

        bool TryShowLocal(out SplashDisplayMode mode)
        {
            mode = SplashDisplayMode.None;

            switch (LocalPriority)
            {
                case FastGameSplashLocalPriority.ImageOnly:
                    return TryShowImage(ref mode) || false;
                case FastGameSplashLocalPriority.VideoOnly:
                    return TryShowVideo(ref mode) || false;
                case FastGameSplashLocalPriority.PreferImage:
                    if (TryShowImage(ref mode))
                        return true;
                    return TryShowVideo(ref mode);
                default:
                    if (TryShowVideo(ref mode))
                        return true;
                    return TryShowImage(ref mode);
            }
        }

        bool TryShowImage(ref SplashDisplayMode mode)
        {
            if (SplashImage == null || !SplashImage.HasLocalContent)
                return false;
            if (!SplashImage.ShowLocal())
                return false;
            mode = SplashDisplayMode.Image;
            return true;
        }

        bool TryShowVideo(ref SplashDisplayMode mode)
        {
            if (SplashVideo == null)
                return false;
            if (SplashVideo.TryShowLocal(OnVideoFinished))
            {
                mode = SplashDisplayMode.Video;
                return true;
            }
            return false;
        }

        void OnVideoFinished()
        {
            Advance();
        }

        void ScheduleImageAdvance()
        {
            if (ImageDisplaySeconds <= 0f)
                Advance();
            else
                Invoke(nameof(Advance), ImageDisplaySeconds);
        }

        async Task<bool> TryFetchOnlineAsync()
        {
            var host = FastGameClientBehaviour.Instance;
            if (host?.Client == null || SplashImage == null)
                return false;

            string splashUrl = null;
            try
            {
                var bootstrap = await host.Client.Content.GetBootstrapAsync(host.GameCode);
                splashUrl = ReadSplashUrl(bootstrap);
                if (string.IsNullOrWhiteSpace(splashUrl))
                {
                    var game = await host.Client.Content.GetGameConfigAsync(host.GameCode);
                    splashUrl = ReadSplashUrlFromPacks(game);
                }
            }
            catch
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(splashUrl) || !IsImageUrl(splashUrl))
                return false;

            return await LoadImageUrlAsync(splashUrl);
        }

        async Task<bool> LoadImageUrlAsync(string url)
        {
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
                return false;

            return SplashImage != null && SplashImage.ShowFromBytes(req.downloadHandler.data);
        }

        void Advance()
        {
            if (_advanced)
                return;
            _advanced = true;
            CancelInvoke(nameof(Advance));

            OnSceneComplete?.Invoke();
            if (AutoLoadNextOnComplete && !string.IsNullOrWhiteSpace(NextScene))
                SceneManager.LoadScene(NextScene);
        }

        enum SplashDisplayMode
        {
            None,
            Image,
            Video
        }

        static bool IsImageUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;
            var u = url.Split('?')[0];
            return u.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                || u.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                || u.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                || u.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)
                || u.EndsWith(".gif", StringComparison.OrdinalIgnoreCase);
        }

        static string ReadSplashUrl(Dictionary<string, object> payload)
        {
            if (payload == null) return null;
            var direct = FastGameJson.GetString(payload, "splash_url");
            if (!string.IsNullOrWhiteSpace(direct))
                return direct;
            return ReadSplashUrlFromPacks(payload);
        }

        static string ReadSplashUrlFromPacks(Dictionary<string, object> payload)
        {
            if (!payload.TryGetValue("asset_packs", out var raw) || raw is not List<object> packs)
                return null;
            foreach (var item in packs)
            {
                if (item is not Dictionary<string, object> pack)
                    continue;
                var packId = FastGameJson.GetString(pack, "pack_id");
                var kind = FastGameJson.GetString(pack, "kind");
                if (!string.Equals(packId, "splash", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(kind, "splash", StringComparison.OrdinalIgnoreCase))
                    continue;
                var packUrl = FastGameJson.GetString(pack, "url");
                if (!string.IsNullOrWhiteSpace(packUrl))
                    return packUrl;
            }
            return null;
        }
    }
}

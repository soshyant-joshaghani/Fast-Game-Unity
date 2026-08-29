using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace FastGame.Editor
{
    /// <summary>Editor utilities to reset Fast Game PlayerPrefs, download cache, and inspect tip publish state.</summary>
    public sealed class FastGameDevToolsWindow : EditorWindow
    {
        Vector2 _scroll;
        string _status = "";
        string _apiBaseUrl = "http://api.localhost/api/v1";
        string _gameCode = "game";
        string _tipStatus = "Tip: not checked yet.";
        bool _tipBusy;

        [MenuItem("Fast Game/Dev Tools…", false, 0)]
        public static void Open()
        {
            var window = GetWindow<FastGameDevToolsWindow>(false, "Fast Game Dev", true);
            window.minSize = new Vector2(360f, 360f);
            window.SyncFromSceneClient();
            window.RefreshStatus();
        }

        [MenuItem("Fast Game/Clear All Local Data", false, 100)]
        static void MenuClearAll()
        {
            if (!EditorUtility.DisplayDialog(
                    "Fast Game",
                    "Clear auth session, saved Enter ID, language, pending payment, and downloaded packs?",
                    "Clear",
                    "Cancel"))
                return;
            FastGameLocalData.ClearAll();
            Debug.Log("Fast Game: cleared all local data.");
        }

        [MenuItem("Fast Game/Clear Auth Session", false, 101)]
        static void MenuClearAuth()
        {
            FastGameLocalData.ClearAuthSession();
            Debug.Log("Fast Game: cleared auth session (token, Enter ID, pending payment).");
        }

        [MenuItem("Fast Game/Clear Download Cache", false, 102)]
        static void MenuClearDownloadCache()
        {
            var cleared = FastGameLocalData.ClearDownloadCache();
            Debug.Log(cleared
                ? "Fast Game: cleared download cache."
                : "Fast Game: no download cache found.");
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Published tip (player contract)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Draft asset-packs in the panel are editor-only. Unity DOWNLOAD uses "
                + "GET /apps/games/tip/{game}/game — 404 until you Publish tip on the game config page.",
                MessageType.Info);

            SyncFromSceneClient();

            using (new EditorGUILayout.HorizontalScope())
            {
                _apiBaseUrl = EditorGUILayout.TextField("Api Base Url", _apiBaseUrl);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                _gameCode = EditorGUILayout.TextField("Game Code", _gameCode);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginDisabledGroup(_tipBusy);
                if (GUILayout.Button(_tipBusy ? "Checking…" : "Check tip published"))
                    CheckTipPublished();
                if (GUILayout.Button("Copy publish curl"))
                    CopyPublishCurl();
                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.HelpBox(_tipStatus, MessageType.None);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Local data", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Use this when testing login, language, or downloads. "
                + "Works in and out of Play mode.",
                MessageType.Info);

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
            EditorGUILayout.TextArea(_status, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(6f);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh"))
                    RefreshStatus();
                if (GUILayout.Button("Clear Auth"))
                    Run(() => FastGameLocalData.ClearAuthSession(), "cleared auth session");
                if (GUILayout.Button("Clear Enter ID"))
                    Run(() => FastGameLocalData.ClearEnteredIdentity(), "cleared Enter ID");
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Clear Language"))
                    Run(() => FastGameLocalePrefs.Clear(), "cleared preferred language");
                if (GUILayout.Button("Clear Download Cache"))
                    Run(() =>
                    {
                        var ok = FastGameLocalData.ClearDownloadCache();
                        Debug.Log(ok
                            ? "Fast Game: cleared download cache."
                            : "Fast Game: no download cache found.");
                    }, null);
                if (GUILayout.Button("Clear All"))
                {
                    if (EditorUtility.DisplayDialog(
                            "Fast Game",
                            "Clear auth, language, and download cache?",
                            "Clear",
                            "Cancel"))
                        Run(() => FastGameLocalData.ClearAll(), "cleared all local data");
                }
            }
        }

        void SyncFromSceneClient()
        {
            var client = FindObjectOfType<FastGameClientBehaviour>();
            if (client == null)
                return;
            if (!string.IsNullOrWhiteSpace(client.ApiBaseUrl))
                _apiBaseUrl = client.ApiBaseUrl;
            if (!string.IsNullOrWhiteSpace(client.GameCode))
                _gameCode = client.GameCode;
        }

        void CheckTipPublished()
        {
            _tipBusy = true;
            _tipStatus = "Tip: checking…";
            Repaint();

            try
            {
                var baseUrl = FastGameConfig.NormalizeApiBaseUrl(_apiBaseUrl).TrimEnd('/');
                var game = (_gameCode ?? "").Trim();
                if (string.IsNullOrEmpty(game))
                {
                    _tipStatus = "Tip: enter Game Code.";
                    return;
                }

                var url = $"{baseUrl}/apps/games/tip/{Uri.EscapeDataString(game)}/bootstrap";
                using var req = UnityWebRequest.Get(url);
                req.SendWebRequest();
                while (!req.isDone)
                { }

#if UNITY_2020_2_OR_NEWER
                if (req.result != UnityWebRequest.Result.Success)
#else
                if (req.isNetworkError || req.isHttpError)
#endif
                {
                    _tipStatus = $"Tip: request failed ({(int)req.responseCode}) — {req.downloadHandler?.text ?? req.error}";
                    return;
                }

                var json = req.downloadHandler?.text ?? "{}";
                var boot = FastGameJson.ParseObject(json);
                var published = boot != null && FastGameJson.GetBool(boot, "published");
                var version = FastGameJson.GetInt(boot, "tip_version");
                _tipStatus = published
                    ? $"Tip: published (version {version}). DOWNLOAD GetGameConfig should return 200."
                    : "Tip: NOT published — panel → game config → Publish tip before Unity DOWNLOAD works.";
            }
            catch (Exception e)
            {
                _tipStatus = "Tip: " + e.Message;
            }
            finally
            {
                _tipBusy = false;
                Repaint();
            }
        }

        void CopyPublishCurl()
        {
            var baseUrl = FastGameConfig.NormalizeApiBaseUrl(_apiBaseUrl).TrimEnd('/');
            var game = (_gameCode ?? "game").Trim();
            var curl =
                $"curl -X POST \"{baseUrl}/apps/games/tip/{game}/admin/publish\" "
                + "-H \"Authorization: Bearer <panel_token>\"";
            EditorGUIUtility.systemCopyBuffer = curl;
            Debug.Log("Fast Game: copied publish curl to clipboard.");
        }

        void Run(Action action, string logMessage)
        {
            action();
            if (!string.IsNullOrEmpty(logMessage))
                Debug.Log("Fast Game: " + logMessage);
            RefreshStatus();
        }

        void RefreshStatus()
        {
            _status = FastGameLocalData.Describe();
            Repaint();
        }
    }
}

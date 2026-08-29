using UnityEditor;
using UnityEngine;

namespace FastGame.Editor
{
    /// <summary>Editor utilities to reset Fast Game PlayerPrefs and download cache.</summary>
    public sealed class FastGameDevToolsWindow : EditorWindow
    {
        Vector2 _scroll;
        string _status = "";

        [MenuItem("Fast Game/Dev Tools…", false, 0)]
        public static void Open()
        {
            var window = GetWindow<FastGameDevToolsWindow>(false, "Fast Game Dev", true);
            window.minSize = new Vector2(360f, 280f);
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

        void Run(System.Action action, string logMessage)
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

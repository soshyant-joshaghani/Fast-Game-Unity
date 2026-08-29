using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace FastGame
{
    /// <summary>
    /// LANGUAGE — builds a button per supported language from GetBootstrap (defaults to en).
    /// </summary>
    [AddComponentMenu("Fast Game/Scenes/Language")]
    public sealed class FastGameLanguageSceneBehaviour : FastGameSceneFlowBehaviour
    {
        [Header("UI")]
        [Tooltip("Parent for instantiated language buttons (e.g. Scroll View / Content).")]
        public Transform ButtonContainer;
        [Tooltip("Optional prefab with Button + Text/TMP label child.")]
        public GameObject LanguageButtonPrefab;
        [Tooltip("Fallback when bootstrap unavailable.")]
        public string[] FallbackLanguages = { "en" };

        [Header("Bootstrap")]
        public bool FetchLanguagesFromBackend = true;

        [Header("Selection")]
        [Tooltip("When true, tapping a language saves it and loads the next scene.")]
        public bool AdvanceOnSelect = true;

        [Header("Authenticated")]
        [Tooltip("When logged in, skip AUTH and go straight to download (or Authenticated Next Scene).")]
        public bool SkipAuthWhenAuthenticated = true;

        [Tooltip("Scene after language when Skip Auth When Authenticated and session exists.")]
        public string AuthenticatedNextScene = FastGameSceneNames.Download;

        [Tooltip("On scene enter, immediately advance when already logged in (skip language UI).")]
        public bool AutoAdvanceWhenAuthenticated = true;

        string _selected = "en";
        bool _advanced;
        readonly Dictionary<string, Button> _buttons = new Dictionary<string, Button>(StringComparer.OrdinalIgnoreCase);

        void Awake()
        {
            if (string.IsNullOrWhiteSpace(NextScene) || NextScene == FastGameSceneNames.Language)
                NextScene = FastGameSceneNames.Auth;
            _selected = FastGameLocalePrefs.Get("en");
        }

        async void Start()
        {
            if (AutoAdvanceWhenAuthenticated && SkipAuthWhenAuthenticated && IsAuthenticated())
            {
                AdvanceFromLanguage();
                return;
            }

            var langs = await LoadLanguagesAsync();
            BuildButtons(langs);
        }

        async Task<string[]> LoadLanguagesAsync()
        {
            if (!FetchLanguagesFromBackend)
                return FallbackLanguages;

            var host = FastGameClientBehaviour.Instance;
            if (host?.Client == null)
                return FallbackLanguages;

            try
            {
                var bootstrap = await host.Client.Content.GetBootstrapAsync(host.GameCode);
                if (bootstrap.TryGetValue("supported_languages", out var raw)
                    && raw is List<object> list
                    && list.Count > 0)
                {
                    var outList = new List<string>();
                    foreach (var item in list)
                    {
                        var code = item?.ToString()?.Trim().ToLowerInvariant();
                        if (!string.IsNullOrEmpty(code))
                            outList.Add(code);
                    }
                    if (outList.Count > 0)
                    {
                        var def = FastGameJson.GetString(bootstrap, "default_language");
                        if (!string.IsNullOrWhiteSpace(def))
                            _selected = def.Trim().ToLowerInvariant();
                        return outList.ToArray();
                    }
                }
            }
            catch
            {
                // unpublished tip — fall back
            }

            return FallbackLanguages;
        }

        void BuildButtons(IReadOnlyList<string> languages)
        {
            _buttons.Clear();

            if (ButtonContainer == null || LanguageButtonPrefab == null)
                return;

            for (var i = ButtonContainer.childCount - 1; i >= 0; i--)
                Destroy(ButtonContainer.GetChild(i).gameObject);

            foreach (var lang in languages)
            {
                var code = lang.Trim().ToLowerInvariant();
                if (string.IsNullOrEmpty(code))
                    continue;
                var go = Instantiate(LanguageButtonPrefab, ButtonContainer);
                go.name = $"Lang_{code}";
                SetButtonLabel(go, LanguageLabel(code));
                DisableLabelRaycasts(go);

                var button = go.GetComponent<Button>() ?? go.GetComponentInChildren<Button>(true);
                if (button != null)
                {
                    _buttons[code] = button;
                    button.onClick.AddListener(() => OnLanguageButtonClicked(code));
                }
            }

            HighlightSelected();
        }

        void OnLanguageButtonClicked(string languageCode)
        {
            SelectLanguage(languageCode);
            HighlightSelected();
            if (AdvanceOnSelect)
                Continue();
        }

        static void SetButtonLabel(GameObject root, string text)
        {
            var label = root.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.text = text;
                return;
            }

            foreach (var c in root.GetComponentsInChildren<Component>(true))
            {
                if (c == null || c is Transform)
                    continue;
                var typeName = c.GetType().Name;
                if (typeName is "TextMeshProUGUI" or "TextMeshPro" or "TMP_Text")
                {
                    FastGameUiText.WriteLabel(c, text);
                    return;
                }
            }
        }

        static void DisableLabelRaycasts(GameObject root)
        {
            foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
            {
                if (graphic is Image img && img.GetComponent<Button>() != null)
                    continue;
                graphic.raycastTarget = false;
            }
        }

        void HighlightSelected()
        {
            foreach (var pair in _buttons)
            {
                if (pair.Value == null)
                    continue;
                var colors = pair.Value.colors;
                var selected = string.Equals(pair.Key, _selected, StringComparison.OrdinalIgnoreCase);
                colors.normalColor = selected
                    ? new Color(0.65f, 0.95f, 1f, 1f)
                    : Color.white;
                pair.Value.colors = colors;
            }
        }

        static string LanguageLabel(string code) => code switch
        {
            "en" => "English",
            "fa" => "فارسی",
            "ar" => "العربية",
            _ => code.ToUpperInvariant()
        };

        public void SelectLanguage(string languageCode)
        {
            _selected = languageCode.Trim().ToLowerInvariant();
            FastGameLocalePrefs.Set(_selected);
        }

        /// <summary>Wire Continue button when AdvanceOnSelect is false.</summary>
        public void Continue()
        {
            AdvanceFromLanguage();
        }

        void AdvanceFromLanguage()
        {
            if (_advanced)
                return;
            _advanced = true;

            FastGameLocalePrefs.Set(_selected);
            var next = ResolveNextScene();
            if (AutoLoadNextOnComplete && !string.IsNullOrWhiteSpace(next))
                LoadScene(next);
            else
                OnSceneComplete?.Invoke();
        }

        string ResolveNextScene()
        {
            if (SkipAuthWhenAuthenticated && IsAuthenticated())
                return string.IsNullOrWhiteSpace(AuthenticatedNextScene)
                    ? FastGameSceneNames.Download
                    : AuthenticatedNextScene;
            return NextScene;
        }

        static bool IsAuthenticated()
        {
            var auth = FastGameClientBehaviour.Instance?.Client?.Auth;
            return auth != null && auth.IsAuthenticated;
        }
    }
}

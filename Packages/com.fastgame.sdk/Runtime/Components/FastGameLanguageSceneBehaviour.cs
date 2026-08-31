using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using FastGame.Models;
using UnityEngine;
using UnityEngine.UI;

namespace FastGame
{
    /// <summary>
    /// LANGUAGE — one button per supported language from GetBootstrap (defaults en / fa / ar).
    /// Uses <see cref="FastGameItemsScrollView"/> (ITEMS_SCRLVIEW_H) when assigned.
    /// </summary>
    [AddComponentMenu("Fast Game/Scenes/Language")]
    public sealed class FastGameLanguageSceneBehaviour : FastGameSceneFlowBehaviour
    {
        static readonly string[] DefaultLanguages = { "en", "fa", "ar" };

        [Header("UI")]
        [Tooltip("ITEMS_SCRLVIEW_H on this scene — preferred.")]
        public FastGameItemsScrollView LanguageScroll;

        [Tooltip("Legacy: Content transform when Language Scroll is not assigned.")]
        public Transform ButtonContainer;

        [Tooltip("Legacy button prefab when Language Scroll is not assigned.")]
        public GameObject LanguageButtonPrefab;

        [Tooltip("Used when bootstrap / tip is unavailable.")]
        public string[] FallbackLanguages = DefaultLanguages;

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
            BuildLanguageList(langs);
        }

        async Task<string[]> LoadLanguagesAsync()
        {
            if (!FetchLanguagesFromBackend)
                return NormalizeLanguageList(FallbackLanguages);

            var host = FastGameClientBehaviour.Instance;
            if (host?.Client == null)
                return NormalizeLanguageList(FallbackLanguages);

            try
            {
                var bootstrap = await host.Client.Content.GetBootstrapAsync(host.GameCode);
                var fromBootstrap = ParseSupportedLanguages(bootstrap);
                if (fromBootstrap.Count > 0)
                {
                    var def = FastGameJson.GetString(bootstrap, "default_language");
                    if (!string.IsNullOrWhiteSpace(def))
                        _selected = def.Trim().ToLowerInvariant();
                    return fromBootstrap.ToArray();
                }
            }
            catch
            {
                // unpublished tip — fall back
            }

            return NormalizeLanguageList(FallbackLanguages);
        }

        static List<string> ParseSupportedLanguages(Dictionary<string, object> bootstrap)
        {
            var list = new List<string>();
            if (bootstrap == null)
                return list;

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            void Add(string code)
            {
                code = code?.Trim().ToLowerInvariant();
                if (string.IsNullOrEmpty(code) || !seen.Add(code))
                    return;
                list.Add(code);
            }

            foreach (var item in FastGameJson.GetArray(bootstrap, "supported_languages") ?? new List<object>())
                Add(item?.ToString());

            if (list.Count == 0
                && bootstrap.TryGetValue("supported_languages", out var raw)
                && raw is List<object> legacy)
            {
                foreach (var item in legacy)
                    Add(item?.ToString());
            }

            return list;
        }

        static string[] NormalizeLanguageList(IReadOnlyList<string> codes)
        {
            var list = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (codes != null)
            {
                foreach (var code in codes)
                {
                    var norm = code?.Trim().ToLowerInvariant();
                    if (string.IsNullOrEmpty(norm) || !seen.Add(norm))
                        continue;
                    list.Add(norm);
                }
            }

            if (list.Count == 0)
            {
                foreach (var code in DefaultLanguages)
                {
                    if (seen.Add(code))
                        list.Add(code);
                }
            }

            return list.ToArray();
        }

        void BuildLanguageList(IReadOnlyList<string> languages)
        {
            _buttons.Clear();
            var items = new List<FastGameMenuItem>();
            foreach (var lang in languages)
            {
                var code = lang?.Trim().ToLowerInvariant();
                if (string.IsNullOrEmpty(code))
                    continue;
                items.Add(new FastGameMenuItem
                {
                    Code = code,
                    Label = LanguageLabel(code),
                    Kind = "language",
                });
            }

            if (LanguageScroll != null)
            {
                LanguageScroll.OnItemClicked.RemoveAllListeners();
                LanguageScroll.OnItemClicked.AddListener(OnLanguageItemClicked);
                LanguageScroll.SetItems(items);
                MapButtonsFromScroll(LanguageScroll);
            }
            else
                BuildLegacyButtons(items);

            if (!_buttons.ContainsKey(_selected) && items.Count > 0)
                _selected = items[0].Code;

            HighlightSelected();
        }

        void BuildLegacyButtons(IReadOnlyList<FastGameMenuItem> items)
        {
            if (ButtonContainer == null || LanguageButtonPrefab == null)
            {
                Debug.LogWarning("[FastGame Language] Assign Language Scroll or Button Container + prefab.", this);
                return;
            }

            for (var i = ButtonContainer.childCount - 1; i >= 0; i--)
                Destroy(ButtonContainer.GetChild(i).gameObject);

            var index = 0;
            foreach (var item in items)
            {
                if (item == null || string.IsNullOrEmpty(item.Code))
                    continue;

                var go = Instantiate(LanguageButtonPrefab, ButtonContainer);
                go.name = item.Code;
                var thumb = go.GetComponent<FastGameItemsThumbView>() ?? go.AddComponent<FastGameItemsThumbView>();
                thumb.LayoutThumb(FastGameScrollLayout.Horizontal, index);
                thumb.Bind(item, OnLanguageItemClicked);
                RegisterButton(item.Code, go);
                index++;
            }

            ResizeLegacyContent(index);
        }

        void ResizeLegacyContent(int count)
        {
            if (ButtonContainer is not RectTransform rt || count <= 0)
                return;
            const float thumbWidth = 128f;
            const float spacing = 8f;
            rt.sizeDelta = new Vector2(count * (thumbWidth + spacing), rt.sizeDelta.y);
        }

        void MapButtonsFromScroll(FastGameItemsScrollView scroll)
        {
            foreach (var row in scroll.Rows)
            {
                if (row == null)
                    continue;
                var code = row.gameObject.name;
                if (row.ClickButton != null)
                    _buttons[code] = row.ClickButton;
            }
        }

        void RegisterButton(string code, GameObject root)
        {
            var button = root.GetComponent<Button>() ?? root.GetComponentInChildren<Button>(true);
            if (button != null)
                _buttons[code] = button;
        }

        void OnLanguageItemClicked(FastGameMenuItem item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Code))
                return;
            OnLanguageButtonClicked(item.Code);
        }

        void OnLanguageButtonClicked(string languageCode)
        {
            SelectLanguage(languageCode);
            HighlightSelected();
            if (AdvanceOnSelect)
                Continue();
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

        public static string LanguageLabel(string code)
        {
            var norm = code?.Trim().ToLowerInvariant() ?? "";
            return norm switch
            {
                "en" => "English",
                "fa" => "Persian",
                "ar" => "Arabic",
                _ => TryCultureEnglishName(norm) ?? norm.ToUpperInvariant(),
            };
        }

        static string TryCultureEnglishName(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return null;
            try
            {
                return CultureInfo.GetCultureInfo(code).EnglishName;
            }
            catch (CultureNotFoundException)
            {
                return null;
            }
        }

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

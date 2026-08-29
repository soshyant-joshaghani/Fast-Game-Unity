using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FastGame.Models;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FastGame
{
    [Serializable] public class FastGameCollectibleInspectEvent : UnityEvent<CollectibleDef> { }

    /// <summary>
    /// MENU hub — Menu / Shop / Collectibles canvases + inspect detail pages.
    /// </summary>
    [AddComponentMenu("Fast Game/Scenes/Menu")]
    public sealed class FastGameMenuSceneBehaviour : FastGameSceneFlowBehaviour
    {
        [Header("Top-level pages")]
        public GameObject MenuCanvas;
        public GameObject ShopCanvas;
        public GameObject CollectiblesCanvas;

        [Header("Collectibles sub-pages")]
        public GameObject AchievementsCanvas;
        public GameObject TitlesCanvas;
        public GameObject AvatarsCanvas;

        [Header("Inspect detail pages")]
        public GameObject InspectAchievementCanvas;
        public GameObject InspectTitleCanvas;
        public GameObject InspectAvatarCanvas;

        [Header("Inspect labels (optional)")]
        public Component InspectTitleLabel;
        public Component InspectBodyLabel;
        public Component InspectImage;

        [Header("List UI (optional prefab + container per kind)")]
        public Transform AchievementListContainer;
        public Transform TitleListContainer;
        public Transform AvatarListContainer;
        public GameObject CollectibleRowPrefab;

        [Header("Shop")]
        public FastGameShopBehaviour Shop;

        [Header("Level")]
        public string DefaultLevelScene = FastGameSceneNames.LevelSample;

        [Header("Logout")]
        [Tooltip("Scene to open after Logout (default LANGUAGE).")]
        public string LogoutScene = FastGameSceneNames.Language;

        [Header("Events")]
        public FastGameCollectibleInspectEvent OnInspectAchievement;
        public FastGameCollectibleInspectEvent OnInspectTitle;
        public FastGameCollectibleInspectEvent OnInspectAvatar;

        CollectibleDef _selected;

        void Awake()
        {
            AutoLoadNextOnComplete = false;
        }

        void Start()
        {
            ShowMenu();
        }

        public void ShowMenu() =>
            ShowTop(MenuCanvas);

        public void ShowShop()
        {
            ShowTop(ShopCanvas);
            if (Shop != null)
                Shop.RefreshCatalog();
        }

        public void ShowCollectibles() =>
            ShowTop(CollectiblesCanvas);

        public void ShowAchievements()
        {
            ShowCollectiblesSub(AchievementsCanvas);
            _ = PopulateListAsync(AchievementListContainer, LoadAchievementsAsync, InspectAchievement);
        }

        public void ShowTitles()
        {
            ShowCollectiblesSub(TitlesCanvas);
            _ = PopulateListAsync(TitleListContainer, LoadTitlesAsync, InspectTitle);
        }

        public void ShowAvatars()
        {
            ShowCollectiblesSub(AvatarsCanvas);
            _ = PopulateListAsync(AvatarListContainer, LoadAvatarsAsync, InspectAvatar);
        }

        public void InspectAchievement(CollectibleDef item) => Inspect(item, InspectAchievementCanvas, OnInspectAchievement);
        public void InspectTitle(CollectibleDef item) => Inspect(item, InspectTitleCanvas, OnInspectTitle);
        public void InspectAvatar(CollectibleDef item) => Inspect(item, InspectAvatarCanvas, OnInspectAvatar);

        public void InspectAchievementByCode(string code) => InspectByCode(code, LoadAchievementsAsync, InspectAchievement);
        public void InspectTitleByCode(string code) => InspectByCode(code, LoadTitlesAsync, InspectTitle);
        public void InspectAvatarByCode(string code) => InspectByCode(code, LoadAvatarsAsync, InspectAvatar);

        public void CloseInspect()
        {
            SetActive(InspectAchievementCanvas, false);
            SetActive(InspectTitleCanvas, false);
            SetActive(InspectAvatarCanvas, false);
        }

        public void OpenLevel(string levelScene = null)
        {
            LoadScene(string.IsNullOrWhiteSpace(levelScene) ? DefaultLevelScene : levelScene);
        }

        /// <summary>Wire Logout button — clears session and opens <see cref="LogoutScene"/>.</summary>
        public void Logout()
        {
            FastGameLocalData.ClearAuthSession();
            LoadScene(LogoutScene);
        }

        void ShowTop(GameObject active)
        {
            FastGameUiPages.ShowOnly(active, MenuCanvas, ShopCanvas, CollectiblesCanvas);
            SetActive(AchievementsCanvas, false);
            SetActive(TitlesCanvas, false);
            SetActive(AvatarsCanvas, false);
            SetActive(InspectAchievementCanvas, false);
            SetActive(InspectTitleCanvas, false);
            SetActive(InspectAvatarCanvas, false);
        }

        void ShowCollectiblesSub(GameObject active)
        {
            SetActive(CollectiblesCanvas, true);
            SetActive(MenuCanvas, false);
            SetActive(ShopCanvas, false);
            FastGameUiPages.ShowOnly(active, AchievementsCanvas, TitlesCanvas, AvatarsCanvas);
            SetActive(InspectAchievementCanvas, false);
            SetActive(InspectTitleCanvas, false);
            SetActive(InspectAvatarCanvas, false);
        }

        void Inspect(CollectibleDef item, GameObject inspectCanvas, FastGameCollectibleInspectEvent evt)
        {
            if (item == null)
                return;
            _selected = item;
            SetActive(inspectCanvas, true);
            FastGameUiText.WriteLabel(InspectTitleLabel, item.Label ?? item.Code);
            FastGameUiText.WriteLabel(InspectBodyLabel, item.Code);
            evt?.Invoke(item);
        }

        async void InspectByCode(
            string code,
            Func<Task<List<CollectibleDef>>> loader,
            Action<CollectibleDef> inspect)
        {
            foreach (var row in await loader())
            {
                if (string.Equals(row.Code, code, StringComparison.OrdinalIgnoreCase))
                {
                    inspect(row);
                    return;
                }
            }
        }

        async Task PopulateListAsync(
            Transform container,
            Func<Task<List<CollectibleDef>>> loader,
            Action<CollectibleDef> onPick)
        {
            if (container == null || CollectibleRowPrefab == null)
                return;

            for (var i = container.childCount - 1; i >= 0; i--)
                Destroy(container.GetChild(i).gameObject);

            List<CollectibleDef> rows;
            try
            {
                rows = await loader();
            }
            catch
            {
                return;
            }

            foreach (var row in rows)
            {
                var go = Instantiate(CollectibleRowPrefab, container);
                go.name = row.Code ?? row.Id;
                var label = go.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = row.Label ?? row.Code;
                var button = go.GetComponent<Button>();
                if (button != null)
                {
                    var captured = row;
                    button.onClick.AddListener(() => onPick(captured));
                }
            }
        }

        async Task<List<CollectibleDef>> LoadAchievementsAsync() => await LoadRows(
            (content, game, lang) => content.ListAchievementsAsync(game, lang));
        async Task<List<CollectibleDef>> LoadTitlesAsync() => await LoadRows(
            (content, game, lang) => content.ListTitlesAsync(game, lang));
        async Task<List<CollectibleDef>> LoadAvatarsAsync() => await LoadRows(
            (content, game, lang) => content.ListAvatarsAsync(game, lang));

        static async Task<List<CollectibleDef>> LoadRows(
            Func<FastGameContent, string, string, Task<List<CollectibleDef>>> fetch)
        {
            var host = FastGameClientBehaviour.Instance;
            if (host?.Client == null)
                return new List<CollectibleDef>();
            var lang = FastGameLocalePrefs.Get("en");
            return await fetch(host.Client.Content, host.GameCode, lang);
        }

        static void SetActive(GameObject go, bool active)
        {
            if (go != null)
                go.SetActive(active);
        }
    }
}

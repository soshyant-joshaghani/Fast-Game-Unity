using System;
using FastGame.Models;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FastGame
{
    [Serializable]
    public sealed class FastGameMenuItemEvent : UnityEvent<FastGameMenuItem> { }

    /// <summary>ITEMS_THUMB — one row in ITEMS_SCRLVIEW_* lists.</summary>
    [AddComponentMenu("Fast Game/UI/Items Thumb")]
    public sealed class FastGameItemsThumbView : MonoBehaviour
    {
        const float DefaultThumbWidth = 128f;

        [Header("Bindings (auto-find THUMB_* if empty)")]
        public Component ThumbImage;
        public Component ThumbLabel;
        public Button ClickButton;

        [Header("Events")]
        public FastGameMenuItemEvent OnClicked;

        FastGameMenuItem _item;
        Action<FastGameMenuItem> _clickHandler;

        void Awake() => ResolveBindings();

        public void Bind(FastGameMenuItem item, Action<FastGameMenuItem> onClick = null)
        {
            _item = item;
            _clickHandler = onClick;
            ResolveBindings();
            WireClick();

            if (item == null)
            {
                FastGameUiText.WriteLabel(ThumbLabel, "");
                return;
            }

            var label = item.Label ?? item.Code ?? item.Id ?? "";
            if (item.Locked)
                label += " 🔒";
            if (item.Owned)
                label += " ✓";
            FastGameUiText.WriteLabel(ThumbLabel, label);

            if (!string.IsNullOrWhiteSpace(item.ImageUrl))
                _ = FastGameUiImage.SetFromUrlAsync(ThumbImage, item.ImageUrl);
        }

        public void LayoutThumb(FastGameScrollLayout layout, int index, float spacing = 8f)
        {
            var rt = transform as RectTransform;
            if (rt == null)
                return;

            switch (layout)
            {
                case FastGameScrollLayout.Horizontal:
                    rt.anchorMin = new Vector2(0f, 0f);
                    rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0f, 0.5f);
                    rt.sizeDelta = new Vector2(DefaultThumbWidth, 0f);
                    rt.anchoredPosition = new Vector2(index * (DefaultThumbWidth + spacing), 0f);
                    break;
                case FastGameScrollLayout.Vertical:
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(1f, 1f);
                    rt.pivot = new Vector2(0.5f, 1f);
                    rt.sizeDelta = new Vector2(0f, DefaultThumbWidth);
                    rt.anchoredPosition = new Vector2(0f, -index * (DefaultThumbWidth + spacing));
                    break;
                case FastGameScrollLayout.Grid:
                {
                    const int columns = 3;
                    var col = index % columns;
                    var row = index / columns;
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0f, 1f);
                    rt.sizeDelta = new Vector2(DefaultThumbWidth, DefaultThumbWidth);
                    rt.anchoredPosition = new Vector2(
                        col * (DefaultThumbWidth + spacing),
                        -row * (DefaultThumbWidth + spacing));
                    break;
                }
            }
        }

        void ResolveBindings()
        {
            if (ThumbLabel == null)
            {
                var txt = transform.Find("THUMB_TXT");
                ThumbLabel = txt != null
                    ? FastGameUiText.ResolveLabel(txt.GetComponent<Component>())
                    : null;
            }
            if (ThumbImage == null)
            {
                var img = transform.Find("THUMB_IMG");
                ThumbImage = img != null ? img.GetComponent<Component>() : null;
            }
            if (ClickButton == null)
                ClickButton = GetComponent<Button>() ?? GetComponentInChildren<Button>(true);
        }

        void WireClick()
        {
            if (ClickButton == null)
                return;

            ClickButton.onClick.RemoveListener(HandleClick);
            ClickButton.onClick.AddListener(HandleClick);
        }

        void HandleClick()
        {
            if (_clickHandler != null)
                _clickHandler(_item);
            else
                OnClicked?.Invoke(_item);
        }
    }
}

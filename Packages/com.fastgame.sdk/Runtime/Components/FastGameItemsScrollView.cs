using System.Collections.Generic;
using FastGame.Models;
using UnityEngine;
using UnityEngine.UI;

namespace FastGame
{
    public enum FastGameScrollLayout
    {
        Horizontal,
        Vertical,
        Grid,
    }

    /// <summary>ITEMS_SCRLVIEW_H / V / HV — populate Content with ITEMS_THUMB rows.</summary>
    [AddComponentMenu("Fast Game/UI/Items Scroll View")]
    [RequireComponent(typeof(ScrollRect))]
    public sealed class FastGameItemsScrollView : MonoBehaviour
    {
        const float ThumbWidth = 128f;
        const float Spacing = 8f;

        [Header("Layout")]
        public FastGameScrollLayout Layout = FastGameScrollLayout.Horizontal;

        [Header("List")]
        public GameObject ThumbPrefab;
        public Transform ContentOverride;

        [Header("Events")]
        public FastGameMenuItemEvent OnItemClicked;

        ScrollRect _scroll;
        readonly List<FastGameItemsThumbView> _rows = new List<FastGameItemsThumbView>();

        public IReadOnlyList<FastGameItemsThumbView> Rows => _rows;

        void Awake()
        {
            _scroll = GetComponent<ScrollRect>();
            ApplyLayout();
        }

        public void Clear()
        {
            var content = GetContent();
            if (content == null)
                return;
            for (var i = content.childCount - 1; i >= 0; i--)
                Destroy(content.GetChild(i).gameObject);
            _rows.Clear();
        }

        public void SetItems(IReadOnlyList<FastGameMenuItem> items)
        {
            Clear();
            if (items == null || items.Count == 0)
                return;

            if (ThumbPrefab == null)
            {
                Debug.LogWarning(
                    $"[FastGame Menu] {name}: ThumbPrefab is not assigned — cannot show {items.Count} item(s).",
                    this);
                return;
            }

            var content = GetContent();
            if (content == null)
            {
                Debug.LogWarning($"[FastGame Menu] {name}: scroll Content is missing.", this);
                return;
            }

            var index = 0;
            foreach (var item in items)
            {
                if (item == null)
                    continue;

                var go = Instantiate(ThumbPrefab, content);
                go.name = string.IsNullOrWhiteSpace(item.Code)
                    ? (string.IsNullOrWhiteSpace(item.Id) ? go.name : item.Id)
                    : item.Code;

                var thumb = go.GetComponent<FastGameItemsThumbView>();
                if (thumb == null)
                    thumb = go.AddComponent<FastGameItemsThumbView>();

                thumb.LayoutThumb(Layout, index, Spacing);
                thumb.Bind(item, clicked =>
                {
                    if (clicked != null)
                        OnItemClicked?.Invoke(clicked);
                });
                _rows.Add(thumb);
                index++;
            }

            ResizeContent(content, index);
        }

        void ResizeContent(Transform content, int count)
        {
            var rt = content as RectTransform;
            if (rt == null || count <= 0)
                return;

            switch (Layout)
            {
                case FastGameScrollLayout.Horizontal:
                    rt.sizeDelta = new Vector2(
                        count * (ThumbWidth + Spacing),
                        rt.sizeDelta.y);
                    break;
                case FastGameScrollLayout.Vertical:
                    rt.sizeDelta = new Vector2(
                        rt.sizeDelta.x,
                        count * (ThumbWidth + Spacing));
                    break;
                case FastGameScrollLayout.Grid:
                {
                    const int columns = 3;
                    var rows = (count + columns - 1) / columns;
                    rt.sizeDelta = new Vector2(
                        columns * (ThumbWidth + Spacing),
                        rows * (ThumbWidth + Spacing));
                    break;
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }

        Transform GetContent()
        {
            if (ContentOverride != null)
                return ContentOverride;
            if (_scroll == null)
                _scroll = GetComponent<ScrollRect>();
            return _scroll != null ? _scroll.content : null;
        }

        void ApplyLayout()
        {
            if (_scroll == null)
                return;

            switch (Layout)
            {
                case FastGameScrollLayout.Horizontal:
                    _scroll.horizontal = true;
                    _scroll.vertical = false;
                    break;
                case FastGameScrollLayout.Vertical:
                    _scroll.horizontal = false;
                    _scroll.vertical = true;
                    break;
                case FastGameScrollLayout.Grid:
                    _scroll.horizontal = true;
                    _scroll.vertical = true;
                    break;
            }
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (_scroll == null)
                _scroll = GetComponent<ScrollRect>();
            ApplyLayout();
        }
#endif
    }
}

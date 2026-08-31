using System;
using FastGame.Models;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FastGame
{
    [Serializable]
    public sealed class FastGameInspectActionEvent : UnityEvent<FastGameMenuItem, int> { }

    /// <summary>INSPECT_CANVAS — detail overlay with back + three custom actions.</summary>
    [AddComponentMenu("Fast Game/UI/Inspect View")]
    public sealed class FastGameInspectView : MonoBehaviour
    {
        [Header("Detail")]
        public Component NameLabel;
        public Component DescriptionLabel;
        public Component ThumbImage;

        [Header("Actions")]
        public Button Action0Button;
        public Button Action1Button;
        public Button Action2Button;
        public Component Action0Label;
        public Component Action1Label;
        public Component Action2Label;

        [Header("Navigation")]
        public Button BackButton;

        [Header("Overlay")]
        [Tooltip("Draw above main menu when INSPECT_CANVAS has its own Canvas.")]
        public int SortingOrder = 100;

        [Header("Events")]
        public FastGameInspectActionEvent OnAction;
        public UnityEvent OnBack;

        FastGameMenuItem _item;
        bool _ready;

        public void Show(
            FastGameMenuItem item,
            FastGameInspectActionSlot action0,
            FastGameInspectActionSlot action1,
            FastGameInspectActionSlot action2)
        {
            EnsureReady();
            _item = item;
            gameObject.SetActive(true);
            BringToFront();

            if (item == null)
                return;

            FastGameUiText.WriteLabel(NameLabel, item.Label ?? item.Code ?? "");
            FastGameUiText.WriteLabel(DescriptionLabel, item.Description ?? item.Code ?? "");
            if (!string.IsNullOrWhiteSpace(item.ImageUrl))
                _ = FastGameUiImage.SetFromUrlAsync(ThumbImage, item.ImageUrl);

            ApplyAction(Action0Button, Action0Label, action0);
            ApplyAction(Action1Button, Action1Label, action1);
            ApplyAction(Action2Button, Action2Label, action2);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            _item = null;
        }

        void EnsureReady()
        {
            if (_ready)
                return;

            AutoBind();
            if (BackButton != null)
            {
                BackButton.onClick.RemoveListener(HandleBack);
                BackButton.onClick.AddListener(HandleBack);
            }
            WireAction(Action0Button, 0);
            WireAction(Action1Button, 1);
            WireAction(Action2Button, 2);
            _ready = true;
        }

        void HandleBack() => OnBack?.Invoke();

        void AutoBind()
        {
            NameLabel ??= FindLabel("NAME_TXT");
            DescriptionLabel ??= FindLabel("DESCRIPTION_TXT");
            ThumbImage ??= FindTransform("THUMB_IMG")?.GetComponent<Component>()
                ?? FindTransform("Right_Panel/THUMB_IMG")?.GetComponent<Component>();

            Action0Button ??= FindTransform("ACTION_0_BTN")?.GetComponent<Button>();
            Action1Button ??= FindTransform("ACTION_1_BTN")?.GetComponent<Button>();
            Action2Button ??= FindTransform("ACTION_2_BTN")?.GetComponent<Button>();
            BackButton ??= FindTransform("BACK_BTN")?.GetComponent<Button>();

            Action0Label ??= FindLabelOn(Action0Button);
            Action1Label ??= FindLabelOn(Action1Button);
            Action2Label ??= FindLabelOn(Action2Button);
        }

        static Component FindLabelOn(Button btn)
        {
            if (btn == null)
                return null;
            return FastGameUiText.ResolveLabel(btn.GetComponent<Component>())
                ?? FastGameUiText.ResolveLabel(btn.GetComponentInChildren<Component>(true));
        }

        Component FindLabel(string name)
        {
            var t = FindTransform(name);
            if (t == null)
                return null;
            return FastGameUiText.ResolveLabel(t.GetComponent<Component>());
        }

        Transform FindTransform(string path)
        {
            var direct = transform.Find(path);
            if (direct != null)
                return direct;
            var leaf = path.Contains("/") ? path.Substring(path.LastIndexOf('/') + 1) : path;
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name == leaf)
                    return t;
            }
            return null;
        }

        void WireAction(Button btn, int index)
        {
            if (btn == null)
                return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnAction?.Invoke(_item, index));
        }

        void BringToFront()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
                return;
            canvas.overrideSorting = true;
            canvas.sortingOrder = SortingOrder;
        }

        static void ApplyAction(Button btn, Component label, FastGameInspectActionSlot slot)
        {
            if (btn != null)
                btn.gameObject.SetActive(slot.Visible);
            if (slot.Visible && label != null)
                FastGameUiText.WriteLabel(label, slot.Label ?? "");
        }
    }
}

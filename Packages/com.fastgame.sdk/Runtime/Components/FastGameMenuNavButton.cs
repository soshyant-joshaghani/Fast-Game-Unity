using UnityEngine;
using UnityEngine.UI;

namespace FastGame
{
    /// <summary>
    /// Drop on any menu footer/header/sub-page button. Wires OnClick to <see cref="FastGameMenuSceneBehaviour"/>.
    /// Action can be set in Inspector or left as None for auto-detect on <see cref="FastGameMenuSceneBehaviour"/> start.
    /// </summary>
    [AddComponentMenu("Fast Game/UI/Menu Nav Button")]
    [RequireComponent(typeof(Button))]
    public sealed class FastGameMenuNavButton : MonoBehaviour
    {
        public FastGameMenuNavAction Action = FastGameMenuNavAction.None;

        [Tooltip("Empty → find FastGameMenuSceneBehaviour in parents.")]
        public FastGameMenuSceneBehaviour Menu;

        Button _button;

        void Awake()
        {
            _button = GetComponent<Button>();
        }

        void Start()
        {
            Bind(FindMenu());
        }

        public void Bind(FastGameMenuSceneBehaviour menu)
        {
            Menu = menu;
            if (_button == null)
                _button = GetComponent<Button>();
            if (_button == null || Menu == null || Action == FastGameMenuNavAction.None)
                return;

            _button.onClick.RemoveListener(OnClick);
            _button.onClick.AddListener(OnClick);
        }

        void OnClick() => Menu?.DispatchNav(Action);

        FastGameMenuSceneBehaviour FindMenu()
        {
            if (Menu != null)
                return Menu;
            return GetComponentInParent<FastGameMenuSceneBehaviour>()
                ?? FindObjectOfType<FastGameMenuSceneBehaviour>();
        }
    }
}

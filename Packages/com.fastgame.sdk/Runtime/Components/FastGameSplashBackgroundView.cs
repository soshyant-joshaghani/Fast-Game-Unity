using UnityEngine;
using UnityEngine.UI;

namespace FastGame
{
    /// <summary>
    /// Full-screen splash background color — place on <b>SPLASH_BG</b> (stretch Image under canvas).
    /// Stays visible for the whole splash; not hidden when image/video layers toggle.
    /// </summary>
    [AddComponentMenu("Fast Game/Splash/Background View")]
    public sealed class FastGameSplashBackgroundView : MonoBehaviour
    {
        public Image Image;
        public Color BackgroundColor = new Color(0.108f, 0.146f, 0.255f, 1f);

        void Reset()
        {
            Image = GetComponent<Image>();
        }

        public void Show()
        {
            if (Image == null)
                Image = GetComponent<Image>();
            gameObject.SetActive(true);
            if (Image != null)
            {
                Image.color = BackgroundColor;
                Image.enabled = true;
            }
        }
    }
}

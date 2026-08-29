using UnityEngine;
using UnityEngine.UI;

namespace FastGame
{
    /// <summary>
    /// Splash image layer — assign on <b>SPLASH_IMAGE</b>; visibility is driven by
    /// <see cref="FastGameSplashBehaviour"/>.
    /// </summary>
    [AddComponentMenu("Fast Game/Splash/Image View")]
    public sealed class FastGameSplashImageView : MonoBehaviour
    {
        public Image Image;
        public RawImage RawImage;
        [Tooltip("Local splash sprite (build-shipped).")]
        public Sprite LocalSprite;

        Texture2D _runtimeTexture;

        void Reset()
        {
            Image = GetComponent<Image>();
            RawImage = GetComponent<RawImage>();
        }

        void OnDestroy()
        {
            if (_runtimeTexture != null)
                Destroy(_runtimeTexture);
        }

        public bool HasLocalContent => LocalSprite != null;

        public void Hide()
        {
            if (Image != null)
                Image.enabled = false;
            if (RawImage != null)
                RawImage.enabled = false;
            gameObject.SetActive(false);
        }

        public bool ShowLocal()
        {
            if (LocalSprite == null)
                return false;
            gameObject.SetActive(true);
            if (Image != null)
            {
                Image.sprite = LocalSprite;
                Image.enabled = true;
                return true;
            }
            if (RawImage != null)
            {
                RawImage.texture = LocalSprite.texture;
                RawImage.enabled = true;
                return true;
            }
            return false;
        }

        public bool ShowTexture(Texture2D texture)
        {
            if (texture == null)
                return false;
            gameObject.SetActive(true);
            if (RawImage != null)
            {
                RawImage.texture = texture;
                RawImage.enabled = true;
                return true;
            }
            if (Image != null)
            {
                Image.sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f));
                Image.enabled = true;
                return true;
            }
            return false;
        }

        public bool ShowFromBytes(byte[] bytes)
        {
            if (_runtimeTexture != null)
            {
                Destroy(_runtimeTexture);
                _runtimeTexture = null;
            }
            _runtimeTexture = FastGameImageUtil.TextureFromBytes(bytes);
            return ShowTexture(_runtimeTexture);
        }
    }
}

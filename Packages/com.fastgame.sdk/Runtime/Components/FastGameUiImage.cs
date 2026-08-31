using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace FastGame
{
    /// <summary>Load remote images into UI Image / RawImage without extra dependencies.</summary>
    public static class FastGameUiImage
    {
        static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();
        static readonly BindingFlags PropFlags = BindingFlags.Instance | BindingFlags.Public;

        public static async Task SetFromUrlAsync(Component target, string url)
        {
            if (target == null || string.IsNullOrWhiteSpace(url))
                return;

            if (!Cache.TryGetValue(url, out var tex) || tex == null)
            {
                tex = await DownloadTextureAsync(url);
                if (tex != null)
                    Cache[url] = tex;
            }

            if (tex != null)
                ApplyTexture(target, tex);
        }

        public static async Task<Texture2D> DownloadTextureAsync(string url)
        {
            using var req = UnityWebRequest.Get(url);
            req.downloadHandler = new DownloadHandlerBuffer();
            var op = req.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

#if UNITY_2020_2_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
                return null;

            return FastGameImageUtil.TextureFromBytes(req.downloadHandler.data);
        }

        static void ApplyTexture(Component target, Texture2D tex)
        {
            var raw = ResolveRawImage(target);
            if (raw != null)
            {
                TrySetProperty(raw, "texture", tex);
                return;
            }

            var image = ResolveImage(target);
            if (image == null)
                return;

            var spriteProp = image.GetType().GetProperty("sprite", PropFlags);
            if (spriteProp == null || !spriteProp.CanWrite)
                return;

            var rect = new Rect(0, 0, tex.width, tex.height);
            var pivot = new Vector2(0.5f, 0.5f);
            spriteProp.SetValue(image, Sprite.Create(tex, rect, pivot));
        }

        static Component ResolveRawImage(Component c)
        {
            if (c == null) return null;
            if (c.GetType().Name == "RawImage") return c;
            return c.GetComponentInChildren<Component>(true) is Component child
                && child.GetType().Name == "RawImage"
                ? child
                : FindByTypeName(c.gameObject, "RawImage");
        }

        static Component ResolveImage(Component c)
        {
            if (c == null) return null;
            if (c.GetType().Name == "Image") return c;
            return FindByTypeName(c.gameObject, "Image");
        }

        static Component FindByTypeName(GameObject go, string typeName)
        {
            foreach (var comp in go.GetComponentsInChildren<Component>(true))
            {
                if (comp != null && comp.GetType().Name == typeName)
                    return comp;
            }
            return null;
        }

        static void TrySetProperty(Component c, string name, object value)
        {
            var prop = c.GetType().GetProperty(name, PropFlags);
            if (prop != null && prop.CanWrite)
                prop.SetValue(c, value);
        }
    }
}

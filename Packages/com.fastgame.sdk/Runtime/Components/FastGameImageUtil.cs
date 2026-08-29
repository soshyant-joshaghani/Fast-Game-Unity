using System.Reflection;
using UnityEngine;

namespace FastGame
{
    /// <summary>Decode PNG/JPEG bytes without a compile-time ImageConversion module reference.</summary>
    public static class FastGameImageUtil
    {
        static readonly MethodInfo LoadImageMethod = typeof(Texture2D).GetMethod(
            "LoadImage",
            BindingFlags.Instance | BindingFlags.Public,
            null,
            new[] { typeof(byte[]) },
            null);

        public static Texture2D TextureFromBytes(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0 || LoadImageMethod == null)
                return null;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!(bool)LoadImageMethod.Invoke(tex, new object[] { bytes }))
            {
                Object.Destroy(tex);
                return null;
            }
            return tex;
        }
    }
}

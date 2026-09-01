using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin.Rendering
{
    public static class TextureThumbnailUtility
    {
        private static readonly Dictionary<string, Texture2D> cachedThumbnails = new();

        public static bool TryGetCachedThumbnail(string key, out Texture2D thumbnail)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                thumbnail = null;
                return false;
            }

            return cachedThumbnails.TryGetValue(key, out thumbnail) && thumbnail != null;
        }

        public static void CacheThumbnail(string key, Texture2D thumbnail)
        {
            if (string.IsNullOrWhiteSpace(key) || thumbnail == null) return;

            if (cachedThumbnails.TryGetValue(key, out var existingThumbnail) && existingThumbnail != null && existingThumbnail != thumbnail)
                Object.Destroy(existingThumbnail);

            cachedThumbnails[key] = thumbnail;
        }

        public static Texture2D CreateThumbnail(Texture source, int maxDimension, string name = null)
        {
            if (source == null) return null;
            if (maxDimension <= 0) return source as Texture2D;

            int sourceWidth = source.width;
            int sourceHeight = source.height;
            int largestSide = Mathf.Max(sourceWidth, sourceHeight);

            if (largestSide <= maxDimension)
                return source as Texture2D;

            float scale = maxDimension / (float)largestSide;
            int width = Mathf.Max(1, Mathf.RoundToInt(sourceWidth * scale));
            int height = Mathf.Max(1, Mathf.RoundToInt(sourceHeight * scale));

            RenderTexture previousActive = RenderTexture.active;
            RenderTexture renderTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);

            Graphics.Blit(source, renderTexture);
            RenderTexture.active = renderTexture;

            var thumbnail = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = string.IsNullOrWhiteSpace(name) ? source.name + " Thumbnail" : name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            thumbnail.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            thumbnail.Apply(false, false);

            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(renderTexture);

            return thumbnail;
        }

        public static void FitWidthToTextureAspect(RectTransform rectTransform, Texture texture)
        {
            if (rectTransform == null || texture == null || texture.height <= 0) return;

            float height = rectTransform.rect.height;
            if (height <= 0)
                height = rectTransform.sizeDelta.y;

            if (height <= 0) return;

            float aspectRatio = texture.width / (float)texture.height;
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, height * aspectRatio);
        }
    }
}

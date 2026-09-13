using System;
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
                UnityEngine.Object.Destroy(existingThumbnail);

            cachedThumbnails[key] = thumbnail;
        }

        public static bool IsDataUri(string path)
        {
            return path?.StartsWith("data:", StringComparison.OrdinalIgnoreCase) == true;
        }

        public static bool TryCreateThumbnailFromDataUri(string dataUri, int maxDimension, string name, out Texture2D thumbnail, out string error)
        {
            thumbnail = null;
            error = null;

            try
            {
                int separatorIndex = dataUri?.IndexOf(',') ?? -1;
                if (separatorIndex < 0 || !dataUri[..separatorIndex].Contains(";base64", StringComparison.OrdinalIgnoreCase))
                    throw new FormatException("The image data URI must contain Base64-encoded data.");

                byte[] imageData = Convert.FromBase64String(dataUri[(separatorIndex + 1)..]);
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                imageData = ResizeEncodedImageBeforeUpload(imageData, maxDimension, dataUri[..separatorIndex]);
#endif
                var sourceTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!sourceTexture.LoadImage(imageData, true))
                {
                    UnityEngine.Object.Destroy(sourceTexture);
                    throw new FormatException("The image data URI does not contain a supported image.");
                }

                thumbnail = CreateThumbnail(sourceTexture, maxDimension, name);
                if (thumbnail != sourceTexture)
                    UnityEngine.Object.Destroy(sourceTexture);

                return true;
            }
            catch (Exception exception) when (exception is FormatException || exception is ArgumentException)
            {
                error = exception.Message;
                return false;
            }
        }

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        private static byte[] ResizeEncodedImageBeforeUpload(byte[] imageData, int maxDimension, string dataUriHeader)
        {
            if (maxDimension <= 0) return imageData;

            IDisposable sourceImage = null;
            IDisposable resizedImage = null;
            IDisposable drawingGraphics = null;

            try
            {
                Type imageType = ResolveDrawingType("System.Drawing.Image");
                Type bitmapType = ResolveDrawingType("System.Drawing.Bitmap");
                Type graphicsType = ResolveDrawingType("System.Drawing.Graphics");
                Type imageFormatType = ResolveDrawingType("System.Drawing.Imaging.ImageFormat");
                if (imageType == null || bitmapType == null || graphicsType == null || imageFormatType == null)
                    return imageData;

                using var sourceStream = new System.IO.MemoryStream(imageData, false);
                sourceImage = imageType.GetMethod("FromStream", new[] { typeof(System.IO.Stream) })?
                    .Invoke(null, new object[] { sourceStream }) as IDisposable;
                if (sourceImage == null)
                    return imageData;

                int sourceWidth = (int)imageType.GetProperty("Width")!.GetValue(sourceImage);
                int sourceHeight = (int)imageType.GetProperty("Height")!.GetValue(sourceImage);

                int largestSide = Math.Max(sourceWidth, sourceHeight);
                if (largestSide <= maxDimension)
                    return imageData;

                float scale = maxDimension / (float)largestSide;
                int width = Math.Max(1, (int)Math.Round(sourceWidth * scale));
                int height = Math.Max(1, (int)Math.Round(sourceHeight * scale));

                resizedImage = Activator.CreateInstance(bitmapType, width, height) as IDisposable;
                drawingGraphics = graphicsType.GetMethod("FromImage", new[] { imageType })?
                    .Invoke(null, new object[] { resizedImage }) as IDisposable;
                graphicsType.GetMethod("DrawImage", new[] { imageType, typeof(int), typeof(int), typeof(int), typeof(int) })?
                    .Invoke(drawingGraphics, new object[] { sourceImage, 0, 0, width, height });

                using var resizedStream = new System.IO.MemoryStream();
                bool preserveTransparency = dataUriHeader.StartsWith("data:image/png", StringComparison.OrdinalIgnoreCase);
                object imageFormat = imageFormatType.GetProperty(preserveTransparency ? "Png" : "Jpeg")?.GetValue(null);
                imageType.GetMethod("Save", new[] { typeof(System.IO.Stream), imageFormatType })?
                    .Invoke(resizedImage, new[] { resizedStream, imageFormat });
                return resizedStream.ToArray();
            }
            catch (Exception)
            {
                // Fall back to Unity's decoder for uncommon or unsupported image encodings.
                return imageData;
            }
            finally
            {
                drawingGraphics?.Dispose();
                resizedImage?.Dispose();
                sourceImage?.Dispose();
            }
        }

        private static Type ResolveDrawingType(string fullName)
        {
            Type type = Type.GetType(fullName + ", System.Drawing")
                        ?? Type.GetType(fullName + ", System.Drawing.Common");
            if (type != null) return type;

            try
            {
                return System.Reflection.Assembly.Load("System.Drawing").GetType(fullName);
            }
            catch (Exception)
            {
                try
                {
                    string managedDirectory = System.IO.Path.GetDirectoryName(typeof(Debug).Assembly.Location);
                    string unityDataDirectory = System.IO.Directory.GetParent(managedDirectory)?.FullName;
                    string drawingAssemblyPath = System.IO.Path.Combine(
                        unityDataDirectory ?? "",
                        "MonoBleedingEdge",
                        "lib",
                        "mono",
                        "unityjit-win32",
                        "System.Drawing.dll"
                    );

                    return System.Reflection.Assembly.LoadFrom(drawingAssemblyPath).GetType(fullName);
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }
#endif

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

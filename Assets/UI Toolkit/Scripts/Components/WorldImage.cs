using System;
using System.Threading.Tasks;
using KindMen.Uxios;
using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI.ExtensionMethods;
using Netherlands3D.Web;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class WorldImage : VisualElement
    {
        private const int MaxImageDimension = 512;
        private VisualElement image;
       
        private float labelOffsetToPosition = 0;
        
        public UnityEvent ImageLoaded = new();
        
        public WorldImage()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
            
            image = this.Q<VisualElement>("Image");
        }

        public async Task SetImage(string url)
        {
            var imageUrl = AddImageSizeParameters(url);
            var texture = await DownloadTextureAsync(imageUrl);

            if (texture == null)
                return;

            if (texture.width > MaxImageDimension || texture.height > MaxImageDimension)
            {
                Debug.LogError($"image has too large resolution: {texture.width}x{texture.height}");
                return;
            }

            image.style.width = texture.width;
            image.style.height = texture.height;
            image.style.backgroundImage = new StyleBackground(texture);

            ImageLoaded.Invoke();
        }

        private string AddImageSizeParameters(string url)
        {
            var uri = new Uri(url);
            var queryParameters = QueryString.Decode(uri.Query);
            var uriBuilder = new UriBuilder(uri);

            if (int.TryParse(queryParameters.Single("w"), out var sourceWidth) &&
                int.TryParse(queryParameters.Single("h"), out var sourceHeight))
            {
                int largestSide = Mathf.Max(sourceWidth, sourceHeight);

                if (largestSide > MaxImageDimension)
                {
                    float scale = MaxImageDimension / (float)largestSide;

                    int width = Mathf.Max(1, Mathf.RoundToInt(sourceWidth * scale));
                    int height = Mathf.Max(1, Mathf.RoundToInt(sourceHeight * scale));

                    uriBuilder.SetQueryParameter("w", width.ToString());
                    uriBuilder.SetQueryParameter("h", height.ToString());
                }
            }
            string format = queryParameters.Single("format")?.ToLowerInvariant();
            if (format is not "png" and not "jpg")
            {
                format = "jpg";
            }
            uriBuilder.SetQueryParameter("format", format);
            return uriBuilder.Uri.ToString();
        }

        private async Task<Texture2D> DownloadTextureAsync(string url)
        {
            using var request = UnityWebRequestTexture.GetTexture(url);
            var operation = request.SendWebRequest();
            var tcs = new TaskCompletionSource<bool>();
            operation.completed += _ => tcs.TrySetResult(true);
            await tcs.Task;

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to download image: {request.error}");
                return null;
            }

            return DownloadHandlerTexture.GetContent(request);
        }

        public void SetColor(Color color)
        {
            image.style.backgroundColor = color;
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Netherlands3D.Twin
{
    public class LayerWithImage : MonoBehaviour, ILayerWithPropertyData
    {
        public LayerURLPropertyData UrlPropertyData = new LayerURLPropertyData() { Data = new Uri("https://netherlands3d.eu/docs/handleiding/imgs/lagen.main.bottom.full.png") };
        LayerPropertyData ILayerWithPropertyData.PropertyData => UrlPropertyData;

        public Sprite ImageSprite { get; private set; }
        public UnityEvent<Sprite> SpriteDownloaded = new();
        
        public void LoadProperties(List<LayerPropertyData> properties)
        {
            var urlProperty = (LayerURLPropertyData)properties.FirstOrDefault(p => p is LayerURLPropertyData);
            if (urlProperty != null)
            {
                UrlPropertyData = urlProperty; //take existing property to overwrite the unlinked one of this class
                StartCoroutine(GetLegendGraphics(urlProperty.Data));
            }
        }

        private IEnumerator GetLegendGraphics(Uri url)
        {
            UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(url);
            yield return webRequest.SendWebRequest();
            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                Texture texture = ((DownloadHandlerTexture)webRequest.downloadHandler).texture;
                Texture2D tex = texture as Texture2D;
                tex.Apply(false, true);
                ImageSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), Vector2.one * 0.5f, 100);
                SpriteDownloaded.Invoke(ImageSprite);
            }
        }
    }
}
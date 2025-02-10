using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class ImagePropertySection : MonoBehaviour
    {
        [SerializeField] private Image image;
        [SerializeField] private TMP_InputField inputField;
        private Coroutine activeCoroutine;
        
        private LayerWithImage controller;
        public  LayerWithImage Controller
        {
            get => controller;
            set
            {
                controller = value;
                var layerWithPropertyData = controller.UrlPropertyData;
                
                activeCoroutine = StartCoroutine(GetImage(controller.UrlPropertyData.Data));
                controller.UrlPropertyData.OnDataChanged.AddListener(UpdatePropertiesPanel);
                
                inputField.onEndEdit.AddListener(UpdateUrlInProjectData);
                inputField.text = layerWithPropertyData.Data.ToString();
                
                inputField.onEndEdit.Invoke(inputField.text);
            }
        }
        
        private void UpdatePropertiesPanel(Uri uri) //update uri if it was changed outside of the property panel
        {
            inputField.SetTextWithoutNotify(uri.ToString());
            DownloadImage(uri);
        }

        private void UpdateUrlInProjectData(string url)
        {
            if (!(url.StartsWith("http://") || url.StartsWith("https://")))
                url = "https://" + url;
            
            controller.UrlPropertyData.Data = new Uri(url);
        }
        
        private void DownloadImage(Uri newUrl)
        {
            if (activeCoroutine != null)
            {
                StopCoroutine(activeCoroutine);
            }

            activeCoroutine = StartCoroutine(GetImage(newUrl));
        }
        
        private IEnumerator GetImage(Uri url)
        {
            UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(url);
            yield return webRequest.SendWebRequest();
            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                Texture texture = ((DownloadHandlerTexture)webRequest.downloadHandler).texture;
                Texture2D tex = texture as Texture2D;
                tex.Apply(false, true);
                image.sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), Vector2.one * 0.5f, 100);
            }
            else
            {
                Debug.LogError(webRequest.error);
            }
        }
    }
}

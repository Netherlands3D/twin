using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.UI.LayerInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    public class Tile3DLayerPropertySection : MonoBehaviour
    {
        [SerializeField] private TMP_InputField urlInputField;
        [SerializeField] private Transform input;
        [SerializeField] private Transform errorMessage;
        [SerializeField] private Image colorFeedbackImage;
        [SerializeField] private Color defaultColor;
        [SerializeField] private Color warningColor;

        private Tile3DLayer2 layer;
        public Tile3DLayer2 Layer
        {
            get => layer;
            set
            {
                if(layer != null)
                {
                    layer.OnURLChanged.RemoveListener(DisplayURL);
                    layer.OnServerRequestFailed.RemoveListener(ShowServerWarningFeedback);
                }
                layer = value;

                if(layer != null)
                {
                    layer.OnURLChanged.AddListener(DisplayURL);
                    layer.OnServerRequestFailed.AddListener(ShowServerWarningFeedback);
                }

                urlInputField.text = layer.URL;
            }
        }

        public void DisplayURL(string url)
        {
            urlInputField.text = url;
        }

        public void ShowServerWarningFeedback(UnityWebRequest.Result webRequestResult)
        {
            if(webRequestResult == UnityWebRequest.Result.ConnectionError 
            || webRequestResult == UnityWebRequest.Result.ProtocolError 
            || webRequestResult == UnityWebRequest.Result.DataProcessingError )
            {
                colorFeedbackImage.color = warningColor;
            }
        }

        public void ApplyURL()
        {
            var sanitizedURL = SanitizeURL(urlInputField.text);
            urlInputField.text = sanitizedURL;

            //Make sure its long enough to contain a domain
            if (!IsValidURL(sanitizedURL))
            {
                colorFeedbackImage.color = warningColor;
                return;
            }        

            colorFeedbackImage.color = defaultColor;
            layer.URL = sanitizedURL;
        }

        private string SanitizeURL(string url)
        {
            //Append https:// if http:// or https:// is not present
            if (url.Length > 5 && !url.StartsWith("http://") && !url.StartsWith("https://"))
            {
                url = "https://" + url;
            }

            return url;
        }

        private bool IsValidURL(string url)
        {
            if(url.Length < 10)
            {
                return false;
            }

            return true;
        }
    }
}

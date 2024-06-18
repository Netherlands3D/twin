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

        private Tile3DLayer tile3DLayer;
        public Tile3DLayer Tile3DLayer
        {
            get => tile3DLayer;
            set
            {
                if(tile3DLayer != null)
                {
                    tile3DLayer.OnURLChanged.RemoveListener(DisplayURL);
                    tile3DLayer.OnServerResponseReceived.RemoveListener(ShowServerWarningFeedback);
                }
                tile3DLayer = value;

                if(tile3DLayer != null)
                {
                    tile3DLayer.OnURLChanged.AddListener(DisplayURL);
                    tile3DLayer.OnServerResponseReceived.AddListener(ShowServerWarningFeedback);
                }

                urlInputField.text = tile3DLayer.URL;
            }
        }

        public void DisplayURL(string url)
        {
            urlInputField.text = url;
        }

        public void ShowServerWarningFeedback(UnityWebRequest webRequest)
        {
            if(webRequest.ReturnedServerError())
            {
                colorFeedbackImage.color = warningColor;
                errorMessage.gameObject.SetActive(true);
                input.gameObject.SetActive(false);
            }
            else
            {
                errorMessage.gameObject.SetActive(false);
                input.gameObject.SetActive(true);
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
            tile3DLayer.URL = sanitizedURL;
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

using Netherlands3D.Twin.ExtensionMethods;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Netherlands3D.Functionalities.OGC3DTiles
{
    public class Tile3DLayerPropertySection : MonoBehaviour
    {
        [SerializeField] private TMP_InputField urlInputField;
        [SerializeField] private Transform input;
        [SerializeField] private Transform errorMessage;
        [SerializeField] private Image colorFeedbackImage;
        [SerializeField] private Color defaultColor;
        [SerializeField] private Color warningColor;

        private Tile3DLayerGameObject tile3DLayerGameObject;
        public Tile3DLayerGameObject Tile3DLayerGameObject
        {
            get => tile3DLayerGameObject;
            set
            {
                if(tile3DLayerGameObject != null)
                {
                    tile3DLayerGameObject.OnURLChanged.RemoveListener(DisplayURL);
                    tile3DLayerGameObject.OnServerResponseReceived.RemoveListener(ShowServerWarningFeedback);
                }
                tile3DLayerGameObject = value;

                if(tile3DLayerGameObject != null)
                {
                    tile3DLayerGameObject.OnURLChanged.AddListener(DisplayURL);
                    tile3DLayerGameObject.OnServerResponseReceived.AddListener(ShowServerWarningFeedback);
                }

                urlInputField.text = tile3DLayerGameObject.URL;
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
            tile3DLayerGameObject.URL = sanitizedURL;
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

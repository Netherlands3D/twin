using System;
using TMPro;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class ImagePropertySection : MonoBehaviour
    {
        [SerializeField] private TMP_Text urlButtonText;
        [SerializeField] private OpenURLInBrowser openURLInBrowser;
        [SerializeField] private TMP_InputField inputField;
        
        private LayerWithImage controller;
        public  LayerWithImage Controller
        {
            get => controller;
            set
            {
                controller = value;
                var layerWithPropertyData = controller.UrlPropertyData;
                
                controller.UrlPropertyData.OnDataChanged.AddListener(UpdatePropertiesPanel);
                inputField.onEndEdit.AddListener(UpdateUrlInProjectData);
                inputField.text = layerWithPropertyData.Data.ToString();
                print("init text "+inputField.text);
                inputField.onEndEdit.Invoke(inputField.text);
                
                // controller.s
            }
        }

        private void UpdatePropertiesPanel(Uri uri)
        {
            print(uri);
            
            inputField.SetTextWithoutNotify(uri.ToString());
            openURLInBrowser.UrlToOpen = uri.ToString();
            urlButtonText.text = uri.ToString();
        }

        private void UpdateUrlInProjectData(string url)
        {
            print("text field url " + url);
            if (!(url.StartsWith("http://") || url.StartsWith("https://")))
                url = "https://" + url;
            
            print("new url" + url);
            controller.UrlPropertyData.Data = new Uri(url);
        }
    }
}

using System;
using TMPro;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class GoToUrlPropertySection : MonoBehaviour
    {
        [SerializeField] private TMP_Text urlButtonText;
        [SerializeField] private OpenURLInBrowser openURLInBrowser;
        [SerializeField] private TMP_InputField inputField;
        
        private LayerWithUrl controller;
        public  LayerWithUrl Controller
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

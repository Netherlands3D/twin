using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Tiles3D;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Netherlands3D.Twin.Layers
{
    public class Tile3DLayerGameObject : LayerGameObject, ILayerWithPropertyData, ILayerWithPropertyPanels, ILayerWithCredentials
    {
        private Read3DTileset tileSet;
        [SerializeField] private bool usePropertySections = true;
        [SerializeField] private bool openPropertiesOnStart = true;
        private List<IPropertySectionInstantiator> propertySections = new();
        
        private UnityEvent<string> onURLChanged = new();
        public UnityEvent<string> OnURLChanged { get => onURLChanged; }
        public UnityEvent<string> UnsupportedExtensionsMessage;
        public UnityEvent<UnityWebRequest> OnServerResponseReceived { get => tileSet.OnServerResponseReceived;  }

        private Tile3DLayerPropertyData propertyData;
        public LayerPropertyData PropertyData => propertyData;

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            var tile3DLayerPropertyData = (Tile3DLayerPropertyData)properties.FirstOrDefault(p => p is Tile3DLayerPropertyData);
            if (tile3DLayerPropertyData != null)
            {
                this.propertyData = tile3DLayerPropertyData;

                // if this game object is configured not to use property sections, we can assume
                // a URL came preconfigured, and we ignore anything coming from the properties
                // so that we always have the most recent version of the layer's data
                if (usePropertySections)
                {
                    URL = propertyData.Url.ToString();
                }
            }
        }

        public string URL
        {
            get => StripUrlOfQueryString(tileSet.tilesetUrl);
            set => OnURLChanged.Invoke(tileSet.tilesetUrl);
        }

        private string StripUrlOfQueryString(string value)
        {
            if(!value.Contains("?"))
                return value;

            var uriBuilder = new UriBuilder(value);
            uriBuilder.Query = "";

            var urlWithoutQuery = uriBuilder.Uri.ToString();
            return urlWithoutQuery;
        }

        /// <summary>
        /// Updates the Url without calling the OnURLChanged event, that is directly tied to the
        /// URL property. This method can be used for internal processes, such as setting properties
        /// without causing a loop.
        /// </summary>
        /// <param name="url"></param>
        private void UpdateUrl(Uri url)
        {
            //Always query parameters (tileset key's must be set via our credentials system)
            string urlWithoutQuery = StripUrlOfQueryString(url.ToString());

            tileSet.tilesetUrl = urlWithoutQuery;
            
            EnableTileset();
        }

        private void EnableTileset()
        {
            if(!tileSet.enabled)
                tileSet.enabled = true;
            else
                tileSet.RefreshTiles();
        }

        private CredentialsPropertySection propertySection;
        public CredentialsPropertySection PropertySection {
            get => propertySection;
            set => propertySection = value;
        }

        protected void Awake()
        {
            tileSet = GetComponent<Read3DTileset>();
            propertyData = new Tile3DLayerPropertyData(null);

            if (usePropertySections)
            {
                // only store the URL when we actually use property sections -and thus this
                // layer is configurable- otherwise we ignore that a URL is stored and we assume
                // it to be a pre-configured, and thus possibly changing, url.
                propertyData.Url = new Uri(URL);
                
                propertySections = GetComponents<IPropertySectionInstantiator>().ToList();
            }
        }

        protected override void Start()
        {
            base.Start();

            // This is done by design, so that when the property's URL is changed outside of
            // this layer, that it will update even if this layer is currently inactive.
            propertyData.OnUrlChanged.AddListener(UpdateUrl);
            
            // The URL property on this game object's only responsibility is to set the URI in the
            // propety data. The events invoked in the property data is then responsible for making
            // sure all visualisations are applied, and thus the UpdateURL function is called
            // whenever that changes.
            this.OnURLChanged.AddListener((string url) => propertyData.Url = new Uri(url));
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            tileSet.unsupportedExtensionsParsed.AddListener(InvokeUnsupportedExtensionsMessage);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            tileSet.unsupportedExtensionsParsed.RemoveListener(InvokeUnsupportedExtensionsMessage);
        }
        
        public override void OnLayerActiveInHierarchyChanged(bool isActive)
        {
            gameObject.SetActive(isActive);
        }

        private void InvokeUnsupportedExtensionsMessage(string[] unsupportedExtensions)
        {
            if (unsupportedExtensions.Length == 0)
                return;

            string message = Name + " contains the following unsupported extensions: ";
            foreach (var extension in unsupportedExtensions)
            {
                message += "\n"+ extension;
            }
            UnsupportedExtensionsMessage.Invoke(message);
        }

        public List<IPropertySectionInstantiator> GetPropertySections()
        {
            return propertySections;
        }

        public void SetCredentials(string username, string password)
        {
            tileSet.AddCustomHeader("Authorization", "Basic " + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(username + ":" + password)), true);
            tileSet.RefreshTiles();
        }

        public void SetKey(string key)
        {
            tileSet.personalKey = key;
            tileSet.publicKey = key;
            
            tileSet.QueryKeyName = "key";
            tileSet.RefreshTiles();
        }

        public void SetBearerToken(string token)
        {
            tileSet.AddCustomHeader("Authorization", "Bearer " + token);
            tileSet.RefreshTiles();
        }
        
        public void SetCode(string code)
        {
            tileSet.personalKey = code;
            tileSet.publicKey = code;
            tileSet.QueryKeyName = "code";
            tileSet.RefreshTiles();
        }

        public void SetToken(string token)
        {
            tileSet.personalKey = token;
            tileSet.publicKey = token;
            tileSet.QueryKeyName = "token";
            tileSet.RefreshTiles();
        }

        public void ClearCredentials()
        {
            tileSet.personalKey = "";
            tileSet.publicKey = "";
            tileSet.QueryKeyName = "key";
            tileSet.RefreshTiles();
        }
    }
}
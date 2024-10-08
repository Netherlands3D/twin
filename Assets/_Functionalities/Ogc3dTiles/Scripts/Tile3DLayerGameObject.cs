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
        private List<IPropertySectionInstantiator> propertySections = new();

        private Tile3DLayerPropertyData urlPropertyData;
        LayerPropertyData ILayerWithPropertyData.PropertyData => urlPropertyData;
        public UnityEvent<string> OnURLChanged => urlPropertyData.OnUrlChanged;
        public UnityEvent<string> UnsupportedExtensionsMessage;
        public UnityEvent<UnityWebRequest> OnServerResponseReceived => tileSet.OnServerResponseReceived;

        public string URL
        {
            get => urlPropertyData.Url;
            set
            {
                //Always query parameters (tileset key's must be set via our credentials system)
                string urlWithoutQuery = TilesetURLWithoutQuery(value);
                urlPropertyData.Url = urlWithoutQuery;
            }
        }

        private string TilesetURLWithoutQuery(string value)
        {
            if (!value.Contains("?"))
                return value;

            var uriBuilder = new UriBuilder(value);
            uriBuilder.Query = "";

            var urlWithoutQuery = uriBuilder.Uri.ToString();
            return urlWithoutQuery;
        }

        private void EnableTileset()
        {
            if (!tileSet.enabled)
                tileSet.enabled = true;
            else
                tileSet.RefreshTiles();
        }

        private CredentialsPropertySection propertySection;

        public CredentialsPropertySection PropertySection
        {
            get => propertySection;
            set => propertySection = value;
        }

        protected void Awake()
        {
            tileSet = GetComponent<Read3DTileset>();
            urlPropertyData = new Tile3DLayerPropertyData(TilesetURLWithoutQuery(tileSet.tilesetUrl));

            if (usePropertySections)
                propertySections = GetComponents<IPropertySectionInstantiator>().ToList();
            else
                propertySections = new();
        }

        private void OnEnable()
        {
            tileSet.unsupportedExtensionsParsed.AddListener(InvokeUnsupportedExtensionsMessage);
        }

        private void OnDisable()
        {
            tileSet.unsupportedExtensionsParsed.RemoveListener(InvokeUnsupportedExtensionsMessage);
        }

        protected override void Start()
        {
            //listen to property changes in start and OnDestroy because the object should still update its transform even when disabled
            urlPropertyData.OnUrlChanged.AddListener(UpdateURL);
        }

        private void UpdateURL(string urlWithoutQuery)
        {
            tileSet.tilesetUrl = urlWithoutQuery;
            EnableTileset();
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
                message += "\n" + extension;
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

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            var urlProperty = (Tile3DLayerPropertyData)properties.FirstOrDefault(p => p is Tile3DLayerPropertyData);
            if (urlProperty != null)
            {
                URL = urlProperty.Url;
                UpdateURL(urlProperty.Url);
            }
        }

        private void OnDestroy()
        {
            urlPropertyData.OnUrlChanged.RemoveListener(UpdateURL);
        }
    }
}
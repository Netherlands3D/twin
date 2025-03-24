using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Credentials;
using Netherlands3D.Credentials.StoredAuthorization;
using Netherlands3D.Tiles3D;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Utility;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Netherlands3D.Functionalities.OGC3DTiles
{
    [RequireComponent(typeof(ReadSubtree))]
    [RequireComponent(typeof(Read3DTileset))]
    public class Tile3DLayerGameObject : LayerGameObject, ILayerWithPropertyData, ILayerWithPropertyPanels
    {
        public override BoundingBox Bounds => tileSet.root != null ? new BoundingBox(tileSet.root.BottomLeft, tileSet.root.TopRight) : null;
        public Tile3DLayerPropertyData PropertyData => urlPropertyData;

        private Read3DTileset tileSet;
        [SerializeField] private bool usePropertySections = true;
        private List<IPropertySectionInstantiator> propertySections = new();

        private Tile3DLayerPropertyData urlPropertyData;
        LayerPropertyData ILayerWithPropertyData.PropertyData => urlPropertyData;
        
        [Obsolete("this is a temporary fix to apply credentials to the 3d Tiles package. this should go through the ICredentialHandler instead")]
        public UnityEvent<Uri> OnURLChanged => urlPropertyData.OnUrlChanged;
        public UnityEvent<string> UnsupportedExtensionsMessage;
        
        [Obsolete("this is a temporary fix to apply credentials to the 3d Tiles package. this should go through the ICredentialHandler instead")]
        public UnityEvent<UnityWebRequest> OnServerResponseReceived => tileSet.OnServerResponseReceived;

        private ICredentialHandlerPanel credentialHandlerPanel;
        
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
        
        protected void Awake()
        {
            tileSet = GetComponent<Read3DTileset>();
            
            credentialHandlerPanel = GetComponent<ICredentialHandlerPanel>();
            if(!string.IsNullOrEmpty(tileSet.tilesetUrl))
                credentialHandlerPanel.BaseUri = new Uri(tileSet.tilesetUrl); //apply the URL from what is serialized in the tileset component.
            
            credentialHandlerPanel.OnAuthorizationHandled.AddListener(HandleCredentials);
            urlPropertyData = new Tile3DLayerPropertyData(TilesetURLWithoutQuery(tileSet.tilesetUrl));
            //listen to property changes in start and OnDestroy because the object should still update its transform even when disabled
            urlPropertyData.OnUrlChanged.AddListener(UpdateURL);

            if (usePropertySections)
                propertySections = GetComponents<IPropertySectionInstantiator>().ToList();
            else
                propertySections = new();
        }

        private void HandleCredentials(StoredAuthorization auth)
        {
            ClearCredentials();
            
            if (auth is HeaderBasedAuthorization headerBasedAuthorization)
            {
                var (headerName, headerValue) = headerBasedAuthorization.GetHeaderKeyAndValue();
                tileSet.AddCustomHeader(headerName, headerValue, true);
                tileSet.RefreshTiles();
                return;
            }
            
            if (auth is QueryStringAuthorization inferableSingleKey)
            {
                tileSet.personalKey = inferableSingleKey.Key;
                tileSet.publicKey = inferableSingleKey.Key;
                tileSet.QueryKeyName = inferableSingleKey.QueryKeyName;
                tileSet.RefreshTiles();
                return;
            }

            throw new NotImplementedException("Authorization type " + auth.GetType() + " is not implemented in " + GetType());
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            tileSet.unsupportedExtensionsParsed.AddListener(InvokeUnsupportedExtensionsMessage);
            OnServerResponseReceived.AddListener(ProcessServerResponse);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            tileSet.unsupportedExtensionsParsed.RemoveListener(InvokeUnsupportedExtensionsMessage);
            OnServerResponseReceived.RemoveListener(ProcessServerResponse);
        }

        private void ProcessServerResponse(UnityWebRequest request)
        {
            LayerData.HasValidCredentials = request.result == UnityWebRequest.Result.Success;
        }

        private void UpdateURL(Uri urlWithoutQuery)
        {
            credentialHandlerPanel.BaseUri = urlWithoutQuery; //apply the URL from what is stored in the Project data
            tileSet.tilesetUrl = urlWithoutQuery.ToString();
            credentialHandlerPanel.ApplyCredentials();
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
        
        public void ClearCredentials()
        {
            tileSet.personalKey = "";
            tileSet.publicKey = "";
            tileSet.QueryKeyName = "key";
            tileSet.ClearKeyFromURL();
            tileSet.RefreshTiles();
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            var urlProperty = (Tile3DLayerPropertyData)properties.FirstOrDefault(p => p is Tile3DLayerPropertyData);
            if (urlProperty != null)
            {
                urlPropertyData = urlProperty; //use existing object to overwrite the current instance
                UpdateURL(new Uri(urlProperty.Url));
            }
        }

        private void OnDestroy()
        {
            urlPropertyData.OnUrlChanged.RemoveListener(UpdateURL);
        }
    }
}
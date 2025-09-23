using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Credentials;
using Netherlands3D.Credentials.StoredAuthorization;
using Netherlands3D.Services;
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
        public override BoundingBox Bounds => TileSet.root != null ? new BoundingBox(TileSet.root.BottomLeft, TileSet.root.TopRight) : null;
        public Tile3DLayerPropertyData PropertyData => tile3DPropertyData;

        private Read3DTileset tileSet;
        private Read3DTileset TileSet => GetAndCacheComponent(ref tileSet);
        
        [SerializeField] private bool usePropertySections = true;
        private List<IPropertySectionInstantiator> propertySections = new();

        private Tile3DLayerPropertyData tile3DPropertyData => LayerData.GetProperty<Tile3DLayerPropertyData>();
        LayerPropertyData ILayerWithPropertyData.PropertyData => tile3DPropertyData;

        [Obsolete("this is a temporary fix to apply credentials to the 3d Tiles package. this should go through the ICredentialHandler instead")]
        public UnityEvent<Uri> OnURLChanged => tile3DPropertyData.OnUrlChanged;

        private ICredentialHandler credentialHandler;
        private ICredentialHandler CredentialHandler => GetAndCacheComponent(ref credentialHandler);
        
        public UnityEvent<string> UnsupportedExtensionsMessage;

        private void EnableTileset()
        {
            if (!TileSet.enabled)
                TileSet.enabled = true;
            else
                TileSet.RefreshTiles();
        }

        protected override void OnLayerInitialize()
        {
            if (tile3DPropertyData == null)
            {
                LayerData.SetProperty(new Tile3DLayerPropertyData(TileSet.tilesetUrl));
            }
            CredentialHandler.OnAuthorizationHandled.AddListener(HandleCredentials);
            
            // listen to property changes in start and OnDestroy because the object should still update its transform even when disabled
            tile3DPropertyData.OnUrlChanged.AddListener(UpdateURL);
            tile3DPropertyData.OnCRSChanged.AddListener(UpdateCRS);
            
            propertySections = usePropertySections 
                ? GetComponents<IPropertySectionInstantiator>().ToList() 
                : new();
        }

        private void HandleCredentials(Uri uri, StoredAuthorization auth)
        {
            ClearCredentials();

            switch (auth) //todo: pass auth.GetConfig to the tileset instead of this switch statement.
            {
                case FailedOrUnsupported:
                    LayerData.HasValidCredentials = false;
                    TileSet.enabled = false;
                    return;
                case HeaderBasedAuthorization headerBasedAuthorization:
                    var (headerName, headerValue) = headerBasedAuthorization.GetHeaderKeyAndValue();
                    TileSet.AddCustomHeader(headerName, headerValue, true);
                    break;
                case QueryStringAuthorization queryStringAuthorization:
                    TileSet.personalKey = queryStringAuthorization.QueryKeyValue;
                    TileSet.publicKey = queryStringAuthorization.QueryKeyValue;
                    TileSet.QueryKeyName = queryStringAuthorization.QueryKeyName;
                    break;
                case Public:
                    break; //nothing specific needed, but it needs to be excluded from default
                default:
                    throw new NotImplementedException("Credential type " + auth.GetType() + " is not supported by " + GetType());
            }

            //also do this for public
            LayerData.HasValidCredentials = true;
            TileSet.RefreshTiles();
            TileSet.enabled = true;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            TileSet.unsupportedExtensionsParsed.AddListener(InvokeUnsupportedExtensionsMessage);
            TileSet.OnServerResponseReceived.AddListener(ProcessServerResponse);
            TileSet.OnTileLoaded.AddListener(InitializeStyling);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            TileSet.unsupportedExtensionsParsed.RemoveListener(InvokeUnsupportedExtensionsMessage);
            TileSet.OnServerResponseReceived.RemoveListener(ProcessServerResponse);
            TileSet.OnTileLoaded.RemoveListener(InitializeStyling);
        }

        private void InitializeStyling(Content content)
        {
            var bitmask = LayerData.DefaultSymbolizer.GetMaskLayerMask();
            
            if (bitmask == null)
                bitmask = LayerGameObject.DEFAULT_MASK_BIT_MASK; 
            
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                UpdateBitMaskForMaterials(bitmask.Value, r.materials);
            }
        }

        protected override void OnLayerReady()
        {
            if (string.IsNullOrEmpty(tile3DPropertyData.Url) && !string.IsNullOrEmpty(TileSet.tilesetUrl)) //if we are making a new layer, we should take the serialized url from the tileset if it exists.
            {
                UpdateURL(new Uri(TileSet.tilesetUrl));
            }
            else
            {
                UpdateURL(new Uri(tile3DPropertyData.Url));
            }
            UpdateCRS(tile3DPropertyData.ContentCRS);
            ServiceLocator.GetService<Tile3DLayerSet>().Attach(this);
        }

        private void ProcessServerResponse(UnityWebRequest request)
        {
            LayerData.HasValidCredentials = request.result == UnityWebRequest.Result.Success;
        }

        private void UpdateURL(Uri storedUri)
        {
            CredentialHandler.Uri = storedUri; //apply the URL from what is stored in the Project data
            TileSet.tilesetUrl = storedUri.ToString();
            CredentialHandler.ApplyCredentials();
            EnableTileset();
        }

        private void UpdateCRS(int crs)
        {
            TileSet.SetCoordinateSystem((Coordinates.CoordinateSystem)crs);
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
            TileSet.personalKey = "";
            TileSet.publicKey = "";
            TileSet.QueryKeyName = "key";
            TileSet.ClearKeyFromURL();
            TileSet.RefreshTiles();
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            UpdateCRS(tile3DPropertyData.ContentCRS);
        }

        protected override void OnDestroy()
        {
            tile3DPropertyData.OnUrlChanged.RemoveListener(UpdateURL);
            tile3DPropertyData.OnCRSChanged.RemoveListener(UpdateCRS);
        }
    }
}
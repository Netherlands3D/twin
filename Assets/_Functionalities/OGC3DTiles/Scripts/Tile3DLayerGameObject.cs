using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Coordinates;
using Netherlands3D.Credentials;
using Netherlands3D.Credentials.StoredAuthorization;
using Netherlands3D.Services;
using Netherlands3D.Tiles3D;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Utility;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Netherlands3D.Functionalities.OGC3DTiles
{
    [RequireComponent(typeof(ReadSubtree))]
    [RequireComponent(typeof(Read3DTileset))]
    public class Tile3DLayerGameObject : LayerGameObject, IVisualizationWithPropertyData //, ILayerWithPropertyPanels
    {
        public override BoundingBox Bounds => TileSet.root != null ? new BoundingBox(TileSet.root.BottomLeft, TileSet.root.TopRight) : null;

        private Read3DTileset tileSet;
        private Read3DTileset TileSet => GetAndCacheComponent(ref tileSet);

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
            CredentialHandler.OnAuthorizationHandled.AddListener(HandleCredentials);
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
            var tile3DPropertyData = LayerData.GetProperty<Tile3DLayerPropertyData>();
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
            var tile3DPropertyData = properties.Get<Tile3DLayerPropertyData>();
            if (tile3DPropertyData == null)
            {
                tile3DPropertyData = new Tile3DLayerPropertyData(TileSet.tilesetUrl);
                LayerData.SetProperty(tile3DPropertyData);
            }

            UpdateURL(new Uri(tile3DPropertyData.Url));
            UpdateCRS(tile3DPropertyData.ContentCRS);
        }
        
        protected override void RegisterEventListeners()
        {
            base.RegisterEventListeners();
            var tile3DPropertyData = LayerData.GetProperty<Tile3DLayerPropertyData>();
            tile3DPropertyData.OnUrlChanged.AddListener(UpdateURL);
            tile3DPropertyData.OnCRSChanged.AddListener(UpdateCRS);
        }

        protected override void OnDestroy()
        {
            var tile3DPropertyData = LayerData.GetProperty<Tile3DLayerPropertyData>();
            tile3DPropertyData.OnUrlChanged.RemoveListener(UpdateURL);
            tile3DPropertyData.OnCRSChanged.RemoveListener(UpdateCRS);
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KindMen.Uxios;
using Netherlands3D.CartesianTiles;
using Netherlands3D.Coordinates;
using Netherlands3D.Credentials.StoredAuthorization;
using Netherlands3D.Functionalities.Wms;
using Netherlands3D.Twin.Utility;
using Newtonsoft.Json;
using PureHDF;
using UnityEngine;

namespace Netherlands3D.Functionalities.NetCDF
{
    public class WCSTileDataLayer : ImageProjectionLayer
    {
        private const string DefaultEpsgCoordinateSystem = "28992";

        private Config requestConfig { get; set; } = Config.Default();
      

        private string _url = "";

        public string Url
        {
            get => _url;
            set
            {
                _url = value;
                if (!_url.Contains("{0}"))
                    Debug.LogError("WFS URL does not contain a '{0}' placeholder for the bounding box.", gameObject);
            }
        }

        private void Awake()
        {
            //Make sure Datasets at least has one item
            if (Datasets.Count == 0)
            {
                var baseDataset = new DataSet()
                {
                    maximumDistance = 3000,
                    maximumDistanceSquared = 1000 * 1000
                };
                Datasets.Add(baseDataset);
            }
        }
        
        public void SetAuthorization(StoredAuthorization auth)
        {
            ClearConfig();
            requestConfig = auth.AddToConfig(requestConfig);
        }

        public void ClearConfig()
        {
            requestConfig = new Config();
        }

        

        protected override IEnumerator DownloadDataAndGenerateTexture(TileChange tileChange, Action<TileChange> callback = null)
        {
            
            // var getCapabilitiesString = OgcWebServicesUtility.CreateGetCapabilitiesURL(wmsProjectionLayer.WmsUrl, ServiceType.Wms);
            // var getCapabilitiesUrl = new Uri(getCapabilitiesString);
            // BoundingBoxCache.Instance.GetBoundingBoxContainer(
            //     getCapabilitiesUrl,
            //     auth,
            //     (responseText) => new WmsGetCapabilities(getCapabilitiesUrl, responseText),
            //     SetBoundingBox
            // );
            
            var tileKey = new Vector2Int(tileChange.X, tileChange.Y);
            if (!tiles.ContainsKey(tileKey))
            {
                yield break;
            }

            //on loading project form save file this can be empty 
            if (string.IsNullOrEmpty(Url)) yield break;

            var mapData = MapFilters.FromUrlWCS(new Uri(Url));

            var boundingBox = DetermineBoundingBox(tileChange, mapData);
            string url = Url.Replace("{0}", boundingBox.ToString());

            // Because requestConfig is by-ref, changing it will change all requests in flight; as such we clone the config before
            // assigning a payload
            var configWithPayload = Config.BasedOn(requestConfig);
            configWithPayload = configWithPayload.WithPayload(new WCSTileDataLayerChangePayload(tileChange, url));

            var promise = Uxios.DefaultInstance.Get<Texture2D>(new Uri(url), configWithPayload);
            promise.Then(OnDownloadedTexture);
            promise.Catch(OnFailedToDownloadTexture);

            yield return Uxios.WaitForRequest(promise);
            callback?.Invoke(tileChange);
        }

        private void OnDownloadedTexture(IResponse response)
        {
            var payload = response.Config.GetPayload<WCSTileDataLayerChangePayload>();
            var tileKey = payload.TileKey;
            byte[] data = response.Data as byte[];
            Debug.Log(data);
        }
        
        private void OnFailedToDownloadTexture(Exception exception)
        {
            // An unknown exception occurred - log the outcome and don't do much since we don't know anything about
            // it. This should not occur in normal operation.
            if (exception is not Error uxiosError)
            {
                Debug.LogException(exception);
                return;
            }

            var payload = uxiosError.Config.GetPayload<WCSTileDataLayerChangePayload>();

            Debug.LogError($"Could not download {payload.Url}: " + exception.Message);
            RemoveGameObjectFromTile(payload.TileKey);
        }
        
        private BoundingBox DetermineBoundingBox(TileChange tileChange, MapFilters mapFilters)
        {
            var bottomLeft = new Coordinate(CoordinateSystem.RD, tileChange.X, tileChange.Y, 0);
            var topRight = new Coordinate(CoordinateSystem.RD, tileChange.X + tileSize, tileChange.Y + tileSize, 0);

            // Yes, there is a semicolon missing, this is on purpose because FindCoordinateSystem finds this and not 
            // with the semicolon
            string coordinateSystemAsString = "CRS84";
            if (mapFilters.spatialReference != "CRS:84")
            {
                var splitReferenceCode = mapFilters.spatialReference.Split(':');
                coordinateSystemAsString = splitReferenceCode[0].ToLower() == "epsg"
                    ? splitReferenceCode[^1]
                    : DefaultEpsgCoordinateSystem;
            }

            CoordinateSystem foundCoordinateSystem = CoordinateSystems.FindCoordinateSystem(coordinateSystemAsString);

            var boundingBox = new BoundingBox(bottomLeft, topRight);
            boundingBox.Convert(foundCoordinateSystem);

            return boundingBox;
        }

        public record WCSTileDataLayerChangePayload(TileChange TileChange, string Url)
        {
            public TileChange TileChange { get; } = TileChange;
            public string Url  { get; } = Url;
            public Vector2Int TileKey => new(TileChange.X, TileChange.Y);
        }
        
        
        protected void RemoveGameObjectFromTile(Vector2Int tileKey)
        {
            if (tiles.ContainsKey(tileKey))
            {
                Tile tile = tiles[tileKey];
                if (tile == null)
                {
                    return;
                }

                if (tile.gameObject == null)
                {
                    return;
                }

                Destroy(tile.gameObject);
            }
        }

        private string GetAPIKey()
        {
            return
                "eyJvcmciOiI1ZTU1NGUxOTI3NGE5NjAwMDEyYTNlYjEiLCJpZCI6IjRjZGY2M2FjYTk1MjRiMzU4N2UwZTNjMjM5NWZlMTlmIiwiaCI6Im11cm11cjEyOCJ9";
        }
        
        public record NetCDFTileDataLayerChangePayload(TileChange TileChange, string Url)
        {
            public TileChange TileChange { get; } = TileChange;
            public string Url  { get; } = Url;
            public Vector2Int TileKey => new(TileChange.X, TileChange.Y);
        }
    }
}
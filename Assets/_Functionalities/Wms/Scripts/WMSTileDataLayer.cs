using System;
using System.Collections;
using System.Collections.Generic;
using KindMen.Uxios;
using Netherlands3D.CartesianTiles;
using Netherlands3D.Coordinates;
using Netherlands3D.Credentials.StoredAuthorization;
using Netherlands3D.Twin.Utility;
using UnityEngine;

namespace Netherlands3D.Functionalities.Wms
{
    public class WMSTileDataLayer : ImageProjectionLayer
    {
        private const string DefaultEpsgCoordinateSystem = "28992";

        private Config requestConfig { get; set; } = new();

        public int RenderIndex
        {
            get => renderIndex;
            set
            {
                int oldIndex = renderIndex;
                renderIndex = value;
                if (oldIndex != renderIndex)
                    UpdateDrawOrderForChildren();
            }
        }

        private int renderIndex = -1;

        private string wmsUrl = "";

        public string WmsUrl
        {
            get => wmsUrl;
            set
            {
                wmsUrl = value;
                if (!wmsUrl.Contains("{0}"))
                    Debug.LogError("WMS URL does not contain a '{0}' placeholder for the bounding box.", gameObject);
            }
        }

        private void Awake()
        {
            //Make sure Datasets at least has one item
            if (Datasets.Count == 0)
            {
                var baseDataset = new DataSet()
                {
                    maximumDistance = 6000,
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
            var tileKey = new Vector2Int(tileChange.X, tileChange.Y);
            if (!tiles.ContainsKey(tileKey))
            {
                onLogMessage.Invoke(LogType.Warning, "Tile key does not exist");
                yield break;
            }

            //on loading project form save file this can be empty 
            if (string.IsNullOrEmpty(wmsUrl)) yield break;

            var mapData = MapFilters.FromUrl(new Uri(wmsUrl));

            var boundingBox = DetermineBoundingBox(tileChange, mapData);
            string url = wmsUrl.Replace("{0}", boundingBox.ToString());
            
            var promise = Uxios.DefaultInstance.Get<Texture2D>(new Uri(url), requestConfig);
            promise.Then(response => OnDownloadedTexture(tileKey, response.Data as Texture2D));
            promise.Catch(exception => OnFailedToDownloadTexture(url, exception, tileKey));
            promise.Finally(() => callback?.Invoke(tileChange)); // Always issue the callback

            yield return Uxios.WaitForRequest(promise);
        }

        private void OnDownloadedTexture(Vector2Int tileKey, Texture2D tex)
        {
            if (!tex)
            {
                onLogMessage.Invoke(LogType.Warning, $"Texture could not load for tile '{tileKey.x},{tileKey.y}'");
                return;
            }

            if (!tiles.TryGetValue(tileKey, out var tile))
            {
                onLogMessage.Invoke(LogType.Warning, $"Tile '{tileKey.x},{tileKey.y}' has been cleaned up in the mean time, cancelling rendering");
                Destroy(tex);
                tex = null; // extra null-setting to make sure any managed shell is cleaned up
                return;
            }

            ClearPreviousTexture(tile);
                    
            if (!tile.gameObject.TryGetComponent<TextureProjectorBase>(out var projector))
            {
                Destroy(tex);
                tex = null; // extra null-setting to make sure any managed shell is cleaned up
                return;
            }
                    
            tex.name = tile.tileKey.ToString();
            projector.Project(tex, tileSize, ProjectorHeight, renderIndex, ProjectorMinDepth, isEnabled);
        }

        private void OnFailedToDownloadTexture(string url, Exception exception, Vector2Int tileKey)
        {
            Debug.LogError($"Could not download {url}: " + exception.Message);
            RemoveGameObjectFromTile(tileKey);
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

        private void UpdateDrawOrderForChildren()
        {
            foreach (KeyValuePair<Vector2Int, Tile> tile in tiles)
            {
                if (tile.Value == null || tile.Value.gameObject == null)
                    continue;

                TextureDecalProjector projector = tile.Value.gameObject.GetComponent<TextureDecalProjector>();
                projector.SetPriority(renderIndex);
            }
        }
    }
}
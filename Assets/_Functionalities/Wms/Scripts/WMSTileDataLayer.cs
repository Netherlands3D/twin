using System;
using System.Collections;
using System.Collections.Generic;
using KindMen.Uxios;
using Netherlands3D.CartesianTiles;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Utility;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Netherlands3D.Functionalities.Wms
{
    public class WMSTileDataLayer : ImageProjectionLayer
    {
        private const string DefaultEpsgCoordinateSystem = "28992";
        private Config requestConfig { get; set; }

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
                    maximumDistance = 3000,
                    maximumDistanceSquared = 1000 * 1000
                };
                Datasets.Add(baseDataset);
            }
        }

        public void SetConfig(Config config)
        {
            requestConfig = config;
            config.TypeOfResponseType = ExpectedTypeOfResponse.Texture(true);
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
            if (string.IsNullOrEmpty(wmsUrl))
                yield break;

            var mapData = MapFilters.FromUrl(new Uri(wmsUrl));
            Tile tile = tiles[tileKey];

            var boundingBox = DetermineBoundingBox(tileChange, mapData);
            string url = wmsUrl.Replace("{0}", boundingBox.ToString());
            var promise = Uxios.DefaultInstance.Get<Texture2D>(new Uri(url), requestConfig);

            promise.Then(response =>
                {
                    ClearPreviousTexture(tile);
                    Texture2D tex = response.Data as Texture2D;
                    tex.name = tile.tileKey.ToString();
                    tex.Compress(true);
                    tex.filterMode = FilterMode.Bilinear;
                    tex.Apply(false, true);

                    if (!tile.gameObject.TryGetComponent<TextureProjectorBase>(out var projector))
                    {
                        Destroy(tex);
                        return;
                    }

                    projector.SetSize(tileSize, tileSize, tileSize);
                    projector.gameObject.SetActive(isEnabled);
                    projector.SetTexture(tex);

                    //force the depth to be at least larger than its height to prevent z-fighting
                    DecalProjector decalProjector = tile.gameObject.GetComponent<DecalProjector>();
                    TextureDecalProjector textureDecalProjector = tile.gameObject.GetComponent<TextureDecalProjector>();
                    if (ProjectorHeight >= decalProjector.size.z)
                        textureDecalProjector.SetSize(decalProjector.size.x, decalProjector.size.y, ProjectorMinDepth);

                    //set the render index, to make sure the render order is maintained
                    textureDecalProjector.SetPriority(renderIndex);
                }
            );

            promise.Catch(exception =>
            {
                Debug.LogError($"Could not download {url}: " + exception.Message);
                RemoveGameObjectFromTile(tileKey);
            });

            // Always issue the callback
            promise.Finally(() => { callback(tileChange); });

            yield return Uxios.WaitForRequest(promise);
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

            CoordinateSystems.FindCoordinateSystem(coordinateSystemAsString, out var foundCoordinateSystem);

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
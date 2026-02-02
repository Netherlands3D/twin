using System.Collections;
using UnityEngine;
using Netherlands3D.CartesianTiles;
using System;
using Netherlands3D.Coordinates;
using KindMen.Uxios;
using Netherlands3D.Credentials.StoredAuthorization;
using Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers;
using Netherlands3D.Twin.Utility;

namespace Netherlands3D.Functionalities.Wfs
{
    /// <summary>
    /// A custom CartesianTile layer that uses the cartesian tiling system to 'stream' parts of 
    /// a WFS service to the client using the 'bbox' parameter.
    /// The Twin GeoJSONLayer is used to render the GeoJSON data.
    /// </summary>
    public class WFSGeoJSONTileDataLayer : Layer
    {
        private const CoordinateSystem DefaultEpsgCoordinateSystem = CoordinateSystem.RD;
        private Netherlands3D.CartesianTiles.TileHandler tileHandler;
        private Config requestConfig { get; set; } = Config.Default();

        private BoundingBox boundingBox;

        public BoundingBox BoundingBox
        {
            get => boundingBox;
            set
            {
                boundingBox = value;
                var crs2D = CoordinateSystems.To2D(value.CoordinateSystem);
                boundingBox.Convert(crs2D); //remove the height, since a GeoJSON is always 2D. This is needed to make the centering work correctly
            }
        }

        private string wfsUrl = "";

        public string WfsUrl
        {
            get => wfsUrl;
            set
            {
                wfsUrl = value;
                if (!wfsUrl.Contains("{0}"))
                    Debug.LogError("WFS URL does not contain a '{0}' placeholder for the bounding box.", gameObject);
            }
        }

        private GeoJsonLayerGameObject wfsGeoJSONLayer;

        public GeoJsonLayerGameObject WFSGeoJSONLayer
        {
            get => wfsGeoJSONLayer;
            set
            {
                if (wfsGeoJSONLayer != null)
                    wfsGeoJSONLayer.LayerData.LayerDestroyed.RemoveListener(OnGeoJSONLayerDestroyed);

                wfsGeoJSONLayer = value;
                wfsGeoJSONLayer.LayerData.LayerDestroyed.AddListener(OnGeoJSONLayerDestroyed);
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

            StartCoroutine(FindTileHandler());
        }

        private IEnumerator FindTileHandler()
        {
            yield return null;

            //Find a required TileHandler in our parent, or else in the scene
            tileHandler = GetComponentInParent<Netherlands3D.CartesianTiles.TileHandler>();

            if (!tileHandler)
                tileHandler = FindAnyObjectByType<Netherlands3D.CartesianTiles.TileHandler>();

            if (tileHandler)
            {
                tileHandler.AddLayer(this);
                yield break;
            }

            Debug.LogError("No TileHandler found.", gameObject);
        }

        private bool IsInExtents(BoundingBox tileBox)
        {
            if (BoundingBox == null) //no bounds set, so we don't know the extents and always need to load the tile
                return true;

            return BoundingBox.Intersects(tileBox);
        }

        public override void HandleTile(TileChange tileChange, Action<TileChange> callback = null)
        {
            TileAction action = tileChange.action;
            var tileKey = new Vector2Int(tileChange.X, tileChange.Y);
            switch (action)
            {
                case TileAction.Create:
                    Tile newTile = CreateNewTile(tileKey);
                    tiles.Add(tileKey, newTile);
                    var tileBox = DetermineBoundingBox(tileChange, CoordinateSystem.RD);
                    if (IsInExtents(tileBox))
                    {
                        newTile.runningCoroutine = StartCoroutine(DownloadGeoJSON(tileChange, newTile, callback));
                    }
                    else
                    {
                        callback?.Invoke(tileChange); //nothing to download, call this to continue loading tiles
                    }

                    break;
                case TileAction.Upgrade:
                    tiles[tileKey].unityLOD++;
                    break;
                case TileAction.Downgrade:
                    tiles[tileKey].unityLOD--;
                    break;
                case TileAction.Remove:
                    wfsGeoJSONLayer.RemoveFeaturesOutOfView();
                    InteruptRunningProcesses(tileKey);
                    tiles.Remove(tileKey);
                    callback?.Invoke(tileChange);
                    return;
                default:
                    break;
            }
        }

        private void OnGeoJSONLayerDestroyed()
        {
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (tileHandler)
                tileHandler.RemoveLayer(this);
        }

        private Tile CreateNewTile(Vector2Int tileKey)
        {
            Tile tile = new()
            {
                unityLOD = 0,
                tileKey = tileKey,
                layer = transform.gameObject.GetComponent<Layer>()
            };

            return tile;
        }

        private BoundingBox DetermineBoundingBox(TileChange tileChange, CoordinateSystem system)
        {
            var bottomLeft = new Coordinate(CoordinateSystem.RD, tileChange.X, tileChange.Y, 0);
            var topRight = new Coordinate(CoordinateSystem.RD, tileChange.X + tileSize, tileChange.Y + tileSize, 0);

            var boundingBox = new BoundingBox(bottomLeft, topRight);
            boundingBox.Convert(system);

            return boundingBox;
        }

        private IEnumerator DownloadGeoJSON(TileChange tileChange, Tile tile, Action<TileChange> callback = null)
        {
            var queryParameters = QueryString.Decode(new Uri(wfsUrl).Query);
            string spatialReference = queryParameters.Single("srsname");

            CoordinateSystem system = DefaultEpsgCoordinateSystem;
            if (!string.IsNullOrEmpty(spatialReference))
            {
                system = CoordinateSystems.FindCoordinateSystem(spatialReference);
                if (system == CoordinateSystem.Undefined) system = DefaultEpsgCoordinateSystem;
            }

            var boundingBox = DetermineBoundingBox(tileChange, system);

            string url = wfsUrl.Replace("{0}", boundingBox.ToString());

            // we need to add the coordinate system value to the bbox as 5th value according to the ogc standards
            if (!string.IsNullOrEmpty(spatialReference))
            {
                url += "," + spatialReference;
            }

            //todo make this page size dynamic?
            const int pageSize = 1000;
            int startIndex = 0;
            
            requestConfig.AddParam("count", pageSize.ToString());

            while (true)
            {
                requestConfig.Params.Set("startIndex", startIndex.ToString());

                string jsonString = null;
                var geoJsonRequest = Uxios.DefaultInstance.Get<string>(new Uri(url), requestConfig);
                geoJsonRequest.Then(r => jsonString = r.Data as string);
                geoJsonRequest.Catch(e => Debug.LogWarning($"Request to {url} failed: {e.Message}"));

                yield return Uxios.WaitForRequest(geoJsonRequest);

                if (string.IsNullOrEmpty(jsonString))
                    break;

                int parsedThisPage = 0;
                var parser = new GeoJSONParser(0.01f);
                parser.OnFeatureParsed.AddListener(_ => parsedThisPage++);
                parser.OnParseError.AddListener(_ => parsedThisPage++);
                parser.OnFeatureParsed.AddListener(wfsGeoJSONLayer.AddFeatureVisualisation);

                yield return parser.ParseJSONString(jsonString);

                if (parsedThisPage == 0) // stop if no features
                    break;

                startIndex += parsedThisPage;
            }
            callback?.Invoke(tileChange);
        }

        public void SetAuthorization(StoredAuthorization auth)
        {
            ClearConfig();
            requestConfig = auth.AddToConfig(requestConfig);
        }

        public void ClearConfig()
        {
            requestConfig = Config.Default();
        }
    }
}
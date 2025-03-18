using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Netherlands3D.CartesianTiles;
using System;
using Netherlands3D.Coordinates;
using KindMen.Uxios;
using Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers;
using Netherlands3D.Twin.Utility;
using System.Collections.Generic;

namespace Netherlands3D.Functionalities.Wfs
{
    /// <summary>
    /// A custom CartesianTile layer that uses the cartesian tiling system to 'stream' parts of 
    /// a WFS service to the client using the 'bbox' parameter.
    /// The Twin GeoJSONLayer is used to render the GeoJSON data.
    /// </summary>
    public class WFSGeoJSONTileDataLayer : Layer
    {
        private const string DefaultEpsgCoordinateSystem = "28992";
        private Netherlands3D.CartesianTiles.TileHandler tileHandler;
        
        public BoundingBox BoundingBox { get; set; }

        private Dictionary<string, string> customHeaders = new Dictionary<string, string>();
        public Dictionary<string, string> CustomHeaders { get => customHeaders; private set => customHeaders = value; }
        private Dictionary<string, string> customQueryParams = new Dictionary<string, string>();
        public Dictionary<string, string> CustomQueryParameters { get => customQueryParams; private set => customQueryParams = value; }

        private string wfsUrl = "";
        public string WfsUrl { 
            get => wfsUrl; 
            set {
                wfsUrl = value;
                if(!wfsUrl.Contains("{0}"))
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

        private Coroutine updateTilesRoutine = null;
        private bool visibleTilesDirty = false;
        private List<TileChange> queuedChanges = new List<TileChange>();
        private float lastUpdatedTimeStamp = 0;
        private float lastUpdatedInterval = 1f;
        private WaitForSeconds wfs = new WaitForSeconds(0.5f);

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

        private CoordinateSystem GetCoordinateSystem(string spatialReference)
        {
            string coordinateSystemAsString = DefaultEpsgCoordinateSystem;
            var splitReferenceCode = spatialReference.Split(':');
            for (int i = 0; i < splitReferenceCode.Length - 1; i++)
                if (splitReferenceCode[i].ToLower() == "epsg")
                {
                    coordinateSystemAsString = splitReferenceCode[^1];
                    break;
                }

            CoordinateSystems.FindCoordinateSystem(coordinateSystemAsString, out var foundCoordinateSystem);
            return foundCoordinateSystem;
        }

        private BoundingBox DetermineBoundingBox(TileChange tileChange, CoordinateSystem system)
        {
            var bottomLeft = new Coordinate(CoordinateSystem.RD, tileChange.X, tileChange.Y, 0);
            var topRight = new Coordinate(CoordinateSystem.RD, tileChange.X + tileSize, tileChange.Y + tileSize, 0);            

            var boundingBox = new BoundingBox(bottomLeft, topRight);
            boundingBox.Convert(system);

            return boundingBox;
        }

        /// <summary>
        /// Add custom headers for all internal WebRequests
        /// </summary>
        public void AddCustomHeader(string key, string value, bool replace = true)
        {
            if (replace && customHeaders.ContainsKey(key))
                customHeaders[key] = value;
            else
                customHeaders.Add(key, value);
        }

        public void ClearCustomHeaders()
        {
            customHeaders.Clear();
        }

        public void AddCustomQueryParameter(string key, string value, bool replace = true)
        {
            if (replace && customQueryParams.ContainsKey(key))
                customQueryParams[key] = value;
            else
                customQueryParams.Add(key, value);
        }

        public void ClearCustomQueryParameters()
        {
            customQueryParams.Clear();
        }

        private IEnumerator DownloadGeoJSON(TileChange tileChange, Tile tile, Action<TileChange> callback = null)
        {
            var queryParameters = QueryString.Decode(new Uri(wfsUrl).Query);
            string spatialReference = queryParameters["srsname"];
            CoordinateSystem system = GetCoordinateSystem(spatialReference);
            var boundingBox = DetermineBoundingBox(tileChange, system);
            
            //we need to add the coordinate system value to the bbox as 5th value according to the ogc standards
            string url = wfsUrl.Replace("{0}", boundingBox.ToString() + "," + ((int)system).ToString());

            string jsonString = null;
            var geoJsonRequest = Uxios.DefaultInstance.Get<string>(new Uri(url));
            geoJsonRequest.Then(response => jsonString = response.Data as string);
            geoJsonRequest.Catch(
                exception => Debug.LogWarning($"Request to {url} failed with message {exception.Message}")
            );
            
            yield return Uxios.WaitForRequest(geoJsonRequest);

            if (string.IsNullOrEmpty(jsonString) == false)
            {
                var parser = new GeoJSONParser(0.01f);
                parser.OnFeatureParsed.AddListener(wfsGeoJSONLayer.AddFeatureVisualisation);
                yield return parser.ParseJSONString(jsonString);
            }

            callback?.Invoke(tileChange);
        }

        public void RefreshTiles()
        {
            //is the update already running cancel it
            if (visibleTilesDirty && updateTilesRoutine != null)
            {
                queuedChanges.Clear();
                StopCoroutine(updateTilesRoutine);
            }

            lastUpdatedTimeStamp = Time.time;
            visibleTilesDirty = true;
            updateTilesRoutine = StartCoroutine(UpdateVisibleTiles());
        }

        private IEnumerator UpdateVisibleTiles()
        {
            //get current tiles
            foreach (KeyValuePair<Vector2Int, Tile> tile in tiles)
            {
                if (tile.Value == null || tile.Value.gameObject == null)
                    continue;

                if (tile.Value.runningCoroutine != null)
                    StopCoroutine(tile.Value.runningCoroutine);

                //OnPreUpdateTile(tile.Value);

                TileChange tileChange = new TileChange();
                tileChange.X = tile.Key.x;
                tileChange.Y = tile.Key.y;
                queuedChanges.Add(tileChange);
            }

            if (!isEnabled)
            {
                queuedChanges.Clear();
                yield break;
            }

            bool ready = true;
            while (queuedChanges.Count > 0)
            {
                //lets wait half a second in case a slider is moving
                if (Time.time - lastUpdatedTimeStamp > lastUpdatedInterval && ready)
                {
                    ready = false;
                    TileChange next = queuedChanges[0];
                    queuedChanges.RemoveAt(0);
                    Vector2Int key = new Vector2Int(next.X, next.Y);
                    if (tiles.ContainsKey(key))
                    {
                        tiles[key].runningCoroutine = StartCoroutine(DownloadGeoJSON(next, tiles[key], key =>
                        {
                            ready = true;
                        }));
                    }
                    else
                    {
                        ready = true;
                    }
                }
                yield return wfs;
            }

            updateTilesRoutine = null;
            visibleTilesDirty = false;
        }
    }
}


using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Layers;
using System;
using Netherlands3D.Coordinates;
using KindMen.Uxios;

namespace Netherlands3D.CartesianTiles
{
    /// <summary>
    /// A custom CartesianTile layer that uses the cartesian tiling system to 'stream' parts of 
    /// a WFS service to the client using the 'bbox' parameter.
    /// The Twin GeoJSONLayer is used to render the GeoJSON data.
    /// </summary>
    public class WFSGeoJSONTileDataLayer : Layer
    {
        private const string DefaultEpsgCoordinateSystem = "28992";
        private TileHandler tileHandler;
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
            tileHandler = GetComponentInParent<TileHandler>();

            if (!tileHandler)
                tileHandler = FindAnyObjectByType<TileHandler>();

            if (tileHandler)
            {
                tileHandler.AddLayer(this);
                yield break;
            }

            Debug.LogError("No TileHandler found.", gameObject);
        }

        public override void HandleTile(TileChange tileChange, System.Action<TileChange> callback = null)
        {
            TileAction action = tileChange.action;
            var tileKey = new Vector2Int(tileChange.X, tileChange.Y);
            switch (action)
            {
                case TileAction.Create:
                    Tile newTile = CreateNewTile(tileKey);
                    tiles.Add(tileKey, newTile);
                    newTile.runningCoroutine = StartCoroutine(DownloadGeoJSON(tileChange, newTile, callback));
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

        private Twin.Wms.BoundingBox DetermineBoundingBox(TileChange tileChange, string spatialReference, out string spatialReferenceCode)
        {
            var bottomLeft = new Coordinate(CoordinateSystem.RD, tileChange.X, tileChange.Y, 0);
            var topRight = new Coordinate(CoordinateSystem.RD, tileChange.X + tileSize, tileChange.Y + tileSize, 0);

            string coordinateSystemAsString = DefaultEpsgCoordinateSystem;
            var splitReferenceCode = spatialReference.Split(':');
            for (int i = 0; i < splitReferenceCode.Length - 1; i++)
                if (splitReferenceCode[i].ToLower() == "epsg")
                {
                    coordinateSystemAsString = splitReferenceCode[^1];
                    break;
                }

            spatialReferenceCode = coordinateSystemAsString;
            CoordinateSystems.FindCoordinateSystem(coordinateSystemAsString, out var foundCoordinateSystem);

            var boundingBox = new Twin.Wms.BoundingBox(bottomLeft, topRight);
            boundingBox.Convert(foundCoordinateSystem);

            return boundingBox;
        }

        private IEnumerator DownloadGeoJSON(TileChange tileChange, Tile tile, System.Action<TileChange> callback = null)
        {
            var queryParameters = QueryString.Decode(new Uri(wfsUrl).Query);
            string spatialReference = queryParameters["srsname"];
            var boundingBox = DetermineBoundingBox(tileChange, spatialReference, out string code);
            string url = wfsUrl.Replace("{0}", boundingBox.ToString() + "," + code);

            //var bboxValue = $"{tileChange.X},{tileChange.Y},{(tileChange.X + tileSize)},{(tileChange.Y + tileSize)}";
            //string url = WfsUrl.Replace("{0}", bboxValue);

            var geoJsonRequest = UnityWebRequest.Get(url);
            tile.runningWebRequest = geoJsonRequest;
            yield return geoJsonRequest.SendWebRequest();

            if (geoJsonRequest.result == UnityWebRequest.Result.Success)
            {
                var parser = new GeoJsonParser(0.01f);
                parser.OnFeatureParsed.AddListener(wfsGeoJSONLayer.AddFeatureVisualisation);
                yield return parser.ParseJSONString(geoJsonRequest.downloadHandler.text);
            }
            else
            {
                // Show a message in the console, because otherwise you will never find out
                // something went wrong. This should be replaced with a better error reporting
                // system
                Debug.LogWarning(
                    $"Request to {url} failed with status code {geoJsonRequest.responseCode} and body \n{geoJsonRequest.downloadHandler.text}"
                );
            }
            callback?.Invoke(tileChange);
        }
    }
}

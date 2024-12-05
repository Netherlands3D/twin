using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Netherlands3D.LayerStyles;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.Properties;
using GeoJSON.Net.Feature;

namespace Netherlands3D.CartesianTiles
{
    /// <summary>
    /// A custom CartesianTile layer that uses the cartesian tiling system to 'stream' parts of 
    /// a WFS service to the client using the 'bbox' parameter.
    /// The Twin GeoJSONLayer is used to render the GeoJSON data.
    /// </summary>
    public class ATMPointLayer : Layer
    {
        private TileHandler tileHandler;
        [SerializeField] private string year = "1943";
        [SerializeField] private string tileFolderPath = "ATMBuildingGeojson/Tiles";
        private LayerURLPropertyData urlPropertyData;
        private ATMVlooienburgController vlooienburgController;

        public string Year
        {
            get => year;
            set { year = value; }
        }

        private GeoJsonLayerGameObject geoJsonLayer;

        public GeoJsonLayerGameObject GeoJSONLayer
        {
            get => geoJsonLayer;
            set
            {
                if (geoJsonLayer != null)
                    geoJsonLayer.LayerData.LayerDestroyed.RemoveListener(OnGeoJSONLayerDestroyed);

                geoJsonLayer = value;
                geoJsonLayer.LayerData.LayerDestroyed.AddListener(OnGeoJSONLayerDestroyed);
            }
        }

        private void Awake()
        {
            geoJsonLayer = GetComponent<GeoJsonLayerGameObject>();
            geoJsonLayer.AddPoint += OnAddPoint;

            UpdateUri(Year);

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

        public override void Start()
        {
            base.Start();
            geoJsonLayer.LayerData.DefaultSymbolizer.SetFillColor(Color.red);
        }

        public void SetATMVlooienburg(ATMVlooienburgController controller)
        {
            vlooienburgController = controller;
        }

        private void OnAddPoint(Feature feature)
        {
            object linkObject;
            feature.Properties.TryGetValue("id", out linkObject);
            string link = (string)linkObject;
            bool hasLink = vlooienburgController.HasAdamlink(link);
            if (hasLink)
            {
                //pass the feature for selection later on
                vlooienburgController.LoadAssetForAdamLink(link, feature);
            }
        }

        public void UpdateUri(string year)
        {
            string file = year + "({x}, {y}).geojson";
            string uri = Path.Combine(Application.streamingAssetsPath, tileFolderPath, year, file);

            urlPropertyData = (LayerURLPropertyData)geoJsonLayer.PropertyData;
            urlPropertyData.Data = new Uri(uri);

            if(vlooienburgController != null) 
            vlooienburgController.DisableAllAssets();
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
                    geoJsonLayer.RemoveFeaturesOutOfView();

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
            foreach (GeoJsonLayerGameObject.FeatureHandler handler in geoJsonLayer.AddPoint.GetInvocationList())
                if (handler == OnAddPoint)
                    geoJsonLayer.AddPoint -= OnAddPoint;

            if (tileHandler)
                tileHandler.RemoveLayer(this);
        }

        private Tile CreateNewTile(Vector2Int tileKey)
        {
            Tile tile = new()
            {
                unityLOD = 0,
                tileKey = tileKey,
                layer = this //transform.gameObject.GetComponent<Layer>()
            };

            return tile;
        }

        private IEnumerator DownloadGeoJSON(TileChange tileChange, Tile tile, System.Action<TileChange> callback = null)
        {
            var uri = urlPropertyData.Data.ToString();
            uri = uri.Replace("{x}", tileChange.X.ToString());
            uri = uri.Replace("{y}", tileChange.Y.ToString());

            var geoJsonRequest = UnityWebRequest.Get(uri);
            tile.runningWebRequest = geoJsonRequest;
            
            yield return geoJsonRequest.SendWebRequest();
            if (geoJsonRequest.result == UnityWebRequest.Result.Success)
            {
                var parser = new GeoJsonParser(0.01f);
                parser.OnFeatureParsed.AddListener(geoJsonLayer.AddFeatureVisualisation);
                yield return parser.ParseJSONString(geoJsonRequest.downloadHandler.text);
            }
            else
            {
                // Show a message in the console, because otherwise you will never find out
                // something went wrong. This should be replaced with a better error reporting
                // system
                Debug.LogWarning(
                    $"Request to {uri} failed with status code {geoJsonRequest.responseCode} and body \n{geoJsonRequest.downloadHandler.text}"
                );
            }

            callback?.Invoke(tileChange);
        }
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using GeoJSON.Net.Feature;
using Netherlands3D.Twin.Layers;

namespace Netherlands3D.CartesianTiles
{
    /// <summary>
    /// A custom CartesianTile layer that uses the cartesian tiling system to 'stream' parts of 
    /// a WFS service to the client using the 'bbox' parameter.
    /// The Twin GeoJSONLayer is used to render the GeoJSON data.
    /// </summary>
	public class WFSGeoJSONTileDataLayer : Layer
	{		
        private TileHandler tileHandler;
        private string wfsUrl = "";
        public string WfsUrl { get => wfsUrl; set => wfsUrl = value; }

        private GeoJSONLayer geoJSONLayer;
        public GeoJSONLayer GeoJSONLayer { 
			get => geoJSONLayer; 
			set
			{
				if(geoJSONLayer != null)
					geoJSONLayer.ReferencedProxy.LayerDestroyed.RemoveListener(OnGeoJSONLayerDestroyed);

				geoJSONLayer = value;
				geoJSONLayer.ReferencedProxy.LayerDestroyed.AddListener(OnGeoJSONLayerDestroyed);
			}
		}

        private void Awake() {
			//Make sure Datasets at least has one item
			if(Datasets.Count == 0)
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
            
            if(!tileHandler)
                tileHandler = FindAnyObjectByType<TileHandler>();

            if(tileHandler){
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
					geoJSONLayer.RemoveFeaturesOutOfView();
					
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

        private void OnDestroy() {
            if(tileHandler)
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

		private IEnumerator DownloadGeoJSON(TileChange tileChange, Tile tile, System.Action<TileChange> callback = null)
		{
			var bboxValue = $"{tileChange.X},{tileChange.Y},{(tileChange.X + tileSize)},{(tileChange.Y + tileSize)}";
			string url = WfsUrl.Replace("{0}", bboxValue);
			Debug.Log("Downloading GeoJSON from: " + url);

			var geoJsonRequest = UnityWebRequest.Get(url);
			tile.runningWebRequest = geoJsonRequest;
			yield return geoJsonRequest.SendWebRequest();

			if (geoJsonRequest.result == UnityWebRequest.Result.Success)
			{
				ParseGeoJSON(geoJsonRequest.downloadHandler.text);		
			}
			callback?.Invoke(tileChange);
		}

		private void ParseGeoJSON(string jsonText)
		{	
            var featureCollection = JsonConvert.DeserializeObject<FeatureCollection>(jsonText);

			if(featureCollection.Features.Count > 0)
				geoJSONLayer.AppendFeatureCollection(featureCollection);
		}
	}
}

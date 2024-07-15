
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin;
using Netherlands3D.Twin.UI.LayerInspector;

namespace Netherlands3D.CartesianTiles
{
    /// <summary>
    /// A custom CartesianTile layer that uses the cartesian tiling system to 'stream' parts of 
    /// a WFS service to the client using the bbox parameter.
    /// The Twin GeoJSONLayer is used to render the GeoJSON data.
    /// </summary>
	[AddComponentMenu("Netherlands3D/CartesianTiles/GeoJSONTileDataLayer")]
	public class WFSGeoJSONTileDataLayer : Layer
	{		
        private string wfsUrl = "";
        public string WfsUrl { get => wfsUrl; set => wfsUrl = value; }

        private GeoJSONLayer geoJSONLayer;
        public GeoJSONLayer GeoJSONLayer { get => geoJSONLayer; set => geoJSONLayer = value; }

        private TileHandler tileHandler;

        private void Awake() {
            geoJSONLayer.LayerDestroyed.AddListener(OnGeoJSONLayerDestroyed);

            //Make sure we live in a tilehandler
            tileHandler = GetComponentInParent<TileHandler>();
            
            if(!tileHandler)
                tileHandler = FindAnyObjectByType<TileHandler>();

            if(tileHandler){
                tileHandler.AddLayer(this);
                return;
            }

            Debug.LogError("No TileHandler found.", gameObject);
        }

        private void OnGeoJSONLayerDestroyed()
        {
            Destroy(gameObject);
        }

        private void OnDestroy() {
            if(tileHandler)
                tileHandler.RemoveLayer(this);
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
					InteruptRunningProcesses(tileKey);
					tiles.Remove(tileKey);
					callback?.Invoke(tileChange);
					return;
				default:
					break;
			}
		}

		private Tile CreateNewTile(Vector2Int tileKey)
		{
			Tile tile = new Tile();
			tile.unityLOD = 0;
			tile.tileKey = tileKey;
			tile.layer = transform.gameObject.GetComponent<Layer>();
			
			return tile;
		}

		private IEnumerator DownloadGeoJSON(TileChange tileChange, Tile tile, System.Action<TileChange> callback = null)
		{
			string url = $"{WfsUrl}{tileChange.X},{tileChange.Y},{(tileChange.X + tileSize)},{(tileChange.Y + tileSize)}";

			var geoJsonRequest = UnityWebRequest.Get(url);
			tile.runningWebRequest = geoJsonRequest;
			yield return geoJsonRequest.SendWebRequest();

			if (geoJsonRequest.result == UnityWebRequest.Result.Success)
			{
                
			}
			callback?.Invoke(tileChange);
		}
	}
}

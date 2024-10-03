using Netherlands3D.CartesianTiles;
using Netherlands3D.Rendering;
using Netherlands3D.Twin.Layers;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering.Universal;

namespace Netherlands3D.Twin
{
    public class WMSTileDataLayer : ImageProjectionLayer
    {
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

        protected override IEnumerator DownloadDataAndGenerateTexture(TileChange tileChange, Action<TileChange> callback = null)
        {
            var tileKey = new Vector2Int(tileChange.X, tileChange.Y);

            if (!tiles.ContainsKey(tileKey))
            {
                onLogMessage.Invoke(LogType.Warning, "Tile key does not exist");
                yield break;
            }

            Tile tile = tiles[tileKey];
            var bboxValue = $"{tileChange.X},{tileChange.Y},{(tileChange.X + tileSize)},{(tileChange.Y + tileSize)}";
            string url = wmsUrl.Replace("{0}", bboxValue);

            //var webRequest = UnityWebRequest.Get(url);
            UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(url);
            tile.runningWebRequest = webRequest;
            yield return webRequest.SendWebRequest();
            tile.runningWebRequest = null;
            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"Could not download {url}");
                RemoveGameObjectFromTile(tileKey);
                callback(tileChange);
            }
            else
            {
                //byte[] imageData = webRequest.downloadHandler.data;
                //Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                //tex.LoadImage(imageData);
                ////tex.LoadRawTextureData(imageData);
                //tex.Apply();

                Texture texture = ((DownloadHandlerTexture)webRequest.downloadHandler).texture;

                ////Texture myTexture = DownloadHandlerTexture.GetContent(webRequest);
                Texture2D tex = texture as Texture2D;

                Color[] pixels = tex.GetPixels();

                if (tile.gameObject.TryGetComponent<TextureProjectorBase>(out var projector))
                {
                    projector.SetSize(tileSize, tileSize, tileSize);
                    projector.gameObject.SetActive(isEnabled);                    
                    projector.SetTexture(tex);
                    //force the depth to be at least larger than its height to prevent z-fighting
                    DecalProjector decalProjector = tile.gameObject.GetComponent<DecalProjector>();
                    TextureDecalProjector textureDecalProjector = tile.gameObject.GetComponent<TextureDecalProjector>();
                    if (ProjectorHeight >= decalProjector.size.z)
                        textureDecalProjector.SetSize(decalProjector.size.x, decalProjector.size.y, ProjectorMinDepth);

                }


                ClearPreviousTexture(tile);
                callback(tileChange);
            }
            yield return null;
        }
    }
}

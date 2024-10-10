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
        //this gives the requesting url the extra param to set transparancy enabled by default
        public bool TransparencyEnabled = true;

        //in case the dataset is very large with many layers. lets topggle the layers after this count to not visible.
        public int DefaultEnabledLayersMax = 5;

        public int RenderIndex 
        { 
            get => renderIndex;
            set
            {
                renderIndex = value;
            }
        }

        private int renderIndex = 0;

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
                Texture texture = ((DownloadHandlerTexture)webRequest.downloadHandler).texture;
                Texture2D tex = texture as Texture2D;
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

                    //set the render index, to make sure the render order is maintained
                    textureDecalProjector.SetPriority(renderIndex);

                }
                ClearPreviousTexture(tile);
                callback(tileChange);
            }
            yield return null;
        }
    }
}

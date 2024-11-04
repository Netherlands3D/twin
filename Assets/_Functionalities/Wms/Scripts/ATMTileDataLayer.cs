using Netherlands3D.CartesianTiles;
using Netherlands3D.Rendering;
using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Coordinates;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering.Universal;

namespace Netherlands3D.Twin
{
    public class ATMTileDataLayer : ImageProjectionLayer
    {
        private ATMTileCoordinates atmTileCoordinates;

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
            atmTileCoordinates = GetComponent<ATMTileCoordinates>();

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

        public GameObject debugTextPrefab;

        private void AddDebugText(GameObject tile, string text)
        {
            var a = Instantiate(debugTextPrefab, tile.gameObject.transform, false);
            a.GetComponentInChildren<TMPro.TMP_Text>().transform.Translate(0, 0, 970);
            a.GetComponentInChildren<TMPro.TMP_Text>().text = text;
        }

        public Vector2 gridOffset;
        private void Update()
        {
            foreach (var tile in tiles)
            {
                var projector = tile.Value.gameObject.GetComponent<DecalProjector>();
                var tileObject = tile.Value.gameObject;
                if (!projector)
                    return;

                projector.size = new Vector3(tileSize * tileObject.transform.localScale.x, tileSize * tileObject.transform.localScale.y, projector.size.z);
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
            var tileCoord = new Coordinate(CoordinateSystem.RD, tileChange.X + tileSize / 2, tileChange.Y + tileSize / 2);
            string url = atmTileCoordinates.GetTileUrl(tileCoord, 16);
            var coord = ATMTileCoordinates.CoordinateToTileXY(tileCoord, 16);
            AddDebugText(tile.gameObject, tileKey.ToString() + "\n" + coord.ToString());
            print(tileKey + "\t" + url);

            UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(url);
            tile.runningWebRequest = webRequest;
            yield return webRequest.SendWebRequest();
            tile.runningWebRequest = null;
            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"Could not download {url}");
                RemoveGameObjectFromTile(tileKey);
            }
            else
            {
                ClearPreviousTexture(tile);
                Texture texture = ((DownloadHandlerTexture)webRequest.downloadHandler).texture;
                Texture2D tex = texture as Texture2D;
                tex.Compress(true);
                tex.filterMode = FilterMode.Bilinear;
                tex.Apply(false, true);

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

                tile.gameObject.transform.position += new Vector3(gridOffset.x, 0, gridOffset.y);
            }

            callback(tileChange);
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

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            foreach (var tile in tiles)
            {
                Gizmos.DrawWireCube(CoordinateConverter.RDtoUnity(new Vector3(tile.Key.x + (tileSize / 2), tile.Key.y + (tileSize / 2), 0)), new Vector3(tileSize, 100, tileSize));
            }
        }
#endif
    }
}
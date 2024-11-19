using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.CartesianTiles;
using Netherlands3D.Coordinates;
using Netherlands3D.Rendering;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering.Universal;
using UnityEngine.Timeline;

namespace Netherlands3D.Twin
{
    public class ATMTileDataLayer : ImageProjectionLayer
    {
        private ATMDataController timeController;

        private float lastUpdatedTimeStamp = 0;
        private float lastUpdatedInterval = 1f;
        private bool visibleTilesDirty = false;
        private List<TileChange> queuedChanges = new List<TileChange>();
        private WaitForSeconds wfs = new WaitForSeconds(0.5f);
        private Coroutine updateTilesRoutine = null;

        private const float earthRadius = 6378.137f;
        private const float equatorialCircumference = 2 * Mathf.PI * earthRadius;
        private const float log2x = 0.30102999566f;

        [SerializeField] private int zoomLevel = 16;
        private XyzTiles xyzTiles;

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

        /// <summary>
        /// Cached zoom level, because we need to compute the actual tilesize from the quad tree; we cache the previous
        /// value and only if it changed will we recompute the tileSize.
        /// </summary>
        private int previousZoomLevel;

        private double referenceTileWidth;
        private double referenceTileHeight;

        private ATMDataController.ATMDataHandler ATMDataHandler;

        private void Awake()
        {
            xyzTiles = GetComponent<XyzTiles>();

            if (timeController == null)
            {
                timeController = gameObject.AddComponent<ATMDataController>();
                ATMDataHandler = (a) => SetVisibleTilesDirty();
                timeController.ChangeYear += ATMDataHandler;
            }
            
            //Make sure Datasets at least has one item
            if (Datasets.Count != 0) return;

            Datasets.Add(new DataSet
            {
                maximumDistance = 3000,
                maximumDistanceSquared = 3000 * 3000
            });
        }

        private void OnDestroy()
        {
            if(timeController != null && ATMDataHandler != null)
                timeController.ChangeYear -= ATMDataHandler;
        }

        private void Update()
        {            
            //zoomLevel = CalculateZoomLevel();
            Debug.Log(zoomLevel);
            if (zoomLevel != previousZoomLevel)
            {
                this.previousZoomLevel = zoomLevel;
                SetVisibleTilesDirty();
            }
        }

        public int CalculateZoomLevel()
        {
            Vector3 camPosition = Camera.main.transform.position;
            float viewDistance = camPosition.y; //lets keep it orthographic?
            var unityCoordinate = new Coordinate(
                CoordinateSystem.Unity,
                camPosition.x,
                camPosition.z,
                0
            );
            Coordinate coord = CoordinateConverter.ConvertTo(unityCoordinate, CoordinateSystem.WGS84);
            float latitude = (float)coord.Points[0];
            float cosLatitude = Mathf.Cos(latitude * Mathf.Deg2Rad); //to rad

            //https://wiki.openstreetmap.org/wiki/Zoom_levels
            float numerator = equatorialCircumference * cosLatitude;
            float zoomLevel = Mathf.Log(numerator / viewDistance) / log2x;

            return Mathf.RoundToInt(zoomLevel);
        }

        protected override IEnumerator DownloadDataAndGenerateTexture(
            TileChange tileChange,
            Action<TileChange> callback = null
        )
        {
            var tileKey = new Vector2Int(tileChange.X, tileChange.Y);

            if (!tiles.ContainsKey(tileKey))
            {
                onLogMessage.Invoke(LogType.Warning, "Tile key does not exist");
                yield break;
            }

            Tile tile = tiles[tileKey];

            //we need to take the center of the cartesian tile to be sure the coordinate does not fall within the conversion boundaries of the bottomleft quadtreecell
            var tileCoordinate = new Coordinate(CoordinateSystem.RD, tileChange.X + 0.5f * tileSize, tileChange.Y + 0.5f * tileSize);
            var xyzTile = xyzTiles.FetchTileAtCoordinate(tileCoordinate, zoomLevel, timeController);

            //because of the predefined map bounds, we dont have to check outside these bounds
            //if (!timeController.IsTileWithinXY(xyzTile.TileIndex.x, xyzTile.TileIndex.y)) yield break;

            // The tile coordinate does not align with the grid of the XYZTiles, so we calculate an offset
            // for the projector to align both grids; this must be done per tile to prevent rounding issues and
            // have the cleanest match
            var offset = CalculateTileOffset(xyzTile, tileCoordinate, zoomLevel);
            //set the output position back to the right coordinate as this was adjusted before
            offset += new Vector3(-0.5f * tileSize, 0, -0.5f * tileSize);

            UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(xyzTile.URL);
            tile.runningWebRequest = webRequest;
            yield return webRequest.SendWebRequest();
            tile.runningWebRequest = null;
            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"Could not download {xyzTile.URL}");
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
                OnTextureDownloaded(tex, tileKey, offset);
            }

            callback(tileChange);
        }

        private void OnTextureDownloaded(Texture2D tex, Vector2Int tileKey, Vector3 projectorOffset)
        {
            Tile tile = tiles[tileKey];

            if (tiles[tileKey] == null || tiles[tileKey].gameObject == null)
                return;

            if (tile.gameObject.TryGetComponent<TextureProjectorBase>(out var projector))
            {
                projector.SetSize((float)referenceTileWidth, (float)referenceTileWidth, (float)referenceTileHeight);
                projector.gameObject.SetActive(isEnabled);
                projector.SetTexture(tex);
                //force the depth to be at least larger than its height to prevent z-fighting
                DecalProjector decalProjector = tile.gameObject.GetComponent<DecalProjector>();
                TextureDecalProjector textureDecalProjector = tile.gameObject.GetComponent<TextureDecalProjector>();
                if (ProjectorHeight >= decalProjector.size.z)
                    textureDecalProjector.SetSize(decalProjector.size.x, decalProjector.size.y, ProjectorMinDepth);

                Vector2Int origin = new Vector2Int(tileKey.x + (tileSize / 2), tileKey.y + (tileSize / 2));
                var rdCoordinate = new Coordinate(
                    CoordinateSystem.RD,
                    origin.x,
                    origin.y,
                    0.0d
                );
                var originCoordinate = CoordinateConverter.ConvertTo(rdCoordinate, CoordinateSystem.Unity).ToVector3();
                originCoordinate.y = ProjectorHeight;
                tile.gameObject.transform.position = originCoordinate;
                decalProjector.transform.position -= projectorOffset;

                //set the render index, to make sure the render order is maintained
                textureDecalProjector.SetPriority(renderIndex);
            }
        }

        private void UpdateReferenceSizes()
        {
            // We use this tile as a reference, each tile has a slight variation but if all is well we can ignore
            // that after casting
            var referenceTileIndex = new Vector2Int(33659, 21539);
            var (tileWidth, tileHeight) = CalculateTileDimensionsInRdMeters(referenceTileIndex);
            referenceTileWidth = tileWidth;
            referenceTileHeight = tileHeight;

            tileSize = (int)tileWidth;
        }

        private (double tileWidth, double tileHeight) CalculateTileDimensionsInRdMeters(Vector2Int tileIndex)
        {
            var (minBound, maxBound) = xyzTiles.FromTileXYToBoundingBox(tileIndex, zoomLevel);
            var minBoundRd = minBound.Convert(CoordinateSystem.RD);
            var maxBoundRd = maxBound.Convert(CoordinateSystem.RD);
            var tileWidth = maxBoundRd.Points[0] - minBoundRd.Points[0];
            var tileHeight = maxBoundRd.Points[1] - minBoundRd.Points[1];

            return (tileWidth, tileHeight);
        }

        private static Vector3 CalculateTileOffset(XyzTiles.XyzTile xyzTile, Coordinate tileCoordRd, int zoomLevel)
        {
            var minBound = xyzTile.MinBound.Convert(CoordinateSystem.RD);
            //convert minbound by zoomlevel 
            //todo here

            var minBoundRd = xyzTile.MinBound.Convert(CoordinateSystem.RD);
            var maxBoundRd = xyzTile.MaxBound.Convert(CoordinateSystem.RD);
            var tileWidth = maxBoundRd.Points[0] - minBoundRd.Points[0];
            double tileSizeAtZoom = tileWidth / Math.Pow(2, xyzTile.ZoomLevel);

            return new Vector3(
                (float)(tileCoordRd.Points[0] - minBound.Points[0]),
                0,
                (float)(tileCoordRd.Points[1] - minBound.Points[1])
            );
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

        public void SetVisibleTilesDirty()
        {
            UpdateReferenceSizes();

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
                        tiles[key].runningCoroutine = StartCoroutine(DownloadDataAndGenerateTexture(next, key =>
                        {
                            ready = true;
                            Vector2Int downloadedKey = new Vector2Int(key.X, key.Y);
                            if (tiles[downloadedKey] != null && tiles[downloadedKey].gameObject != null)
                            {
                                DecalProjector projector = tiles[downloadedKey].gameObject.GetComponent<DecalProjector>();
                                if (!projector) return;

                                var localScale = projector.transform.localScale;

                                // because the EPSG:3785 tiles are square, but RD is not square; we make it square by changing the
                                // projection dimensions
                                projector.size = new Vector3(
                                    (float)(referenceTileWidth * localScale.x),
                                    (float)(referenceTileWidth * localScale.y),
                                    projector.size.z
                                );
                            }
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

        public override void LayerToggled()
        {
            base.LayerToggled();
            if (!isEnabled)
            {
                //get current tiles
                foreach (KeyValuePair<Vector2Int, Tile> tile in tiles)
                {
                    if (tile.Value == null || tile.Value.gameObject == null)
                        continue;

                    if (tile.Value.runningCoroutine != null)
                        StopCoroutine(tile.Value.runningCoroutine);

                    //TextureDecalProjector projector = tile.Value.gameObject.GetComponent<TextureDecalProjector>();
                    //projector.gameObject.SetActive(false);
                }
            }
        }
    }
}
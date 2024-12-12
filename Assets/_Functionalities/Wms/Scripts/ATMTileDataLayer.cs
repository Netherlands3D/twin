using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.CartesianTiles;
using Netherlands3D.Coordinates;
using Netherlands3D.Rendering;
using Netherlands3D.Twin.FloatingOrigin;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering.Universal;
using UnityEngine.Timeline;

namespace Netherlands3D.Twin
{
    public class ATMTileDataLayer : ImageProjectionLayer
    {
        public int ZoomLevel => zoomLevel;

        private float lastUpdatedTimeStamp = 0;
        private float lastUpdatedInterval = 1f;
        private bool visibleTilesDirty = false;
        private List<TileChange> queuedChanges = new List<TileChange>();
        private WaitForSeconds wfs = new WaitForSeconds(0.5f);
        private Coroutine updateTilesRoutine = null;      

        private int zoomLevel = -1;
        private XyzTiles xyzTiles;
        private ATMDataController atmDataController;

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
        private double referenceTileWidth;
        private double referenceTileHeight;

        private void Awake()
        {
            xyzTiles = GetComponent<XyzTiles>();
            
            //Make sure Datasets at least has one item
            if (Datasets.Count != 0) return;

            Datasets.Add(new DataSet
            {
                maximumDistance = 3000,
                maximumDistanceSquared = 3000 * 3000
            });

            GetComponent<WorldTransform>().onPostShift.AddListener(ShiftUpdateTiles);
        }

        private void ShiftUpdateTiles(WorldTransform worldTransform, Coordinate cd)
        {
            xyzTiles.ClearDebugTiles();
            SetVisibleTilesDirty();
        }
        
        public void CancelTiles()
        {
            List<Vector2Int> keys = tiles.Keys.ToList();
            foreach (Vector2Int key in keys)
            {
                InteruptRunningProcesses(key);
                if (tiles[key] != null && tiles[key].gameObject != null)
                    ClearPreviousTexture(tiles[key]);
            }
        }

        private void OnDisable()
        {
            if (updateTilesRoutine != null)
            {
                StopCoroutine(updateTilesRoutine);
            }
        }
        
        public void SetDataController(ATMDataController controller)
        {
            atmDataController = controller;
        }

        public void SetZoomLevel(int zoomLevel)
        {
            this.zoomLevel = zoomLevel;
        }

        private void EnableGroundPlanesInTileRange(bool enabled, Coordinate min, Coordinate max)
        {
            Vector3 unityMin = min.ToUnity();
            Vector3 unityMax = max.ToUnity();
            ATMBagGroundPlane[] aTMBagGroundPlanes = GetComponentsInChildren<ATMBagGroundPlane>(true);
            foreach (ATMBagGroundPlane plane in aTMBagGroundPlanes)
            {
                Vector3 pos = plane.coord.ToUnity();
                if (pos.x >= unityMin.x && pos.x < unityMax.x && pos.z >= unityMin.z && pos.z < unityMax.z)
                {
                    plane.gameObject.SetActive(enabled);
                }
            }
        }
        
        protected override IEnumerator DownloadDataAndGenerateTexture(
            TileChange tileChange,
            Action<TileChange> callback = null
        )
        {
            var tileKey = new Vector2Int(tileChange.X, tileChange.Y);

            if (zoomLevel < 0)
                yield break;

            if (!tiles.ContainsKey(tileKey))
            {
                onLogMessage.Invoke(LogType.Warning, "Tile key does not exist");
                yield break;
            }

            Tile tile = tiles[tileKey];

            var tileCenter = new Coordinate(CoordinateSystem.RD, tileChange.X, tileChange.Y);
            xyzTiles.UrlTemplate = atmDataController.GetUrl();
            var xyzTile = xyzTiles.FetchTileAtCoordinate(tileCenter, zoomLevel);

            // The tile coordinate does not align with the grid of the XYZTiles, so we calculate an offset
            // for the projector to align both grids; this must be done per tile to prevent rounding issues and
            // have the cleanest match
            var offset = CalculateTileOffset(xyzTile, tileCenter);


            var halfTileSize = tileSize * .5f;
            var tileCoordinateMin = new Coordinate(CoordinateSystem.RD, tileChange.X - halfTileSize, tileChange.Y - halfTileSize);
            var tileCoordinateMax = new Coordinate(CoordinateSystem.RD, tileChange.X + halfTileSize, tileChange.Y + halfTileSize);
            EnableGroundPlanesInTileRange(false, tileCoordinateMin, tileCoordinateMax);

            UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture((Uri)xyzTile.URL);
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
                if (tiles[tileKey] != null && !(tiles[tileKey].gameObject))
                {
                    tile.gameObject.TryGetComponent<TextureProjectorBase>(out var projector);
                    ((TextureDecalProjector)projector).debugUrl = xyzTile.URL;
                }
                
                EnableGroundPlanesInTileRange(true, tileCoordinateMin, tileCoordinateMax);
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
                {
                    textureDecalProjector.SetSize(decalProjector.size.x, decalProjector.size.y, ProjectorMinDepth);
                }

                var rdCoordinate = new Coordinate(CoordinateSystem.RD, tileKey.x, tileKey.y, 0.0d);
                var originCoordinate = rdCoordinate.ToUnity();
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
            var pos = xyzTiles.FetchTileAtCoordinate(new Coordinate(CoordinateSystem.RD, 120000, 480000, 0), zoomLevel, true);
            var referenceTileIndex = pos.TileIndex;
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

        private static Vector3 CalculateTileOffset(XyzTiles.XyzTile xyzTile, Coordinate tileCenter)
        {
            var xyzCenter = xyzTile.Center.Convert(CoordinateSystem.RD);           
            return new Vector3(
                (float)(tileCenter.Points[0] - xyzCenter.Points[0]), 
                0,
                (float)(tileCenter.Points[1] - xyzCenter.Points[1]) 
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
            if (zoomLevel < 0)
                return;

            xyzTiles.ClearDebugTiles();
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
            if (isEnabled) return;
            
            //get current tiles
            foreach (KeyValuePair<Vector2Int, Tile> tile in tiles)
            {
                if (tile.Value == null) continue;
                if (tile.Value.gameObject == null) continue;

                if (tile.Value.runningCoroutine != null)
                {
                    StopCoroutine(tile.Value.runningCoroutine);
                }

                //TextureDecalProjector projector = tile.Value.gameObject.GetComponent<TextureDecalProjector>();
                //projector.gameObject.SetActive(false);
            }
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KindMen.Uxios;
using Netherlands3D.CartesianTiles;
using Netherlands3D.Coordinates;
using Netherlands3D.Rendering;
using Netherlands3D.Twin.FloatingOrigin;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering.Universal;

namespace Netherlands3D.Twin
{
    public class ATMTileDataLayer : ImageProjectionLayer
    {
        public int ZoomLevel => zoomLevel;

        private float lastUpdatedTimeStamp = 0;
        private float lastUpdatedInterval = 1f;
        private bool visibleTilesDirty = false;
        private List<TileChange> queuedChanges = new List<TileChange>();
        private WaitForSeconds waitForSeconds = new WaitForSeconds(0.5f);
        private Coroutine updateTilesRoutine = null;      

        private int zoomLevel = -1;
        private XyzTiles xyzTiles;
        private ATMDataController atmDataController;

        /// <summary>
        /// Cartesian Tiles do not align with the XyzTiles because the bottom left of Cartesian Tiles are always a
        /// multiple of the tileSize added on top of the Unity origin (0,0,0), while XyzTiles bottom left always matches
        /// a specific WGS84 Coordinate.
        ///
        /// This means that a single Cartesian Tile may intersect with multiple XyzTiles, and thus we need to check each
        /// corner of the CartesianTile to which XyzTile it belongs and dynamically create and destroy these 'Atm Tiles'
        /// based on whether cartesian tiles are linked to them; only when none are linked: cleanup.
        /// </summary>
        private Dictionary<XyzTiles.XyzTile, VisibleAtmTile> atmTiles = new();

        private class VisibleAtmTile
        {
            public XyzTiles.XyzTile XyzTile;
            public TextureDecalProjector Projector;
            public List<Vector2Int> LinkedCartesianTiles = new();
            public UnityWebRequest webRequest = null;
        }
        
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
            // TODO: Recalculate projector positions
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

            xyzTiles.UrlTemplate = atmDataController.GetUrl();

            var (tileCoordinateMin, tileCoordinateMax) = GetMinAndMaxCoordinate(tileKey);
            var (xyzTileBL, xyzTileBR, xyzTileTR, xyzTileTL) = GetXyzTilesForMinAndMaxCoordinate(tileCoordinateMin, tileCoordinateMax);

            EnableGroundPlanesInTileRange(false, tileCoordinateMin, tileCoordinateMax);

            yield return TryAddProjector(tileKey, xyzTileBL);
            yield return TryAddProjector(tileKey, xyzTileBR);
            yield return TryAddProjector(tileKey, xyzTileTL);
            yield return TryAddProjector(tileKey, xyzTileTR);

            EnableGroundPlanesInTileRange(true, tileCoordinateMin, tileCoordinateMax);

            callback?.Invoke(tileChange);
        }

        private (XyzTiles.XyzTile xyzTileBL, XyzTiles.XyzTile xyzTileBR, XyzTiles.XyzTile xyzTileTR, XyzTiles.XyzTile xyzTileTL) 
            GetXyzTilesForMinAndMaxCoordinate(Coordinate minimum, Coordinate maximum)
        {
            var bbox = new BoundingBox(
                minimum.Points[0], 
                minimum.Points[1], 
                maximum.Points[0],
                maximum.Points[1]
            );
            
            // We assume tileSize is correctly set, meaning that if we query each corner of the cartesian tile, that we
            // know all XYZTiles that may be there.
            var xyzTileBL = xyzTiles.FetchTileAtCoordinate(new Coordinate(CoordinateSystem.RD, bbox.MinX, bbox.MinY), zoomLevel);
            var xyzTileBR = xyzTiles.FetchTileAtCoordinate(new Coordinate(CoordinateSystem.RD, bbox.MaxX, bbox.MinY), zoomLevel);
            var xyzTileTR = xyzTiles.FetchTileAtCoordinate(new Coordinate(CoordinateSystem.RD, bbox.MaxX, bbox.MaxY), zoomLevel);
            var xyzTileTL = xyzTiles.FetchTileAtCoordinate(new Coordinate(CoordinateSystem.RD, bbox.MinX, bbox.MaxY), zoomLevel);
            return (xyzTileBL, xyzTileBR, xyzTileTR, xyzTileTL);
        }

        private (Coordinate tileCoordinateMin, Coordinate tileCoordinateMax) GetMinAndMaxCoordinate(Vector2Int tileKey)
        {
            var tileCoordinateMin = new Coordinate(CoordinateSystem.RD, tileKey.x, tileKey.y);
            var tileCoordinateMax = new Coordinate(CoordinateSystem.RD, tileKey.x + tileSize, tileKey.y + tileSize);
            return (tileCoordinateMin, tileCoordinateMax);
        }

        private void OnTextureDownloaded(Texture2D tex, TextureDecalProjector textureDecalProjector)
        {
            textureDecalProjector.SetSize((float)referenceTileWidth, (float)referenceTileWidth, (float)referenceTileHeight);
            textureDecalProjector.gameObject.SetActive(isEnabled);
            textureDecalProjector.SetTexture(tex);
            //force the depth to be at least larger than its height to prevent z-fighting
            if (!textureDecalProjector) return;

            DecalProjector decalProjector = textureDecalProjector.GetComponent<DecalProjector>();
            if (ProjectorHeight >= decalProjector.size.z)
            {
                textureDecalProjector.SetSize(decalProjector.size.x, decalProjector.size.y, ProjectorMinDepth);
            }

            //set the render index, to make sure the render order is maintained
            textureDecalProjector.SetPriority(renderIndex);
        }

        protected override void RemoveGameObjectFromTile(Vector2Int tileKey)
        {
            TryRemoveProjectorFrom(tileKey);

            base.RemoveGameObjectFromTile(tileKey);
        }

        private IEnumerator TryAddProjector(Vector2Int tileKey, XyzTiles.XyzTile xyzTile)
        {
            // The projector has already been added by another tile due to overlap, so we can continue
            var (_, foundAtmTile) = atmTiles.FirstOrDefault(pair => pair.Key.Equals(xyzTile));
            if (foundAtmTile != default)
            {
                if (foundAtmTile.LinkedCartesianTiles.Contains(tileKey) == false)
                {
                    foundAtmTile.LinkedCartesianTiles.Add(tileKey);
                }

                yield break;
            }

            var projector = Instantiate(ProjectorPrefab) as TextureDecalProjector;
            projector.name = xyzTile.ToString();
            var decalProjector = projector.GetComponent<DecalProjector>();
            projector.SetSize(tileSize, tileSize, decalProjector.size.z);
            projector.SetSize(tileSize, tileSize, decalProjector.size.z);
            // DecalProjector uses the position as center, but the position is bottomLeft; so we use the pivot
            // to make sure the positioning is from the bottomLeft
            decalProjector.pivot = new Vector3(tileSize * .5f, tileSize * .5f, 0);
            
            var projectorPosition = xyzTile.MinBound.ToUnity();
            projectorPosition.y = projector.transform.position.y;
            projector.transform.position = projectorPosition;

            var atmTile = new VisibleAtmTile()
            {
                XyzTile = xyzTile,
                Projector = projector,
                webRequest = null
            };
            atmTile.LinkedCartesianTiles.Add(tileKey);
            atmTiles.Add(xyzTile, atmTile);

            TemplatedUri templatedUri = xyzTile.URL
                .With("x", xyzTile.TileIndex.x.ToString())
                .With("y", xyzTile.TileIndex.y.ToString())
                .With("z", zoomLevel.ToString())
            ;
            atmTile.webRequest = UnityWebRequestTexture.GetTexture((Uri)templatedUri);
            yield return atmTile.webRequest.SendWebRequest();
            if (atmTile.webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"Could not download {xyzTile}: {atmTile.webRequest.error}");
                RemoveGameObjectFromTile(tileKey);
            }
            else
            {
                projector.ClearTexture();
                Texture texture = ((DownloadHandlerTexture)atmTile.webRequest.downloadHandler).texture;
                Texture2D tex = texture as Texture2D;
                tex.Compress(true);
                tex.filterMode = FilterMode.Bilinear;
                tex.Apply(false, true);
                OnTextureDownloaded(tex, projector);
            }
        }

        private void TryRemoveProjectorFrom(Vector2Int tileKey)
        {
            var (tileCoordinateMin, tileCoordinateMax) = GetMinAndMaxCoordinate(tileKey);
            var (xyzTileBL, xyzTileBR, xyzTileTR, xyzTileTL) = GetXyzTilesForMinAndMaxCoordinate(tileCoordinateMin, tileCoordinateMax);

            var (_, atmTileBR) = atmTiles.FirstOrDefault(pair => pair.Key.Equals(xyzTileBR));
            if (atmTileBR != default)
            {
                atmTileBR.LinkedCartesianTiles.Remove(tileKey);
                CleanupAtmTile(atmTileBR);
            }
            var (_, atmTileBL) = atmTiles.FirstOrDefault(pair => pair.Key.Equals(xyzTileBL));
            if (atmTileBL != default)
            {
                atmTileBL.LinkedCartesianTiles.Remove(tileKey);
                CleanupAtmTile(atmTileBL);
            }
            var (_, atmTileTR) = atmTiles.FirstOrDefault(pair => pair.Key.Equals(xyzTileTR));
            if (atmTileTR != default)
            {
                atmTileTR.LinkedCartesianTiles.Remove(tileKey);
                CleanupAtmTile(atmTileTR);
            }
            var (_, atmTileTL) = atmTiles.FirstOrDefault(pair => pair.Key.Equals(xyzTileTL));
            if (atmTileTL != default)
            {
                atmTileTL.LinkedCartesianTiles.Remove(tileKey);
                CleanupAtmTile(atmTileTL);
            }
        }

        private void CleanupAtmTile(VisibleAtmTile atmTile)
        {
            if (atmTile.LinkedCartesianTiles.Count != 0) return;

            Destroy(atmTile.Projector.gameObject);
            atmTiles.Remove(atmTile.XyzTile);
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

                queuedChanges.Add(new TileChange
                {
                    X = tile.Key.x,
                    Y = tile.Key.y,
                    action = TileAction.Upgrade
                });
            }

            if (!isEnabled)
            {
                queuedChanges.Clear();
                yield break;
            }

            while (queuedChanges.Count > 0)
            {
                // lets wait half a second in case a slider is moving
                if (Time.time - lastUpdatedTimeStamp > lastUpdatedInterval)
                {
                    TileChange next = queuedChanges[0];
                    queuedChanges.RemoveAt(0);
                    HandleTile(next);
                }
                yield return waitForSeconds;
            }

            updateTilesRoutine = null;
            visibleTilesDirty = false;
        }

        public override void LayerToggled()
        {
            base.LayerToggled();

            foreach (var atmTilePair in atmTiles)
            {
                atmTilePair.Value.Projector.gameObject.SetActive(isEnabled);
            }
        }
    }
}
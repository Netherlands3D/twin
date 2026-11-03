using System;
using System.Collections.Generic;
using Netherlands3D.Coordinates;
using UnityEngine;

namespace Netherlands3D.WMSTiles3D
{
    public class Tileset3DHandler
    {

        public Action<Tile> SubtreeCreator;
        public Action<Tile> ContentCreator;
        public Tile rootnode = null;
        public CoordinateSystem contentCoordinateSystem;

        private TilePrioritiser TilePrioritiser;
        public TilePrioritiser tilePrioritiser
        {
            set{TilePrioritiser = value;
                if(TilePrioritiser == null)
                {
                    usingPrioritiser = false;
                }
                else
                {
                    usingPrioritiser=true;
                }
            }
        }
        private bool usingPrioritiser =false;

        private int maxScreenHeightInPixels = 1080;

        private float sseComponent;
        public Camera currentCamera;

        private List<Tile> visibleTiles = new List<Tile>();

        public int maximumScreenSpaceError = 5;
        public void LoadInView()
        {
            if(rootnode == null)return;
            if(currentCamera==null)currentCamera = UnityEngine.Camera.main;
            rootnode.CountLoadingChildren();
            rootnode.CountLoadedChildren();
            SetSSEComponent();
            RecalculateAllTileBounds(rootnode);
            DisposeTilesOutsideView();
            destroyOutOfViewTilesets();
            LoadInViewRecursively(rootnode);
        }



        /// <summary>
        /// Screen-space error component calculation.
        /// Screen height is clamped to limit the amount of geometry that
        /// would be loaded on very high resolution displays.
        /// </summary>
        public void SetSSEComponent()
        {
            if (usingPrioritiser) maxScreenHeightInPixels = TilePrioritiser.MaxScreenHeightInPixels;

            var screenHeight = (maxScreenHeightInPixels > 0) ? Mathf.Min(maxScreenHeightInPixels, Screen.height) : Screen.height;

            if (currentCamera.orthographic)
            {
                sseComponent = screenHeight / currentCamera.orthographicSize;
            }
            else
            {
                var coverage = 2 * Mathf.Tan((Mathf.Deg2Rad * currentCamera.fieldOfView) / 2);
                sseComponent = screenHeight / coverage;
            }
        }
    
        private void RecalculateAllTileBounds(Tile tile)
        {
            if (tile == null) return;

            tile.CalculateUnitBounds();

            foreach (var child in tile.children)
            {
                RecalculateAllTileBounds(child);
            }
        }
    
        private void DisposeTilesOutsideView()
        {
            //Clean up list op previously loaded tiles outside of view
            for (int i = visibleTiles.Count - 1; i >= 0; i--)
            {
                var tile = visibleTiles[i];
                var tileIsInView = tile.IsInViewFrustrum(currentCamera);
                if (!tileIsInView)
                {
                    if(usingPrioritiser)
                    {
                    TilePrioritiser.RequestDispose(tile, true);
                    }
                    else
                    {
                        tile.Dispose();
                    }
                    visibleTiles.RemoveAt(i);
                    continue;
                }

                var closestPointOnBounds = tile.ContentBounds.ClosestPoint(currentCamera.transform.position); //Returns original point when inside the bounds
                CalculateTileScreenSpaceError(tile, currentCamera, closestPointOnBounds);
                var enoughDetail = tile.screenSpaceError < maximumScreenSpaceError;
                if (enoughDetail) // tile has (more then) enoug detail
                {
                    if (tile.parent.screenSpaceError<maximumScreenSpaceError) //parent tile also has enough detail
                    {
                        // can be removed if a parentTile is loaded
                        if (tile.parent.CountLoadedParents() > 0)
                        {
                            TilePrioritiser.RequestDispose(tile);
                            visibleTiles.RemoveAt(i);
                            continue;
                        }
                    }
                   

                }

                else //too little detail
                {
                    if (tile.refine=="ADD")
                    {
                        // tile should remain
                    }
                    else if (tile.loadingChildren == 0)
                    {
                        if (tile.loadedChildren > 0)
                        {
                            if(usingPrioritiser)
                            {
                                TilePrioritiser.RequestDispose(tile);
                            }
                            else
                            {
                                tile.Dispose();
                            

                            visibleTiles.RemoveAt(i);
                            }
                            
                        }
                    }


                }
            }
        }
    
         private void CalculateTileScreenSpaceError(Tile child, Camera currentMainCamera, Vector3 closestPointOnBounds)
        {
            float sse;
            if (currentMainCamera.orthographic)
            {
                //geometric error has no influence anymore so lets calculate the sse camera values
                Bounds bounds = child.ContentBounds;
                Vector3 extents = bounds.extents;
                Vector3 clampedExtents = Vector3.Min(extents, Vector3.one * 1000);
                float halfHeight = currentMainCamera.orthographicSize;
                float halfWidth = halfHeight * currentMainCamera.aspect;
                float fh2 = halfHeight * 2f;
                float fw2 = halfWidth * 2f;
                float frustumDiagonalSq = fw2 * fw2 + fh2 * fh2;
                float tx = clampedExtents.x * 2f;
                float tz = clampedExtents.z * 2f;
                float tileGroundSizeSq = tx * tx + tz * tz;
                float ratio = 0f;
                if (frustumDiagonalSq > 0f)
                    ratio = Mathf.Sqrt(tileGroundSizeSq / frustumDiagonalSq);

                float zoomFactor = Mathf.Clamp(halfHeight / 10f, 0.1f, 10f);
                float rawSSE = sseComponent * ratio * zoomFactor * 2f;
                sse = Mathf.Max(rawSSE, 0.5f);
            }
            
            else {
                float distance = Vector3.Distance(currentMainCamera.transform.position, closestPointOnBounds);
                if (distance < 0.1) distance = 1f;
                
                sse = (sseComponent * (float)child.geometricError) / distance;
            }
            child.screenSpaceError = sse;
        }
    
        private void destroyOutOfViewTilesets()
        {
            foreach (Tile child in rootnode.children)
            {
                child.DestroyChildTilesIfTilesetOutOfView(currentCamera);
            }
        }
    
        private void LoadInViewRecursively(Tile tile)
        {
            var tileIsInView = tile.IsInViewFrustrum(currentCamera);
            if (!tileIsInView)
            {
                return;
            }

            if (tile.isLoading == false && tile.children.Count == 0 && tile.hasSubtree)
            {
                tile.isLoading = true;
                SubtreeCreator(tile);
                return;
            }

            var closestPointOnBounds = tile.ContentBounds.ClosestPoint(currentCamera.transform.position); //Returns original point when inside the bounds
            CalculateTileScreenSpaceError(tile, currentCamera, closestPointOnBounds);
            var enoughDetail = tile.screenSpaceError < maximumScreenSpaceError;
            
            if (enoughDetail == false)  
            {
                    foreach (var childTile in tile.children)
                    {
                    LoadInViewRecursively(childTile);
                    }
            }
            else
            {
                    if (tile.hascontent)
                    {
                        if (!visibleTiles.Contains(tile))
                            {
                                visibleTiles.Add(tile);
                                ContentCreator(tile);
                                if (usingPrioritiser)
                        {
                            TilePrioritiser.RequestUpdate(tile);
                        }
                        else{
                                tile.content.Load();
                        }
                                
                            }
  
                    }
            }

           
        }
    
        public void DestroyAllVisibleTiles()
        {
        for (int i = visibleTiles.Count - 1; i >= 0; i--)
            {
                if(usingPrioritiser)
                {
                    TilePrioritiser.RequestDispose(visibleTiles[i],true);
                }
                else
                {
                visibleTiles[i].Dispose();
                }
                visibleTiles.RemoveAt(i);
            }
        }
    }
}

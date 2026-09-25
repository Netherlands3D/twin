using System;
using System.Collections.Generic;
using Netherlands3D.Coordinates;
using UnityEngine;

namespace Netherlands3D.Tilekit
{
    public class TileSelector
    {
        private readonly Plane[] frustumPlanes = new Plane[6];

        public void GetDesiredTilesNonAlloc(Camera camera, float maxViewDistance, int rootTileSize, int minTileSize,
            float splitThreshold, List<Tile> result)
        {
            result.Clear();
            
            GeometryUtility.CalculateFrustumPlanes(camera, frustumPlanes);
            
            var camCoord = new Coordinate(camera.transform.position).Convert(CoordinateSystem.RD);
            
            var minTileY = (int)Math.Floor((camCoord.northing - maxViewDistance) / rootTileSize);
            var maxTileY = (int)Math.Floor((camCoord.northing + maxViewDistance) / rootTileSize);
            var minTileX = (int)Math.Floor((camCoord.easting - maxViewDistance) / rootTileSize);
            var maxTileX = (int)Math.Floor((camCoord.easting + maxViewDistance) / rootTileSize);
            
            var maxViewDistanceSquared = maxViewDistance * maxViewDistance;

            for (var tileY = minTileY; tileY <= maxTileY; tileY++)
            {
                for (var tileX = minTileX; tileX <= maxTileX; tileX++)
                {
                    var tile = new Tile(tileX * rootTileSize, tileY * rootTileSize, rootTileSize);
                    var tileBounds = TileMath.GetUnityBounds(tile);
                    
                    var nearestRdX = Math.Clamp(camCoord.easting, tile.RdX, tile.RdX + tile.Size);
                    var nearestRdY = Math.Clamp(camCoord.northing, tile.RdY, tile.RdY + tile.Size);

                    var deltaX = camCoord.easting - nearestRdX;
                    var deltaY = camCoord.northing - nearestRdY;

                    if (deltaX * deltaX + deltaY * deltaY > maxViewDistanceSquared)
                    {
                        //Remove tiles that are too far away. (This calculation is not exact.)
                       continue;
                    }
                    
                    if (!GeometryUtility.TestPlanesAABB(frustumPlanes, tileBounds))
                    {
                        continue;
                    }

                    SelectDesiredTiles(camera, tile, minTileSize, splitThreshold, result);

                }
            }
            
        }

        private void SelectDesiredTiles(Camera camera, Tile candidate, int minTileSize, float splitThreshold,
            List<Tile> result)
        {
            if (!ShouldSplit(camera, candidate, minTileSize, splitThreshold))
            {
                result.Add(candidate);
                return;
            }
            var childSize = candidate.Size / 2;
            var x = candidate.RdX;
            var y = candidate.RdY;
            
            SelectDesiredTiles(camera, new Tile(x, y, childSize), minTileSize, splitThreshold, result);
            SelectDesiredTiles(camera, new Tile(x + childSize, y, childSize), minTileSize, splitThreshold, result);
            SelectDesiredTiles(camera, new Tile(x, y + childSize, childSize), minTileSize, splitThreshold, result);
            SelectDesiredTiles(camera, new Tile(x + childSize, y + childSize, childSize), minTileSize, splitThreshold, result);
        }

        private bool ShouldSplit(Camera camera, Tile tile, int minTileSize, float splitThreshold)
        {
            if (tile.Size <= minTileSize) return false;
            
            var bounds = TileMath.GetUnityBounds(tile);
            if (!GeometryUtility.TestPlanesAABB(frustumPlanes, bounds))
            {
                return false;
            }

            var viewportRect = TileMath.GetBoundsViewportRect(camera, bounds);
            var tileScreenSize = Mathf.Min(
                viewportRect.width * camera.pixelWidth, viewportRect.height * camera.pixelHeight);

            //DrawDebugLineViewport(camera, new Vector2(0,0), new Vector2(1,1), Color.green);
            //DrawDebugLineViewport(camera, new Vector2(viewportRect.min.x, viewportRect.min.y), new Vector2(viewportRect.max.x, viewportRect.min.y), Color.green);
            //DrawDebugLineViewport(camera, new Vector2(viewportRect.min.x, viewportRect.max.y), new Vector2(viewportRect.min.x, viewportRect.min.y), Color.green);
            //DrawDebugLineViewport(camera, new Vector2(viewportRect.max.x, viewportRect.max.y), new Vector2(viewportRect.min.x, viewportRect.max.y), Color.green);
            //DrawDebugLineViewport(camera, new Vector2(viewportRect.max.x, viewportRect.min.y), new Vector2(viewportRect.max.x, viewportRect.min.y), Color.green);

            return tileScreenSize > splitThreshold;
        }
        
        
        public void DrawDebugLineViewport(Camera camera, Vector2 startViewport, Vector2 endViewport, Color color, float duration = 0f)
        {
            try
            {
                // Place the line slightly in front of the camera lens so it doesn't get culled
                float depth = camera.nearClipPlane + 0.1f;

                // Convert normalized viewport coordinates to 3D world space positions
                Vector3 worldStart = camera.ViewportToWorldPoint(new Vector3(1-startViewport.x, startViewport.y, depth));
                Vector3 worldEnd = camera.ViewportToWorldPoint(new Vector3(1-endViewport.x, endViewport.y, depth));

                // Draw the line in the scene
                Debug.DrawLine(worldStart, worldEnd, color, duration);
            }
            catch
            {
                
            }
        }

        
    }
}

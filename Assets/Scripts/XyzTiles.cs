using System;
using Netherlands3D.Coordinates;
using UnityEngine;

namespace Netherlands3D.Twin
{
    /// <see href="en.wikipedia.org/wiki/Tiled_web_map"/>
    public class XyzTiles : MonoBehaviour
    {
        [SerializeField] private string url = @"https://images.huygens.knaw.nl/webmapper/maps/pw-1943/{z}/{x}/{y}.png";
        
        /// <summary>
        /// Initialize a Quadtree with the boundaries for an XYZTiles, which is a modified EPSG:3857 projection space
        /// that is made square by using the same projected height as the projected width. (Yes, this means the top
        /// and bottom cells do not contain data when mapped onto a EPSG:3857 map, but this is deemed acceptable as
        /// this makes working with the grid easier by it being square)
        /// </summary>
        private readonly QuadTree quadTree = new(
            -20037508.34d, 
            -20037508.34d, 
            20037508.34d, 
            20037508.34d
        );

        public struct XyzTile
        {
            public Vector2Int TileIndex;
            public int ZoomLevel { get; set; }
            public Coordinate MinBound;
            public Coordinate MaxBound;
            public string URL;
        }
        
        public XyzTile FetchTileAtCoordinate(Coordinate at, int zoomLevel)
        {
            // Ensure the coordinate is in EPSG:3857
            at = at.Convert(CoordinateSystem.WGS84_PseudoMercator);
            
            // Derive the TileIndex from the given Coordinate
            var tileIndex = this.CoordinateToTileXY(at, zoomLevel);
            
            // Determine the bounds from the tile index
            var (minBound, maxBound) = this.FromTileXYToBoundingBox(tileIndex, zoomLevel);

            return new XyzTile()
            {
                TileIndex = tileIndex,
                ZoomLevel = zoomLevel,
                URL = this.GetTileUrl(tileIndex, zoomLevel),
                MaxBound = maxBound,
                MinBound = minBound
            };
        }

        private string GetTileUrl(Vector2Int tileIndex, int zoomLevel)
        {
            return url.Replace("{z}", zoomLevel.ToString())
                .Replace("{x}", tileIndex.x.ToString())
                .Replace("{y}", tileIndex.y.ToString());
        }

        private Vector2Int CoordinateToTileXY(Coordinate coord, int zoomLevel)
        {
            if (coord.CoordinateSystem != (int)CoordinateSystem.WGS84_PseudoMercator)
            {
                throw new Exception("Only WGS84 Pseudomercator (EPSG:3857) is supported");
            }

            Vector2Int tileIndex = quadTree.GetTileIndex(
                coord.Points[0], 
                coord.Points[1], 
                zoomLevel
            );
            
            return new(tileIndex.x, tileIndex.y);
        }

        public (Coordinate min, Coordinate max) FromTileXYToBoundingBox(Vector2Int tileXy, int zoomLevel)
        {
            var boundingBox = quadTree.GetTileBoundingBox(tileXy, zoomLevel);

            const CoordinateSystem crs = CoordinateSystem.WGS84_PseudoMercator;
            var min = new Coordinate(crs, boundingBox.MinX, boundingBox.MinY, 0);
            var max = new Coordinate(crs, boundingBox.MaxX, boundingBox.MaxY, 0);
            
            return (min, max);
        }
    }
}
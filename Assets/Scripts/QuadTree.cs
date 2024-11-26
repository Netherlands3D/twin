using System;
using System.Xml;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class BoundingBox
    {
        public double MinX { get; }
        public double MinY { get; }
        public double MaxX { get; }
        public double MaxY { get; }

        public double cx;
        public double cy;

        public BoundingBox(double minX, double minY, double maxX, double maxY)
        {
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
            cx = (maxX - minX) * 0.5f + minX;
            cy = (maxY - minY) * 0.5f + minY;
        }

        public double Width => MaxX - MinX;
        public double Height => MaxY - MinY;

        public (double centerX, double centerY) Center => (cx, cy);

        public bool Contains(double x, double y)
        {
            return x >= MinX && x <= MaxX && y >= MinY && y <= MaxY;
        }

        public override string ToString()
        {
            return $"BoundingBox(MinX: {MinX}, MinY: {MinY}, MaxX: {MaxX}, MaxY: {MaxY})";
        }
    }

    /// <summary>
    /// A quadtree operates in 'space', which is the boundingbox when of the whole area when z=0. For every level of Z,
    /// the space is subdivided into quads (hence the name quadtree).
    ///
    /// Using the methods in this object, you can find the tile for a given z value and coordinate within the space of
    /// the quadtree.
    /// </summary>
    public class QuadTree
    {
        public readonly BoundingBox boundingBox;

        public QuadTree(BoundingBox boundingBox)
        {
            this.boundingBox = boundingBox;
        }

        public QuadTree(double minX, double minY, double maxX, double maxY)
        {
            boundingBox = new BoundingBox(minX, minY, maxX, maxY);
        }
        
        /// <summary>
        /// Returns the bounding box in meters according within the bounds according to subdivision of the boundingbox
        /// in the constructor.
        /// </summary>
        public BoundingBox GetTileBoundingBox(Vector2Int tileIndex, int z)
        {
            var (tileSizeX, tileSizeY) = GetTileSizeInMeters(z);

            // Calculate minX and maxX
            double tileMinX = this.boundingBox.MinX + tileIndex.x * tileSizeX;
            double tileMaxX = tileMinX + tileSizeX;

            // Calculate minY and maxY (note Y increases downwards in the tile grid)
            double tileMaxY = this.boundingBox.MaxY - tileIndex.y * tileSizeY;
            double tileMinY = tileMaxY - tileSizeY;            

            return new BoundingBox(tileMinX, tileMinY, tileMaxX, tileMaxY);
        }      

        public Vector2Int GetTileIndex(double x, double y, int zoomLevel)
        {
            // Calculate the size of each tile in meters at the given zoom level
            var (tileSizeX, tileSizeY) = GetTileSizeInMeters(zoomLevel);

            // Calculate the X and Y indices
            int tileIndexX = (int)((x - this.boundingBox.MinX) / tileSizeX);
            int tileIndexY = (int)((this.boundingBox.MaxY - y) / tileSizeY);
            //int tileIndexY = (int)((y - this.boundingBox.MinY) / tileSizeY);

            return new Vector2Int(tileIndexX, tileIndexY);
        }

        public (double width, double height) GetTileSizeInMeters(int zoomLevel)
        {
            var width = this.boundingBox.Width / Math.Pow(2, zoomLevel);
            var height = this.boundingBox.Height / Math.Pow(2, zoomLevel);

            return (width, height);
        }
    }
}
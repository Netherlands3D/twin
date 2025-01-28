using System;
using Netherlands3D.Coordinates;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class BoundingBox
    {
        public Coordinate BottomLeft { get; private set; }
        public Coordinate TopRight { get; private set; }
        public CoordinateSystem CoordinateSystem { get; private set; }

        public BoundingBox(Coordinate bottomLeft, Coordinate topRight)
        {
            if (bottomLeft.Points[0] > topRight.Points[0] || bottomLeft.Points[1] > topRight.Points[1])
            {
                throw new ArgumentException(
                    "Invalid coordinates for BoundingBox. BottomLeft should have lower values than TopRight."
                );
            }

            if (topRight.CoordinateSystem != bottomLeft.CoordinateSystem)
            {
                topRight = topRight.Convert((CoordinateSystem)bottomLeft.CoordinateSystem);
            }

            BottomLeft = bottomLeft;
            TopRight = topRight;
            CoordinateSystem = (CoordinateSystem)bottomLeft.CoordinateSystem;
        }

        public void Convert(CoordinateSystem coordinateSystem)
        {
            BottomLeft = BottomLeft.Convert(coordinateSystem);
            TopRight = TopRight.Convert(coordinateSystem);
        }

        // Method to check if this BoundingBox contains another BoundingBox completely
        public bool Contains(BoundingBox other)
        {
            if (other.CoordinateSystem != CoordinateSystem)
                other = ConvertToCRS(other, CoordinateSystem);
            
            return other.BottomLeft.Points[0] >= this.BottomLeft.Points[0] &&
                   other.BottomLeft.Points[1] >= this.BottomLeft.Points[1] &&
                   other.TopRight.Points[0] <= this.TopRight.Points[0] &&
                   other.TopRight.Points[1] <= this.TopRight.Points[1];
        }

        
        public bool Intersects(BoundingBox other)
        {
            if (other.CoordinateSystem != CoordinateSystem)
                other = ConvertToCRS(other, CoordinateSystem);
            
            // Check if one box is to the left of the other
            if (
                this.TopRight.Points[0] < other.BottomLeft.Points[0] ||
                other.TopRight.Points[0] < this.BottomLeft.Points[0]
            )
            {
                return false;
            }

            // Check if one box is above the other
            if (
                this.TopRight.Points[1] < other.BottomLeft.Points[1] ||
                other.TopRight.Points[1] < this.BottomLeft.Points[1]
            )
            {
                return false;
            }

            return true;
        }

        private BoundingBox ConvertToCRS(BoundingBox box, CoordinateSystem newCoordinateSystem)
        {
            var bottomLeft = box.BottomLeft.Convert(newCoordinateSystem);
            var topRight = box.TopRight.Convert(newCoordinateSystem);
            return new BoundingBox(bottomLeft, topRight);
        }

        /// <summary>
        /// Returns the string as a WMS bounding box string
        /// </summary>
        public override string ToString()
        {
            return $"{BottomLeft.easting},{BottomLeft.northing},{TopRight.easting},{TopRight.northing}";
        }
    }
}
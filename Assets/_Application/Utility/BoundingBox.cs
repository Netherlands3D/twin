using System;
using Netherlands3D.Coordinates;
using UnityEngine;

namespace Netherlands3D.Twin.Utility
{
    public class BoundingBox
    {
        public Coordinate BottomLeft { get; private set; }
        public Coordinate TopRight { get; private set; }
        public CoordinateSystem CoordinateSystem { get; private set; }

        public BoundingBox(Coordinate bottomLeft, Coordinate topRight)
        {
            if (bottomLeft.value1 > topRight.value1 || bottomLeft.value2 > topRight.value2)
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

            if (BottomLeft.PointsLength == 2)
            {
                return other.BottomLeft.easting >= this.BottomLeft.easting &&
                       other.BottomLeft.northing >= this.BottomLeft.northing &&
                       other.TopRight.easting <= this.TopRight.easting &&
                       other.TopRight.northing <= this.TopRight.northing;   
            }

            return other.BottomLeft.easting >= this.BottomLeft.easting &&
                   other.BottomLeft.northing >= this.BottomLeft.northing &&
                   other.BottomLeft.height >= this.BottomLeft.height &&
                   other.TopRight.easting <= this.TopRight.easting &&
                   other.TopRight.northing <= this.TopRight.northing &&
                   other.TopRight.height <= this.TopRight.height;
        }

        public bool Contains(Coordinate coordinate)
        {
            if ((CoordinateSystem)coordinate.CoordinateSystem != CoordinateSystem)
                coordinate = coordinate.Convert(CoordinateSystem);

            if (BottomLeft.PointsLength == 2)
            {
                return coordinate.easting >= this.BottomLeft.easting &&
                       coordinate.northing >= this.BottomLeft.northing &&
                       coordinate.easting <= this.TopRight.easting &&
                       coordinate.northing <= this.TopRight.northing;
            }

            return coordinate.easting >= this.BottomLeft.easting &&
                   coordinate.northing >= this.BottomLeft.northing &&
                   coordinate.height >= this.BottomLeft.height &&
                   coordinate.easting <= this.TopRight.easting &&
                   coordinate.northing <= this.TopRight.northing &&
                   coordinate.height <= this.TopRight.height;
        }


        public bool Intersects(BoundingBox other)
        {
            if (other.CoordinateSystem != CoordinateSystem)
                other = ConvertToCRS(other, CoordinateSystem);

            if (BottomLeft.PointsLength == 2)
            {
                return !(TopRight.easting < other.BottomLeft.easting || BottomLeft.easting > other.TopRight.easting ||
                         TopRight.northing < other.BottomLeft.northing || BottomLeft.northing > other.TopRight.northing);
            }

            return !(TopRight.easting < other.BottomLeft.easting || BottomLeft.easting > other.TopRight.easting ||
                     TopRight.northing < other.BottomLeft.northing || BottomLeft.northing > other.TopRight.northing ||
                     TopRight.height < other.BottomLeft.height || BottomLeft.height > other.TopRight.height);
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
using System;
using Netherlands3D.Coordinates;
using UnityEngine;

namespace Netherlands3D.Twin.Utility
{
    public class BoundingBox
    {
        public Coordinate BottomLeft { get; private set; }
        public Coordinate TopRight { get; private set; }
        public Coordinate Center => (BottomLeft + TopRight) * 0.5f;
        public Coordinate Size => TopRight - BottomLeft;

        public CoordinateSystem CoordinateSystem { get; private set; }

        public BoundingBox(Bounds bounds, Vector3 offset)
        {
            BottomLeft = new Coordinate(bounds.min + offset);
            TopRight = new Coordinate(bounds.max + offset);
            CoordinateSystem = (CoordinateSystem)BottomLeft.CoordinateSystem;
        }
        
        public BoundingBox(Coordinate bottomLeft, Coordinate topRight)
        {
            if (topRight.CoordinateSystem != bottomLeft.CoordinateSystem)
            {
                topRight = topRight.Convert((CoordinateSystem)bottomLeft.CoordinateSystem);
            }

            if (bottomLeft.easting > topRight.easting || bottomLeft.northing > topRight.northing)
            {
                throw new ArgumentException(
                    "Invalid coordinates for BoundingBox. BottomLeft should have lower values than TopRight."
                );
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
                other.Convert(CoordinateSystem);

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
                other.Convert(CoordinateSystem);

            if (BottomLeft.PointsLength == 2)
            {
                return !(TopRight.easting < other.BottomLeft.easting || BottomLeft.easting > other.TopRight.easting ||
                         TopRight.northing < other.BottomLeft.northing || BottomLeft.northing > other.TopRight.northing);
            }

            return !(TopRight.easting < other.BottomLeft.easting || BottomLeft.easting > other.TopRight.easting ||
                     TopRight.northing < other.BottomLeft.northing || BottomLeft.northing > other.TopRight.northing ||
                     TopRight.height < other.BottomLeft.height || BottomLeft.height > other.TopRight.height);
        }               

        /// <summary>
        /// Returns the string as a WMS bounding box string
        /// </summary>
        public override string ToString()
        {
            return $"{BottomLeft.easting},{BottomLeft.northing},{TopRight.easting},{TopRight.northing}";
        }
        
        public double GetSizeMagnitude()
        {
            var size = Size;
            var d = (size.easting * size.easting) + (size.northing * size.northing) + (size.height * size.height);
            return Math.Sqrt(d);
        }
        
        //RDBounds as defined by https://epsg.io/28992
        public static BoundingBox RDBounds => new BoundingBox(new Coordinate(CoordinateSystem.RD, 482.06d, 306602.42d), new Coordinate(CoordinateSystem.RD, 284182.97d, 637049.52d));


        public void Debug(Color color)
        {
            float height = 100;
            Vector3 unityBottomLeft = BottomLeft.ToUnity();
            unityBottomLeft.y = height;
            Vector3 unityTopRight = TopRight.ToUnity();
            unityTopRight.y = height;
            Vector3 unityBottomRight = new Vector3(unityTopRight.x, height, unityBottomLeft.z);
            Vector3 unityTopLeft = new Vector3(unityBottomLeft.x, height, unityTopRight.z);

            UnityEngine.Debug.DrawLine(unityBottomLeft, unityBottomRight, color);
            UnityEngine.Debug.DrawLine(unityBottomRight, unityTopRight, color);
            UnityEngine.Debug.DrawLine(unityTopRight, unityTopLeft, color);
            UnityEngine.Debug.DrawLine(unityTopLeft, unityBottomLeft, color);
        }
    }
}
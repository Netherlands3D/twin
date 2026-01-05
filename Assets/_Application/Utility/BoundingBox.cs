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

        public CoordinateSystem CoordinateSystem => (CoordinateSystem)BottomLeft.CoordinateSystem;
        
        public BoundingBox(Bounds worldSpaceBounds)
        {
            BottomLeft = new Coordinate(worldSpaceBounds.min);
            TopRight = new Coordinate(worldSpaceBounds.max);
        }
        
        public BoundingBox(Coordinate bottomLeft, Coordinate topRight)
        {
            if (topRight.CoordinateSystem != bottomLeft.CoordinateSystem)
            {
                topRight = topRight.Convert(CoordinateSystem);
            }

            if (bottomLeft.easting > topRight.easting || bottomLeft.northing > topRight.northing)
            {
                throw new ArgumentException(
                    "Invalid coordinates for BoundingBox. BottomLeft should have lower values than TopRight."
                );
            }

            BottomLeft = bottomLeft;
            TopRight = topRight;
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

        public void Encapsulate(Bounds bounds)
        {
            Encapsulate(new Coordinate(bounds.center - bounds.extents));
            Encapsulate(new Coordinate(bounds.center + bounds.extents));
        }
        
        public void Encapsulate(BoundingBox bounds)
        {
            if(bounds == null)
                return;
            
            Encapsulate(bounds.Center - bounds.Size * 0.5f);
            Encapsulate(bounds.Center + bounds.Size * 0.5f);
        }
        
        public void Encapsulate(Coordinate coordinate)
        {
            coordinate = coordinate.Convert(CoordinateSystem);
            var blv1 = Min(coordinate.value1, BottomLeft.value1);
            var blv2 = Min(coordinate.value2, BottomLeft.value2);
            var trv1 = Max(coordinate.value1, TopRight.value1);
            var trv2 = Max(coordinate.value2, TopRight.value2);
            if (BottomLeft.PointsLength == 2)
            {
                BottomLeft = new Coordinate(CoordinateSystem, blv1, blv2);
                TopRight = new Coordinate(CoordinateSystem, trv1, trv2);
                return;
            }
            var blv3 = Min(coordinate.value3, BottomLeft.value3);
            var trv3 = Max(coordinate.value3, TopRight.value3);
            
            BottomLeft = new Coordinate(CoordinateSystem, blv1, blv2, blv3);
            TopRight = new Coordinate(CoordinateSystem, trv1, trv2, trv3);
        }

        public Bounds ToUnityBounds()
        {
            // size.ToUnity does not create an accurate size, we should consider returning a Vector3Double instead of a Coordinate if we do calculations on coordinates 
            var size = new Vector3((float)Size.easting, (float)Size.height, (float)Size.northing);
            return new Bounds(Center.ToUnity(), size);
        }

        private static double Min(double lhs, double rhs)
        {
            return lhs < rhs ? lhs : rhs;
        }

        private static double Max(double lhs, double rhs)
        {
            return lhs > rhs ? lhs : rhs;
        }

        public bool Equals(BoundingBox other)
        {
            if (other == null) return false;
            if (other.CoordinateSystem != CoordinateSystem)
                other.Convert(CoordinateSystem);

            return BottomLeft.Equals(other.BottomLeft) && TopRight.Equals(other.TopRight);
        }

        public void Debug(Color color)
        {
            Vector3 unityBottomLeft = BottomLeft.ToUnity();
            Vector3 unityTopRight = TopRight.ToUnity();
            float down = Mathf.Min(unityBottomLeft.y, unityTopRight.y);
            float up = Mathf.Max(unityBottomLeft.y, unityTopRight.y);
            Vector3 unityBottomRightDown = new Vector3(unityTopRight.x, down, unityBottomLeft.z);
            Vector3 unityTopLeftDown = new Vector3(unityBottomLeft.x, down, unityTopRight.z);
            Vector3 unityBottomLeftDown = new Vector3(unityBottomLeft.x, down, unityBottomLeft.z);
            Vector3 unityTopRightDown = new Vector3(unityTopRight.x, down, unityTopRight.z);

            Vector3 unityBottomRightUp = new Vector3(unityTopRight.x, up, unityBottomLeft.z);
            Vector3 unityTopLeftUp = new Vector3(unityBottomLeft.x, up, unityTopRight.z);
            Vector3 unityBottomLeftUp = new Vector3(unityBottomLeft.x, up, unityBottomLeft.z);
            Vector3 unityTopRightUp = new Vector3(unityTopRight.x, up, unityTopRight.z);

            UnityEngine.Debug.DrawLine(unityBottomLeftDown, unityBottomRightDown, color);
            UnityEngine.Debug.DrawLine(unityBottomRightDown, unityTopRightDown, color);
            UnityEngine.Debug.DrawLine(unityTopRightDown, unityTopLeftDown, color);
            UnityEngine.Debug.DrawLine(unityTopLeftDown, unityBottomLeftDown, color);

            UnityEngine.Debug.DrawLine(unityBottomLeftDown, unityBottomLeftUp, color);
            UnityEngine.Debug.DrawLine(unityBottomRightDown, unityBottomRightUp, color);
            UnityEngine.Debug.DrawLine(unityTopRightDown, unityTopRightUp, color);
            UnityEngine.Debug.DrawLine(unityTopLeftDown, unityTopLeftUp, color);

            UnityEngine.Debug.DrawLine(unityBottomLeftUp, unityBottomRightUp, color);
            UnityEngine.Debug.DrawLine(unityBottomRightUp, unityTopRightUp, color);
            UnityEngine.Debug.DrawLine(unityTopRightUp, unityTopLeftUp, color);
            UnityEngine.Debug.DrawLine(unityTopLeftUp, unityBottomLeftUp, color);
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Coordinates
{
    /// <summary>
    /// Operations for WGS84LatLonHeight
    /// </summary>
    class WGS84_LatLonHeight_Operations : CoordinateSystemOperation
    {
        public override Coordinate ConvertFromWGS84LatLonH(Coordinate coordinate)
        {
            return coordinate;
        }

        public override Coordinate ConvertToWGS84LatLonH(Coordinate coordinate)
        {
            return coordinate;
        }

        public override bool CoordinateIsValid(Coordinate coordinate)
        {
            if (coordinate.Points.Length!=3)
            {
                return false;
            }
            if (coordinate.Points[0]>90d)
            {
                return false;
            }
            if (coordinate.Points[0] < -90d)
            {
                return false;
            }
            if (coordinate.Points[1] > 180d)
            {
                return false;
            }
            if (coordinate.Points[1] < -180d)
            {
                return false;
            }
            return true;
        }

        public override CoordinateSystemGroup GetCoordinateSystemGroup()
        {
            return CoordinateSystemGroup.WGS84;
        }

        public override CoordinateSystemType GetCoordinateSystemType()
        {
            return CoordinateSystemType.Geographic;
        }

        public override Vector3WGS GlobalUpDirection(Coordinate coordinate)
        {
            return new Vector3WGS(coordinate.Points[1], coordinate.Points[0], 0);
        }

        public override Vector3WGS LocalUpDirection(Coordinate coordinate)
        {
            return GlobalUpDirection(coordinate);
        }

        public override Vector3WGS Orientation()
        {
            return new Vector3WGS(0, 0, 0);
        }
    }
}

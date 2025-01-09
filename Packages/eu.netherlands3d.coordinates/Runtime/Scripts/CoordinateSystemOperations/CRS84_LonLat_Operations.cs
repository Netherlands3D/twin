using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Coordinates
{
    /// <summary>
    /// Operations for WGS84LatLon
    /// </summary>
    class CRS84_LonLat_Operations : CoordinateSystemOperation
    {
        public override string Code()
        {
            return "CRS84";
        }

        public override int NorthingIndex()
        {
            return 1;
        }
        public override int EastingIndex()
        {
            return 0;
        }
        public override int AxisCount()
        {
            return 2;
        }
        public override Coordinate ConvertFromWGS84LatLonH(Coordinate coordinate)
        {
            //double[] newPoints = new double[2] { coordinate.Points[1], coordinate.Points[0] };
            Coordinate result = new Coordinate(CoordinateSystem.CRS84, coordinate.y, coordinate.x);
            result.extraLattitudeRotation = coordinate.extraLattitudeRotation;
            result.extraLongitudeRotation = coordinate.extraLongitudeRotation;
            return result;
        }

        public override Coordinate ConvertToWGS84LatLonH(Coordinate coordinate)
        {
            //double[] newPoints = new double[3] { coordinate.Points[1], coordinate.Points[0],0 };
            Coordinate result = new Coordinate(CoordinateSystem.WGS84_LatLonHeight, coordinate.y, coordinate.x, 0);
            result.extraLattitudeRotation = coordinate.extraLattitudeRotation;
            result.extraLongitudeRotation = coordinate.extraLongitudeRotation;
            return result;
        }

        public override bool CoordinateIsValid(Coordinate coordinate)
        {
            if (coordinate.PointsLength != 2)
            {
                return false;
            }
            if (coordinate.y > 90d)
            {
                return false;
            }
            if (coordinate.y < -90d)
            {
                return false;
            }
            if (coordinate.x > 180d)
            {
                return false;
            }
            if (coordinate.x < -180d)
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
            return new Vector3WGS(coordinate.x, coordinate.y, 0);
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

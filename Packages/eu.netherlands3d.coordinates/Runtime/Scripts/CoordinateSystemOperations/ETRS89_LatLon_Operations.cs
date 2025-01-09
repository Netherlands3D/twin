using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Coordinates
{
    /// <summary>
    /// Operations for WGS84LatLon
    /// </summary>
    class ETRS89_LatLon_Operations : CoordinateSystemOperation
    {
        public override string Code()
        {
            return "4258";
        }

        public override int NorthingIndex()
        {
            return 0;
        }
        public override int EastingIndex()
        {
            return 1;
        }

        public override int AxisCount()
        {
            return 2;
        }
        public override Coordinate ConvertFromWGS84LatLonH(Coordinate coordinate)
        {
            //double[] newPoints = new double[2] { coordinate.Points[0], coordinate.Points[1] };
            Coordinate result = new Coordinate(CoordinateSystem.ETRS89_LatLon, coordinate.x, coordinate.y);
            result.extraLattitudeRotation = coordinate.extraLattitudeRotation;
            result.extraLongitudeRotation = coordinate.extraLongitudeRotation;
            return result;
        }

        public override Coordinate ConvertToWGS84LatLonH(Coordinate coordinate)
        {
            //double[] newPoints = new double[3] { coordinate.Points[0], coordinate.Points[1],0 };
            Coordinate result = new Coordinate(CoordinateSystem.WGS84_LatLonHeight, coordinate.x, coordinate.y, 0);
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
            if (coordinate.x > 84.73d)
            {
                return false;
            }
            if (coordinate.x < 40.18d)
            {
                return false;
            }
            if (coordinate.y > 32.88d)
            {
                return false;
            }
            if (coordinate.y < -16.1d)
            {
                return false;
            }
            return true;
        }

        public override CoordinateSystemGroup GetCoordinateSystemGroup()
        {
            return CoordinateSystemGroup.ETRS89;
        }

        public override CoordinateSystemType GetCoordinateSystemType()
        {
            return CoordinateSystemType.Geographic;
        }

        public override Vector3WGS GlobalUpDirection(Coordinate coordinate)
        {
            return new Vector3WGS(coordinate.y, coordinate.x, 0);
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

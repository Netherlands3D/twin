using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Coordinates
{/// <summary>
/// Operations for WGS84ECEF
/// </summary>
    class ETRS89_ECEF_Operations : CoordinateSystemOperation
    {
        GeographicToGeocentricSettings conversionSettings = new GeographicToGeocentricSettings(0d, 6378137d, 298.257222101);

        public override Coordinate ConvertFromWGS84LatLonH(Coordinate coordinate)
        {
            Coordinate result = GeographicToGeocentric.Forward(coordinate, conversionSettings);
            Coordinate output = new Coordinate(CoordinateSystem.ETRS89_ECEF, result.Points);
            output.extraLattitudeRotation = coordinate.extraLattitudeRotation;
            output.extraLongitudeRotation = coordinate.extraLongitudeRotation;
            return output;

        }

        public override Coordinate ConvertToWGS84LatLonH(Coordinate coordinate)
        {
            Coordinate result = GeographicToGeocentric.Reverse(coordinate, conversionSettings);
            Coordinate output = new Coordinate(CoordinateSystem.WGS84_LatLonHeight, result.Points);
            output.extraLattitudeRotation = result.Points[0];
            output.extraLongitudeRotation = result.Points[1];

            return output;
        }

        public override bool CoordinateIsValid(Coordinate coordinate)
        {
            if (coordinate.Points.Length!=3)
            {
                return false;
            }
            if (coordinate.Points[0] > 449077.15d)
            {
                return false;
            }
            if (coordinate.Points[0] < -1486881.13d)
            {
                return false;
            }
            if (coordinate.Points[1] > 5361250.91d)
            {
                return false;
            }
            if (coordinate.Points[1] < 3459328.1d)
            {
                return false;
            }
            double radiusSquared = (coordinate.Points[0] * coordinate.Points[0]) + (coordinate.Points[1] * coordinate.Points[1]) + (coordinate.Points[2] * coordinate.Points[2]);
            double minRadiusSquared = 6370000d*6370000d;
            if (radiusSquared<minRadiusSquared)
            {
                return false;
            }
            double maxRadiusSquared = 6500000d * 6500000d;
            if (radiusSquared>maxRadiusSquared)
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
            return CoordinateSystemType.Geocentric;
        }

        public override Vector3WGS GlobalUpDirection(Coordinate coordinate)
        {
            Coordinate wgs = ConvertToWGS84LatLonH(coordinate);
            Vector3WGS result = new Vector3WGS(wgs.Points[1], wgs.Points[0], 0);
            return result;
        }

        public override Vector3WGS LocalUpDirection(Coordinate coordinate)
        {
            return GlobalUpDirection(coordinate);
        }

        public override Vector3WGS Orientation()
        {
            return new Vector3WGS(0, 90, 0);
        }
    }
}

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

        public override string Code()
        {
            return "4936";
        }

        public override Coordinate ConvertFromWGS84LatLonH(Coordinate coordinate)
        {
            Coordinate result = GeographicToGeocentric.Forward(coordinate, conversionSettings);
            //Coordinate output = new Coordinate(CoordinateSystem.ETRS89_ECEF, result.Points);
            Coordinate output = new Coordinate(CoordinateSystem.ETRS89_ECEF, result.value1, result.value2, result.value3);
            output.extraLattitudeRotation = coordinate.extraLattitudeRotation;
            output.extraLongitudeRotation = coordinate.extraLongitudeRotation;
            return output;

        }

        public override Coordinate ConvertToWGS84LatLonH(Coordinate coordinate)
        {
            Coordinate result = GeographicToGeocentric.Reverse(coordinate, conversionSettings);
            //Coordinate output = new Coordinate(CoordinateSystem.WGS84_LatLonHeight, result.Points);
            Coordinate output = new Coordinate(CoordinateSystem.WGS84_LatLonHeight, result.value1, result.value2, result.value3);
            output.extraLattitudeRotation = coordinate.extraLattitudeRotation;
            output.extraLongitudeRotation = coordinate.extraLongitudeRotation;

            return output;
        }

        public override bool CoordinateIsValid(Coordinate coordinate)
        {
            if (coordinate.PointsLength != 3)
            {
                return false;
            }
            if (coordinate.value1 > 449077.15d)
            {
                return false;
            }
            if (coordinate.value1 < -1486881.13d)
            {
                return false;
            }
            if (coordinate.value2 > 5361250.91d)
            {
                return false;
            }
            if (coordinate.value2 < 3459328.1d)
            {
                return false;
            }
            double radiusSquared = (coordinate.value1 * coordinate.value1) + (coordinate.value2 * coordinate.value2) + (coordinate.value3 * coordinate.value3);
            double minRadiusSquared = 6370000d * 6370000d;
            if (radiusSquared < minRadiusSquared)
            {
                return false;
            }
            double maxRadiusSquared = 6500000d * 6500000d;
            if (radiusSquared > maxRadiusSquared)
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
            Vector3WGS result = new Vector3WGS(wgs.value2, wgs.value1, 0);
            return result;
        }

        public override Vector3WGS LocalUpDirection(Coordinate coordinate)
        {
            return GlobalUpDirection(coordinate);
        }

        public override int NorthingIndex()
        {
            return 1;
        }
        public override int EastingIndex()
        {
            return 0;
        }

        public override Vector3WGS Orientation()
        {
            return new Vector3WGS(0, 90, 0);
        }

        public override int AxisCount()
        {
            return 3;
        }
    }
}

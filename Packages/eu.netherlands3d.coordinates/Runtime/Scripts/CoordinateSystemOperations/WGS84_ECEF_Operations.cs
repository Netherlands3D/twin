using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Coordinates
{/// <summary>
/// Operations for WGS84ECEF
/// </summary>
    class WGS84_ECEF_Operations : CoordinateSystemOperation
    {
        public override string Code()
        {
            return "4978";
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
            return 3;
        }
        GeographicToGeocentricSettings conversionSettings = new GeographicToGeocentricSettings(0d, 6378137d, 298.2572236);

        public override Coordinate ConvertFromWGS84LatLonH(Coordinate coordinate)
        {
            Coordinate result = GeographicToGeocentric.Forward(coordinate, conversionSettings);
            Coordinate output = new Coordinate(CoordinateSystem.WGS84_ECEF, result.Points);
            output.extraLattitudeRotation = coordinate.extraLattitudeRotation;
            output.extraLongitudeRotation = coordinate.extraLongitudeRotation;
            return output;

        }

        public override Coordinate ConvertToWGS84LatLonH(Coordinate coordinate)
        {
            Coordinate result = GeographicToGeocentric.Reverse(coordinate, conversionSettings);
            Coordinate output = new Coordinate(CoordinateSystem.WGS84_LatLonHeight, result.Points);
            output.extraLattitudeRotation = coordinate.extraLattitudeRotation;
            output.extraLongitudeRotation = coordinate.extraLongitudeRotation;

            return new Coordinate(CoordinateSystem.WGS84_LatLonHeight, result.Points);
        }

        public override bool CoordinateIsValid(Coordinate coordinate)
        {
            if (coordinate.Points.Length!=3)
            {
                return false;
            }
            double radiusSquared = (coordinate.Points[0] * coordinate.Points[0]) + (coordinate.Points[1] * coordinate.Points[1]) + (coordinate.Points[2] * coordinate.Points[2]);
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
            return CoordinateSystemGroup.WGS84;
        }

        public override CoordinateSystemType GetCoordinateSystemType()
        {
            return CoordinateSystemType.Geocentric;
        }

        public override Vector3WGS GlobalUpDirection(Coordinate coordinate)
        {
            Coordinate latlon = ConvertToWGS84LatLonH(coordinate);
            return new Vector3WGS(latlon.Points[1], latlon.Points[0], 0);
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

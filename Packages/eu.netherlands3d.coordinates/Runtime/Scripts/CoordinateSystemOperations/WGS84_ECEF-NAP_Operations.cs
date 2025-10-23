namespace Netherlands3D.Coordinates
{
    /// <summary>
    /// Operations for WGS84_NAP_ECEF
    /// this coordinatesystem officially does not exist, but is still used by some of our users
    /// the coordinatesystem is created by using coordinates in epsg:9286 (ETRS89 + NAP height) and then using the geographic to geocentric transformation for WGS84
    /// 
    /// to convert this coordinatesystem to and from wgs84 we use the rdnap-WGS84-transformation to get the heightdifference between the wgs84 ellipsoid and the NAP-datum
    /// </summary>
    class WGS84_NAP_ECEF_Operations : CoordinateSystemOperation
    {
        public override string Code()
        {
            return "NAP_ECEF";
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
            Coordinate rdnap = new Coordinate(CoordinateSystem.WGS84_LatLonHeight, coordinate.northing, coordinate.easting, 0).Convert(CoordinateSystem.RDNAP);

            (double x, double y, double z) = GeographicToGeocentric.Forward(coordinate, conversionSettings);
            Coordinate output = new Coordinate(CoordinateSystem.WGS84_NAP_ECEF, x, y, z + rdnap.height);
            output.extraLattitudeRotation = coordinate.extraLattitudeRotation;
            output.extraLongitudeRotation = coordinate.extraLongitudeRotation;
            return output;
        }

        public override Coordinate ConvertToWGS84LatLonH(Coordinate coordinate)
        {
            (double x, double y, double z) = GeographicToGeocentric.Reverse(coordinate, conversionSettings);
            Coordinate rdnap = new Coordinate(CoordinateSystem.WGS84_LatLonHeight, x, y, 0).Convert(CoordinateSystem.RDNAP);
            Coordinate output = new Coordinate(CoordinateSystem.WGS84_LatLonHeight, x, y, z - rdnap.height);
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
            return CoordinateSystemGroup.WGS84;
        }

        public override CoordinateSystemType GetCoordinateSystemType()
        {
            return CoordinateSystemType.Geocentric;
        }

        public override Vector3WGS GlobalUpDirection(Coordinate coordinate)
        {
            Coordinate latlon = ConvertToWGS84LatLonH(coordinate);
            return new Vector3WGS(latlon.value2, latlon.value1, 0);
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
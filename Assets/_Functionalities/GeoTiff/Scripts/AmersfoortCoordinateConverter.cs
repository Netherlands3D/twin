using System;

namespace Netherlands3D.Functionalities.GeoTiff
{
    /// <summary>
    /// Converts EPSG:4289 (Amersfoort geographic latitude/longitude) directly to
    /// EPSG:28992 using the EPSG:9809 Oblique Stereographic projection.
    /// </summary>
    public static class AmersfoortCoordinateConverter
    {
        private const double SemiMajorAxis = 6377397.155;
        private const double InverseFlattening = 299.1528128;
        private const double LatitudeOfOriginDegrees = 52.1561605555556;
        private const double LongitudeOfOriginDegrees = 5.38763888888889;
        private const double ScaleFactor = 0.9999079;
        private const double FalseEasting = 155000.0;
        private const double FalseNorthing = 463000.0;
        private const double DegreesToRadians = Math.PI / 180.0;

        public static void ToRd(double longitudeDegrees, double latitudeDegrees, out double easting,
            out double northing)
        {
            if (latitudeDegrees < -90.0 || latitudeDegrees > 90.0 ||
                longitudeDegrees < -180.0 || longitudeDegrees > 180.0)
                throw new ArgumentOutOfRangeException(nameof(latitudeDegrees),
                    "EPSG:4289 coordinates must contain longitude and latitude in degrees.");

            var latitude = latitudeDegrees * DegreesToRadians;
            var longitude = longitudeDegrees * DegreesToRadians;
            var latitudeOfOrigin = LatitudeOfOriginDegrees * DegreesToRadians;
            var longitudeOfOrigin = LongitudeOfOriginDegrees * DegreesToRadians;

            var flattening = 1.0 / InverseFlattening;
            var eccentricitySquared = 2.0 * flattening - flattening * flattening;
            var eccentricity = Math.Sqrt(eccentricitySquared);
            var sinOrigin = Math.Sin(latitudeOfOrigin);
            var cosOrigin = Math.Cos(latitudeOfOrigin);
            var originDenominator = 1.0 - eccentricitySquared * sinOrigin * sinOrigin;
            var meridionalRadius = SemiMajorAxis * (1.0 - eccentricitySquared) /
                                   Math.Pow(originDenominator, 1.5);
            var primeVerticalRadius = SemiMajorAxis / Math.Sqrt(originDenominator);
            var sphereRadius = Math.Sqrt(meridionalRadius * primeVerticalRadius);
            var n = Math.Sqrt(1.0 + eccentricitySquared * Math.Pow(cosOrigin, 4.0) /
                (1.0 - eccentricitySquared));

            var s1 = (1.0 + sinOrigin) / (1.0 - sinOrigin);
            var s2 = (1.0 - eccentricity * sinOrigin) / (1.0 + eccentricity * sinOrigin);
            var w1 = Math.Pow(s1 * Math.Pow(s2, eccentricity), n);
            var sinChiDoubleZero = (w1 - 1.0) / (w1 + 1.0);
            var c = (n + sinOrigin) * (1.0 - sinChiDoubleZero) /
                    ((n - sinOrigin) * (1.0 + sinChiDoubleZero));
            var w2 = c * w1;
            var chiOrigin = Math.Asin((w2 - 1.0) / (w2 + 1.0));

            var sinLatitude = Math.Sin(latitude);
            var sa = (1.0 + sinLatitude) / (1.0 - sinLatitude);
            var sb = (1.0 - eccentricity * sinLatitude) / (1.0 + eccentricity * sinLatitude);
            var w = c * Math.Pow(sa * Math.Pow(sb, eccentricity), n);
            var chi = Math.Asin((w - 1.0) / (w + 1.0));
            var deltaLambda = n * (longitude - longitudeOfOrigin);
            var denominator = 1.0 + Math.Sin(chi) * Math.Sin(chiOrigin) +
                              Math.Cos(chi) * Math.Cos(chiOrigin) * Math.Cos(deltaLambda);
            var projectionScale = 2.0 * sphereRadius * ScaleFactor / denominator;

            easting = FalseEasting + projectionScale * Math.Cos(chi) * Math.Sin(deltaLambda);
            northing = FalseNorthing + projectionScale *
                (Math.Sin(chi) * Math.Cos(chiOrigin) -
                 Math.Cos(chi) * Math.Sin(chiOrigin) * Math.Cos(deltaLambda));
        }
    }
}

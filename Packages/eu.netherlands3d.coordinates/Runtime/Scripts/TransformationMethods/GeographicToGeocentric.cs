
using System;


namespace Netherlands3D.Coordinates
{
    internal static class GeographicToGeocentric
    {

        internal static (double x, double y, double z) Forward(Coordinate coordinate, GeographicToGeocentricSettings settings)
        {
            double lattitude = coordinate.value1 * Math.PI / 180.0d;
            double longitude = (coordinate.value2 + settings.primeMeridian) * Math.PI / 180.0d;
            double ellipisoidalHeight = coordinate.value3;
            //EPSG datset coordinate operation method code 9602)
            double primeVerticalRadius = settings.semiMajorAxis / (Math.Sqrt(1d - (Math.Pow(settings.eccentricity, 2) * Math.Pow(Math.Sin(lattitude), 2))));
            double X = (primeVerticalRadius + ellipisoidalHeight) * Math.Cos(lattitude) * Math.Cos(longitude);
            double Y = (primeVerticalRadius + ellipisoidalHeight) * Math.Cos(lattitude) * Math.Sin(longitude);
            double Z = ((1d - Math.Pow(settings.eccentricity, 2)) * primeVerticalRadius + ellipisoidalHeight) * Math.Sin(lattitude);
            return (X, Y, Z); //temporary unity coordinate container

        }
        internal static (double x, double y, double z) Reverse(Coordinate coordinate, GeographicToGeocentricSettings settings)
        {
            double X = coordinate.value1;
            double Y = coordinate.value2;
            double Z = coordinate.value3;

            double p = Math.Sqrt(Math.Pow(X, 2) + Math.Pow(Y, 2));
            double q = Math.Atan2((Z * settings.semiMajorAxis), p * settings.b);

            double lattitude = Math.Atan2((Z + settings.eta * settings.b * Math.Pow(Math.Sin(q), 3)), p - Math.Pow(settings.eccentricity, 2) * settings.semiMajorAxis * Math.Pow(Math.Cos(q), 3));

            double primeVerticalRadius = settings.semiMajorAxis / (Math.Sqrt(1 - (Math.Pow(settings.eccentricity, 2) * Math.Pow(Math.Sin(lattitude), 2))));
            double iteratedLattitude = Math.Atan2(Z + Math.Pow(settings.eccentricity, 2) * primeVerticalRadius * Math.Sin(lattitude), Math.Sqrt(Math.Pow(X, 2) + Math.Pow(Y, 2)));
            double DeltaLat = Math.Abs(iteratedLattitude - lattitude);
            lattitude = iteratedLattitude;
            for (int i = 0; i < 10; i++)
            {
                if (DeltaLat < Math.Pow(10, -6) * 180 / Math.PI)
                {
                    i = 11;
                    continue;
                }
                primeVerticalRadius = settings.semiMajorAxis / (Math.Sqrt(1 - (Math.Pow(settings.eccentricity, 2) * Math.Pow(Math.Sin(lattitude), 2))));
                iteratedLattitude = Math.Atan2(Z + Math.Pow(settings.eccentricity, 2) * primeVerticalRadius * Math.Sin(lattitude), Math.Sqrt(Math.Pow(X, 2) + Math.Pow(Y, 2)));

            }
            lattitude = iteratedLattitude;
            double longitude = Math.Atan2(Y, X);
            double height = (p / Math.Cos(lattitude)) - primeVerticalRadius;
            return (lattitude * 180 / Math.PI, (longitude * 180 / Math.PI) - settings.primeMeridian, height);//temporary unity coordinate container

        }
    }
}

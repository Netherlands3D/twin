using System;
using System.Globalization;

namespace Netherlands3D.LayerStyles.Expressions.Operations
{
    public static class DistanceOperation
    {
        public const string Code = "distance";

        public static double Evaluate(Expression expr, ExpressionContext ctx)
        {
            // Expect two [lon, lat] arrays
            var a = ExpressionEvaluator.Evaluate(expr, 0, ctx) as object[];
            var b = ExpressionEvaluator.Evaluate(expr, 1, ctx) as object[];
            if (a == null || b == null || a.Length < 2 || b.Length < 2)
                throw new InvalidOperationException($"\"distance\" requires two coordinate arrays [lon,lat].");

            double lon1 = Convert.ToDouble(a[0], CultureInfo.InvariantCulture);
            double lat1 = Convert.ToDouble(a[1], CultureInfo.InvariantCulture);
            double lon2 = Convert.ToDouble(b[0], CultureInfo.InvariantCulture);
            double lat2 = Convert.ToDouble(b[1], CultureInfo.InvariantCulture);

            double ToRad(double deg) => deg * Math.PI / 180.0;
            double dLat = ToRad(lat2 - lat1);
            double dLon = ToRad(lon2 - lon1);
            double rLat1 = ToRad(lat1);
            double rLat2 = ToRad(lat2);

            double sinDlat = Math.Sin(dLat / 2);
            double sinDlon = Math.Sin(dLon / 2);
            double aVal = sinDlat * sinDlat
                          + Math.Cos(rLat1) * Math.Cos(rLat2) * sinDlon * sinDlon;
            double c = 2 * Math.Atan2(Math.Sqrt(aVal), Math.Sqrt(1 - aVal));

            const double earthRadius = 6371000; // meters
            return earthRadius * c;
        }
    }
}
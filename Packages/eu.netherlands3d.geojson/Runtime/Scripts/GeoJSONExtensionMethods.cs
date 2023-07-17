using System;
using System.Linq;
using GeoJSON.Net;
using GeoJSON.Net.CoordinateReferenceSystem;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;

namespace Netherlands.GeoJSON
{
    public static class GeoJSONExtensionMethods
    {
        public static int EPSGId(this GeoJSONObject geoJsonObject)
        {
            // Default EPSG code for GeoJSON
            int epsgId = 4326;

            NamedCRS crsObject = geoJsonObject.CRS as NamedCRS;
            if (crsObject == null) return epsgId;
    
            crsObject.Properties.TryGetValue("name", out var crsName);

            if (crsName is not string crsNameString || !crsNameString.StartsWith("urn:ogc:def:crs:EPSG")) return epsgId;
            
            int.TryParse(crsNameString.Split(':').Last(), out epsgId);

            return epsgId;
        }
        
        public static double[] DerivedBoundingBoxes(this FeatureCollection featureCollection)
        {
            // We use max values as initial values so that we _know_ for sure that the first encountered position
            // will initialize the array. Using 0 as initial value can cause issues
            double[] boundingBox = { double.MaxValue, double.MaxValue, double.MinValue, Double.MinValue };

            foreach (Feature feature in featureCollection.Features)
            {
                MultiPolygon geometry = feature.Geometry as MultiPolygon;
                if (geometry != null)
                {
                    foreach (var poly in geometry.Coordinates)
                    {
                        AdjustBoundingBoxBasedOnPolygon(poly, boundingBox);
                    }

                    continue;
                }

                Polygon polygon = feature.Geometry as Polygon;
                if (polygon != null)
                {
                    AdjustBoundingBoxBasedOnPolygon(polygon, boundingBox);
                }
            }

            return boundingBox;
        }

        private static void AdjustBoundingBoxBasedOnPolygon(Polygon polygon, double[] boundingBox)
        {
            foreach (var lineString in polygon.Coordinates)
            {
                foreach (var position in lineString.Coordinates)
                {
                    if (position.Longitude < boundingBox[0]) boundingBox[0] = position.Longitude;
                    if (position.Longitude > boundingBox[2]) boundingBox[2] = position.Longitude;
                    if (position.Latitude < boundingBox[1]) boundingBox[1] = position.Latitude;
                    if (position.Latitude > boundingBox[3]) boundingBox[3] = position.Latitude;
                }
            }
        }
    }
}
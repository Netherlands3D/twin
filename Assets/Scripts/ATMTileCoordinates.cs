using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Coordinates;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class ATMTileCoordinates : MonoBehaviour
    {
        [SerializeField] private int[] z = new[] { 16, 19 };
        [SerializeField] private string url = @"https://images.huygens.knaw.nl/webmapper/maps/pw-1943/{z}/{x}/{y}.png";

        private const double RDOriginX = 155000; // Origin X for RD New

        private const double RDOriginY = 463000; // Origin Y for RD New

        private const double RDMinX = -285401.92;  // RD min X coordinate
        private const double RDMinY = 22598.08;    // RD min Y coordinate
        private const double RDMaxX = 595400.0;    // RD max X coordinate
        private const double RDMaxY = 903401.92;   // RD max Y coordinate

        private const double RDToWebMercatorScale = 1.9332276; // Accurate scaling factor for RD New to Web Mercator

        public static (int tileX, int tileY) RDToTileXY(double rdX, double rdY, int zoom)
        {
            // Step 1: Normalize RD coordinates to a 0-1 scale based on RD New bounds
            double normX = (rdX - RDMinX) / (RDMaxX - RDMinX);
            double normY = (RDMaxY - rdY) / (RDMaxY - RDMinY); // Y axis is inverted for tile coordinates

            // Step 2: Scale normalized coordinates to the appropriate tile coordinates
            int mapSize = 1 << zoom; // Total tiles in one dimension at this zoom level
            int tileX = (int)Math.Floor(normX * mapSize);
            int tileY = (int)Math.Floor(normY * mapSize);

            return (tileX, tileY);
        }

        // magic ChatGPT function 
        public static (int x, int y) LatLonToTileXY(double lat, double lon, int zoom)
        {
            // Calculate the x coordinate
            var dx = (lon + 180.0d) / 360.0d * (1 << zoom);
            int x = (int)Math.Floor(dx);

            // Calculate the y coordinate
            double latRad = lat * Math.PI / 180.0d;
            var dy = (1 - Math.Log(Math.Tan(latRad) + 1 / Math.Cos(latRad)) / Math.PI) / 2 * (1 << zoom);
            int y = (int)Math.Floor(dy);

            return (x, y);
        }

        public static (int x, int y) CoordinateToTileXY(Coordinate coord, int zoomLevel)
        {
            var latLonCoord = coord.Convert(CoordinateSystem.WGS84_LatLon);
            print("getting url: " + latLonCoord.northing + "\t" + latLonCoord.easting);
            return LatLonToTileXY(latLonCoord.northing, latLonCoord.easting, zoomLevel);
        }

        private string GetTileUrl(int z, int x, int y)
        {
            return url.Replace("{z}", z.ToString())
                .Replace("{x}", x.ToString())
                .Replace("{y}", y.ToString());
        }

        public Vector2Int xy;

        public string GetTileUrl(Coordinate coord, int zoomLevel)
        {
            var converted = CoordinateToTileXY(coord, zoomLevel);
            print("conv: " + converted.x + ", " + converted.y);
            xy = new Vector2Int(converted.x, converted.y);

            var a = CoordinateToTileXY(new Coordinate(CoordinateSystem.RD, 121687, 487326), 16);
            var b = RDToTileXY(121687, 487326, 16);
            Debug.LogError(a + "-----" + b);

            return GetTileUrl(zoomLevel, converted.x, converted.y);
        }

        public string GetTileURL(Vector2Int tileKey, int zoomLevel)
        {
            var converted = RDToTileXY(tileKey.x, tileKey.y, zoomLevel);
            xy = new Vector2Int(converted.tileX, converted.tileY);
            return GetTileUrl(zoomLevel, converted.tileX, converted.tileY);
        }
    }
}
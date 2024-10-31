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
            print("dx" + dx + " dy:" + dy);
            print("x" + x + " y:" + y);
            
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

        public string GetTileUrl(Coordinate coord, int zoomLevel)
        {
            var converted = CoordinateToTileXY(coord, zoomLevel);
            print("conv: " + converted.x + ", " + converted.y);
            return GetTileUrl(zoomLevel, converted.x, converted.y);
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Coordinates;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class ATMTileCoordinates : MonoBehaviour
    {
        public int[] z = new[] { 16, 19 };
        public int x, y;
        public string url = @"https://images.huygens.knaw.nl/webmapper/maps/pw-1943/{z}/{x}/{y}.png";
        
        // magic ChatGPT function 
        public static (int x, int y) LatLonToTileXY(double lat, double lon, int zoom)
        {
            // Calculate the x coordinate
            int x = (int)Math.Floor((lon + 180.0) / 360.0 * (1 << zoom));
        
            // Calculate the y coordinate
            double latRad = lat * Math.PI / 180.0;
            int y = (int)Math.Floor((1 - Math.Log(Math.Tan(latRad) + 1 / Math.Cos(latRad)) / Math.PI) / 2 * (1 << zoom));

            return (x, y);
        }

        public static (int x, int y) CoordinateToTileXY(Coordinate coord, int zoomLevel)
        {
            var latLonCoord = coord.Convert(CoordinateSystem.WGS84_LatLon);
            return LatLonToTileXY(latLonCoord.northing, latLonCoord.easting, zoomLevel);
        }

        private string GetTileUrl(int z, int x, int y)
        {
            url = url.Replace("{z}", z.ToString())
                .Replace("{x}", x.ToString())
                .Replace("{y}", y.ToString());
            return url;
        }
        
        public string GetTileUrl(Coordinate coord, int zoomLevel)
        {
            var converted = CoordinateToTileXY(coord, zoomLevel);
            return GetTileUrl(zoomLevel, converted.x, converted.y);
        }
        
        private void Start()
        {
            var rdCoord = new Coordinate(CoordinateSystem.RD, x, y);
            var converted = CoordinateToTileXY(rdCoord, z[0]);
            
            print(converted.x + "\t" + converted.y);
        }
    }
}

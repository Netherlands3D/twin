using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Utility;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin.Samplers
{
    public class HeightMap : MonoBehaviour
    {
        [SerializeField] private Texture2D heightMap;

        private double minX = StandardBoundingBoxes.RD_NetherlandsBounds_Cropped.BottomLeft.easting;
        private double maxX = StandardBoundingBoxes.RD_NetherlandsBounds_Cropped.TopRight.easting;
        private double minY = StandardBoundingBoxes.RD_NetherlandsBounds_Cropped.BottomLeft.northing;
        private double maxY = StandardBoundingBoxes.RD_NetherlandsBounds_Cropped.TopRight.northing;

        private const float lowestPoint = -6.76f;
        private const float highestPoint = 322.4f;
        private const float lowestValue = 0.0f;
        private const float highestValue = 1.0f;

        private float totalHeight => highestPoint - lowestPoint;
        private float totalValue => highestValue - lowestValue;

        private void Start()
        {
        }

        public float GetHeight(Coordinate coordinate)
        {
            if (heightMap == null) return 0f;

            //Coordinate wgs84 = coordinate.Convert(CoordinateSystem.WGS84_LatLon);
            //double u = (wgs84.easting - MinLon) / (MaxLon - MinLon);
            //double v = (wgs84.northing - MinLat) / (MaxLat - MinLat);
            //int x = (int)(Mathf.Clamp01((float)u) * (heightMap.width - 1));
            //int y = (int)(Mathf.Clamp01((float)v) * (heightMap.height - 1));
            //Color pixel = heightMap.GetPixel(x, y);

            
            Coordinate rd = coordinate.Convert(CoordinateSystem.RD);

            double u = (rd.easting - minX) / (maxX - minX); // horizontal
            double v = (rd.northing - minY) / (maxY - minY); // vertical
            int px = Mathf.RoundToInt((float)u * (heightMap.width - 1));
            int py = Mathf.RoundToInt((float)v * (heightMap.height - 1));
            Color pixel = heightMap.GetPixel(px, py);
            float h = pixel.r;
            //Debug.Log(h);
            float floor = h - lowestValue;
            h = (floor / totalValue) * totalHeight + lowestPoint;

            return h;
        }
    }
}

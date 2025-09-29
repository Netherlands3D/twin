using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Utility;
using UnityEngine;

namespace Netherlands3D.Twin.Samplers
{
    public class HeightMap : MonoBehaviour
    {
        [SerializeField] private Texture2D heightMap;

        private double MinLat = 50.75035;     // south
        private double MaxLat = 53.5171625;   // north
        private double MinLon = 3.35833;      // west
        private double MaxLon = 7.22778;      // east

        private const float lowestPoint = -6.76f;
        private const float highestPoint = 322.4f;

        private Vector2 pixelBounds;

        private void Start()
        {
            MinLat = StandardBoundingBoxes.Wgs84LatLon_NetherlandsBounds_Cropped.BottomLeft.northing;
            MaxLat = StandardBoundingBoxes.Wgs84LatLon_NetherlandsBounds_Cropped.TopRight.northing;
            MinLon = StandardBoundingBoxes.Wgs84LatLon_NetherlandsBounds_Cropped.BottomLeft.easting;
            MaxLon = StandardBoundingBoxes.Wgs84LatLon_NetherlandsBounds_Cropped.TopRight.easting;

            FindPixelBounds();

        }

        private void FindPixelBounds()
        {
            Color[] pixels = heightMap.GetPixels();
            float minValue = float.MaxValue;
            float maxValue = float.MinValue;
            Vector2Int minPos = Vector2Int.zero;
            Vector2Int maxPos = Vector2Int.zero;
            int width = heightMap.width;

            for(int x = 0; x < heightMap.width; x++)
                for(int y = 0; y < heightMap.height; y++)



            for (int i = 0; i < pixels.Length; i++)
            {
                float v = pixels[i].r;

                // Skip black (no-data)
                if (v < 0.1f) continue;

                if (v < minValue)
                    minValue = v;
                if (v > maxValue)
                    maxValue = v;
            }
            pixelBounds = new Vector2(minValue, maxValue);
        }

        public float GetHeight(Coordinate coordinate)
        {
            if (heightMap == null) return 0f;

            Coordinate wgs84 = coordinate.Convert(CoordinateSystem.WGS84_LatLon);
            double u = (wgs84.easting - MinLon) / (MaxLon - MinLon);
            double v = (wgs84.northing - MinLat) / (MaxLat - MinLat);
            int x = (int)(Mathf.Clamp01((float)u) * heightMap.width);
            int y = (int)(Mathf.Clamp01((float)v) * heightMap.height);
            Color pixel = heightMap.GetPixel(x,y);
            float h = pixel.r;

            // Optionally scale to meters (depends on how you normalized your texture!)
            // Example: if stored as 0–255 → 0–255 m, or you rescaled to AHN range
            return h;
        }
    }
}

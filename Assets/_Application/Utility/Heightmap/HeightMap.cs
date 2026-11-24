using Netherlands3D.Coordinates;
using UnityEngine;

namespace Netherlands3D.Twin.HeightMap
{
    public class HeightMap : MonoBehaviour
    {
        [SerializeField] private Texture2D heightMap;
        
        private Coordinate bottomLeft = new Coordinate(CoordinateSystem.WGS84_LatLon, 50.75035d, 3.35833d);
        private Coordinate topRight = new Coordinate(CoordinateSystem.WGS84_LatLon, 53.5171625d, 7.22778d);

        private const float lowestPoint = -6.76f;
        private const float highestPoint = 322.4f;
        private const float lowestValue = 0.0f;
        private const float highestValue = 1.0f;

        private float totalHeight => highestPoint - lowestPoint;
        private float totalValue => highestValue - lowestValue;

        public float GetHeight(Coordinate coordinate, bool bilinear = false)
        {
            if (heightMap == null)
            {
                Debug.LogError("missing heightmap texture");
                return 0f;
            }

            Coordinate rd = coordinate.Convert(CoordinateSystem.RD);
            double u = (rd.easting - bottomLeft.easting) / (topRight.easting - bottomLeft.easting);
            double v = (rd.northing - bottomLeft.northing) / (topRight.northing - bottomLeft.northing);
            
            Color pixel;
            if (bilinear)
                pixel = heightMap.GetPixelBilinear((float)u, (float)v);
            else
            {
                int px = Mathf.RoundToInt((float)u * (heightMap.width - 1));
                int py = Mathf.RoundToInt((float)v * (heightMap.height - 1));
                pixel = heightMap.GetPixel(px, py);
            }
            float h = pixel.r;
            float floor = h - lowestValue;
            h = (floor / totalValue) * totalHeight + lowestPoint;
            return h;
        }
    }
}

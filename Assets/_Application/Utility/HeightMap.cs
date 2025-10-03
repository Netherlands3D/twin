using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Utility;
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

        public float GetHeight(Coordinate coordinate, bool bilinear = false)
        {
            if (heightMap == null)
            {
                Debug.LogError("missing heightmap texture");
                return 0f;
            }

            Coordinate rd = coordinate.Convert(CoordinateSystem.RD);
            double u = (rd.easting - minX) / (maxX - minX);
            double v = (rd.northing - minY) / (maxY - minY);
            
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

using Netherlands3D.Coordinates;
using UnityEngine;

namespace Netherlands3D.Tilekit
{
    public class TileMath
    {

        public static Bounds GetUnityBounds(Tile tile)
        {
            const int height = 20;
            
            var rdCenterX = tile.RdX + tile.Size * 0.5;
            var rdCenterY = tile.RdY + tile.Size * 0.5;
            
            var rdCenter = new Coordinate(CoordinateSystem.RD, rdCenterX, rdCenterY);
            
            var unityCenter = rdCenter.ToUnity();
            unityCenter.y = height * 0.5f;
            return new Bounds(unityCenter, new Vector3(tile.Size, height, tile.Size));
        }

        public static Rect GetBoundsViewportRect(
            Camera camera,
            Bounds bounds)
        {
            var viewportMin = new Vector2(
                float.PositiveInfinity,
                float.PositiveInfinity
            );

            var viewportMax = new Vector2(
                float.NegativeInfinity,
                float.NegativeInfinity
            );

            var center = bounds.center;
            var extents = bounds.extents;

            for (var x = -1; x <= 1; x += 2)
            {
                for (var y = -1; y <= 1; y += 2)
                {
                    for (var z = -1; z <= 1; z += 2)
                    {
                        var worldPoint = center + new Vector3(
                            extents.x * x,
                            extents.y * y,
                            extents.z * z
                        );

                        var viewportPoint =
                            camera.WorldToViewportPoint(worldPoint);

                        // The bounds crosses or is behind the camera.
                        // For LOD selection, treating this as very large is safe.
                        if (viewportPoint.z <= 0f)
                        {
                            return new Rect(
                                0f,
                                0f,
                                float.PositiveInfinity,
                                float.PositiveInfinity
                            );
                        }

                        var point = new Vector2(
                            viewportPoint.x,
                            viewportPoint.y
                        );

                        viewportMin = Vector2.Min(viewportMin, point);
                        viewportMax = Vector2.Max(viewportMax, point);
                    }
                }
            }

            return Rect.MinMaxRect(
                viewportMin.x,
                viewportMin.y,
                viewportMax.x,
                viewportMax.y
            );
        }

        

        public static Tile GetParent(Tile tile)
        {
            var parentSize = tile.Size * 2;
            
             var parentRdX = Mathf.FloorToInt((float)tile.RdX / parentSize) * parentSize;
             var parentRdY = Mathf.FloorToInt((float)tile.RdY / parentSize) * parentSize;
             return new Tile(parentRdX, parentRdY, parentSize);
        }
        
        public static Tile GetChild(Tile parent, int index)
        {
            var childSize = parent.Size / 2;

            return new Tile(
                parent.RdX + index % 2 * childSize,
                parent.RdY + index / 2 * childSize,
                childSize
            );
        }
        

    }
}

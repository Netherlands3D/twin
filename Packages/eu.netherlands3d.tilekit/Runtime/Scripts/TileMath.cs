using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.Rendering;

namespace Netherlands3D.Tilekit
{
    public class TileMath
    {
        private static readonly Vector3[] nearFrustumCorners = new Vector3[4];
        private static readonly Vector3[] farFrustumCorners = new Vector3[4];
        private static readonly List<Vector3> visibleGroundPolygon = new(6);
        
        private static Vector2 topLeft = new Vector2(0, 1);
        private static Vector2 topRight = new Vector2(1, 1);
        private static Vector2 bottomRight = new Vector2(1, 0);
        private static Vector2 bottomLeft = new Vector2(0, 0);

        public static void GetVisibleGroundPolygonNonAlloc_1(Camera camera, float maximumViewDistance, List<Vector3> result)
        {
            result.Clear();
            result.Add(GetCornerPoint_1(camera, topLeft, maximumViewDistance));
            result.Add(GetCornerPoint_1(camera, topRight, maximumViewDistance));
            result.Add(GetCornerPoint_1(camera, bottomRight, maximumViewDistance));
            result.Add(GetCornerPoint_1(camera, bottomLeft, maximumViewDistance));
        }

        public static Vector3 GetCornerPoint_1(Camera camera, Vector2 screenPosition, float maximumViewDistance)
        {
            var result = new Vector3();
            
            var topScreenPointFar = camera.ViewportToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 10000));
            var topScreenPointNear = camera.ViewportToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 10));
            // Calculate direction vector
            Vector3 direction = topScreenPointNear - topScreenPointFar;
            float factor; //factor waarmee de Richtingvector vermenigvuldigd moet worden om op het maaiveld te stoppen
            if (direction.y < 0) //wanneer de Richtingvector omhooggaat deze factor op 1 instellen
            {
                factor = 1;
            }
            else
            {
                factor = ((topScreenPointNear.y) / direction.y); //factor bepalen t.o.v. maaiveld (aanname maaiveld op 0 NAP = ca 40 Unityeenheden in Y-richting)
            }

            // Determine the X, Y, en Z location where the viewline ends
            result.x = topScreenPointNear.x - Mathf.Clamp((factor * direction.x), -1 * maximumViewDistance, maximumViewDistance);
            result.y = topScreenPointNear.y - Mathf.Clamp((factor * direction.y), -1 * maximumViewDistance, maximumViewDistance);
            result.z = topScreenPointNear.z - Mathf.Clamp((factor * direction.z), -1 * maximumViewDistance, maximumViewDistance);

            return result;
        }
        
        public static bool GetVisibleGroundPolygonNonAlloc_2(Camera camera, float maximumViewDistance, List<Vector3> result)
        {
            result.Clear();
            result.Add(GetCornerPoint_2(camera, topLeft, maximumViewDistance));
            result.Add(GetCornerPoint_2(camera, topRight, maximumViewDistance));
            result.Add(GetCornerPoint_2(camera, bottomRight, maximumViewDistance));
            result.Add(GetCornerPoint_2(camera, bottomLeft, maximumViewDistance));
            return true;
        }
        
        private static readonly Plane[] frustumPlanes = new Plane[6];

        public static void GetVisibleTilesNonAlloc(Camera camera, float maximumViewDistance, int tileSize, List<Tile> result)
        {
            result.Clear();

            GeometryUtility.CalculateFrustumPlanes(camera, frustumPlanes);

            var cameraPosition = camera.transform.position;

            var minimumTileX = Mathf.FloorToInt((cameraPosition.x - maximumViewDistance) / tileSize);
            var maximumTileX = Mathf.FloorToInt((cameraPosition.x + maximumViewDistance) / tileSize);
            var minimumTileY = Mathf.FloorToInt((cameraPosition.z - maximumViewDistance) / tileSize);
            var maximumTileY = Mathf.FloorToInt((cameraPosition.z + maximumViewDistance) / tileSize);
            
            var maximumViewDistanceSquared = maximumViewDistance * maximumViewDistance;

            for (var tileY = minimumTileY; tileY <= maximumTileY; tileY++)
            {
                for (var tileX = minimumTileX; tileX <= maximumTileX; tileX++)
                {
                    var tileCoordinate = new Vector2Int(tileX, tileY);
                    var tile = new Tile(tileCoordinate, tileSize);
                    var tileBounds = GetBounds(tile);
                    
                    var nearestX = Mathf.Clamp(cameraPosition.x, tileBounds.min.x, tileBounds.max.x);
                    var nearestZ = Mathf.Clamp(cameraPosition.z, tileBounds.min.z, tileBounds.max.z);

                    var deltaX = cameraPosition.x - nearestX;
                    var deltaZ = cameraPosition.z - nearestZ;

                    if (deltaX * deltaX + deltaZ * deltaZ > maximumViewDistanceSquared)
                    {
                        continue;
                    }
                    
                    if (!GeometryUtility.TestPlanesAABB(frustumPlanes, tileBounds))
                    {
                        continue;
                    }

                    result.Add(tile);
                }
            }
        }
        
        public static Bounds GetBounds(Tile tile)
        {
            return new Bounds(
                new Vector3(tile.Coordinate.x + tile.Size * 0.5f, 0f, tile.Coordinate.y + tile.Size * 0.5f),
                new Vector3(tile.Size, 200f, tile.Size)
            );
        }

        public static Rect GetBoundsScreenSize(Camera camera, Bounds bounds)
        {
            var screenMin = camera.WorldToViewportPoint(bounds.min);
            var screenMax = camera.WorldToViewportPoint(bounds.max);

            return new Rect(screenMin, screenMax - screenMin);
        }


        public static Vector3 GetCornerPoint_2(Camera camera, Vector2 screenPosition, float maximumViewDistance)
        {
            /*
            var ray = camera.ViewportPointToRay(screenPosition);
            var floorPlane = new Plane(Vector3.up, Vector3.zero);
            if (floorPlane.Raycast(ray, out var floorPlaneDistance))
            {
                if (floorPlaneDistance <= maximumViewDistance)
                {
                    return ray.GetPoint(floorPlaneDistance);
                }
            }
            
            var forwardOnFloor = Vector3.ProjectOnPlane(
                camera.transform.forward,
                Vector3.up
            );

            
            var directionOnFloor = Vector3.ProjectOnPlane(
                ray.direction,
                Vector3.up
            );
            var limitDirection =
                Vector3.Dot(directionOnFloor, forwardOnFloor) >= 0f
                    ? forwardOnFloor
                    : -forwardOnFloor;
            
            var cameraOnFloor = new Vector3(
                camera.transform.position.x,
                0f,
                camera.transform.position.z
            );
            
            var farPlanePosition =
                cameraOnFloor + limitDirection * maximumViewDistance;
            var farPlane = new Plane(-limitDirection, farPlanePosition);
            var rayOnFloor = new Ray(cameraOnFloor, directionOnFloor);
            if (farPlane.Raycast(rayOnFloor, out var farPlaneDistance))
            {
                return rayOnFloor.GetPoint(farPlaneDistance);
            }

            // Degenerate fallback.
            return cameraOnFloor +
                   directionOnFloor * maximumViewDistance;
            
            
             */

            var viewportPosition = screenPosition;
            var floorHeight = 0f;
            
            var ray = camera.ViewportPointToRay(viewportPosition);

            var floorPlane = new Plane(
                Vector3.up,
                new Vector3(0f, floorHeight, 0f)
            );

            var cameraOnFloor = new Vector3(
                camera.transform.position.x,
                floorHeight,
                camera.transform.position.z
            );

            var forwardOnFloor = Vector3.ProjectOnPlane(
                camera.transform.forward,
                Vector3.up
            ).normalized;

            // Use the real floor intersection when it is within range.
            if (floorPlane.Raycast(ray, out var floorDistance))
            {
                var floorPoint = ray.GetPoint(floorDistance);

                var forwardDistance = Vector3.Dot(
                    floorPoint - cameraOnFloor,
                    forwardOnFloor
                );

                if (forwardDistance <= maximumViewDistance)
                    return floorPoint;
            }

            // A vertical limit plane facing the camera.
            var maximumDistancePlane = new Plane(
                -forwardOnFloor,
                cameraOnFloor + forwardOnFloor * maximumViewDistance
            );

            // Lay the corner ray's direction onto the floor.
            var directionOnFloor = Vector3.ProjectOnPlane(
                ray.direction,
                Vector3.up
            ).normalized;

            var rayOnFloor = new Ray(cameraOnFloor, directionOnFloor);

            maximumDistancePlane.Raycast(rayOnFloor, out var distance);
            return rayOnFloor.GetPoint(distance);
        }
        
        public static bool GetVisibleGroundPolygon(
            Camera camera,
            float terrainHeight,
            float maximumViewDepth,
            List<Vector3> result)
        {
            result.Clear();

            var nearDepth = camera.nearClipPlane;
            var farDepth = Mathf.Min(maximumViewDepth, camera.farClipPlane);

            if (farDepth <= nearDepth)
                return false;

            var viewport = new Rect(0f, 0f, 1f, 1f);

            camera.CalculateFrustumCorners(
                viewport,
                nearDepth,
                Camera.MonoOrStereoscopicEye.Mono,
                nearFrustumCorners);

            camera.CalculateFrustumCorners(
                viewport,
                farDepth,
                Camera.MonoOrStereoscopicEye.Mono,
                farFrustumCorners);

            // Convert the camera-relative corners to world coordinates.
            for (var i = 0; i < 4; i++)
            {
                nearFrustumCorners[i] =
                    camera.transform.TransformPoint(nearFrustumCorners[i]);

                farFrustumCorners[i] =
                    camera.transform.TransformPoint(farFrustumCorners[i]);
            }

            // Intersect all twelve frustum edges with the ground.
            for (var i = 0; i < 4; i++)
            {
                var next = (i + 1) % 4;

                // Near-plane edges.
                AddGroundIntersection(
                    nearFrustumCorners[i],
                    nearFrustumCorners[next],
                    terrainHeight,
                    result);

                // Far-plane edges.
                AddGroundIntersection(
                    farFrustumCorners[i],
                    farFrustumCorners[next],
                    terrainHeight,
                    result);

                // Edges connecting the near and far planes.
                AddGroundIntersection(
                    nearFrustumCorners[i],
                    farFrustumCorners[i],
                    terrainHeight,
                    result);
            }

            if (result.Count < 3)
                return false;

            SortAroundCentre(result);
            return true;
        }
        
        private static void AddGroundIntersection(
            Vector3 start,
            Vector3 end,
            float terrainHeight,
            List<Vector3> result)
        {
            const float epsilon = 0.0001f;

            var startDistance = start.y - terrainHeight;
            var endDistance = end.y - terrainHeight;

            var startOnPlane = Mathf.Abs(startDistance) <= epsilon;
            var endOnPlane = Mathf.Abs(endDistance) <= epsilon;

            if (startOnPlane)
                AddUnique(result, start);

            if (endOnPlane)
                AddUnique(result, end);

            if (startOnPlane || endOnPlane)
                return;

            // Both points are on the same side of the ground.
            if ((startDistance < 0f) == (endDistance < 0f))
                return;

            var t = startDistance / (startDistance - endDistance);
            var intersection = Vector3.LerpUnclamped(start, end, t);
            intersection.y = terrainHeight;

            AddUnique(result, intersection);
        }

        private static void AddUnique(List<Vector3> points, Vector3 point)
        {
            const float epsilonSquared = 0.0001f * 0.0001f;

            for (var i = 0; i < points.Count; i++)
            {
                if ((points[i] - point).sqrMagnitude <= epsilonSquared)
                    return;
            }

            points.Add(point);
        }
        
        private static void SortAroundCentre(List<Vector3> points)
        {
            var centre = Vector3.zero;

            for (var i = 0; i < points.Count; i++)
                centre += points[i];

            centre /= points.Count;

            // Allocation-free insertion sort.
            for (var i = 1; i < points.Count; i++)
            {
                var point = points[i];
                var angle = GetAngle(point, centre);
                var j = i - 1;

                while (j >= 0 && GetAngle(points[j], centre) > angle)
                {
                    points[j + 1] = points[j];
                    j--;
                }

                points[j + 1] = point;
            }
        }
        
        private static float GetAngle(Vector3 point, Vector3 centre)
        {
            return Mathf.Atan2(
                point.z - centre.z,
                point.x - centre.x);
        }
        
        public static void FindVisibleTiles(
            List<Vector3> groundPolygon,
            float tileSize,
            Vector2 gridOrigin,
            List<Vector2Int> result)
        {
            result.Clear();

            if (groundPolygon.Count < 3)
                return;

            var minX = groundPolygon[0].x;
            var maxX = groundPolygon[0].x;
            var minZ = groundPolygon[0].z;
            var maxZ = groundPolygon[0].z;

            for (var i = 1; i < groundPolygon.Count; i++)
            {
                var point = groundPolygon[i];

                minX = Mathf.Min(minX, point.x);
                maxX = Mathf.Max(maxX, point.x);
                minZ = Mathf.Min(minZ, point.z);
                maxZ = Mathf.Max(maxZ, point.z);
            }

            var minTileX = Mathf.FloorToInt((minX - gridOrigin.x) / tileSize);
            var maxTileX = Mathf.CeilToInt((maxX - gridOrigin.x) / tileSize) - 1;
            var minTileY = Mathf.FloorToInt((minZ - gridOrigin.y) / tileSize);
            var maxTileY = Mathf.CeilToInt((maxZ - gridOrigin.y) / tileSize) - 1;

            for (var tileY = minTileY; tileY <= maxTileY; tileY++)
            {
                for (var tileX = minTileX; tileX <= maxTileX; tileX++)
                {
                    var tileRectangle = new Rect(
                        gridOrigin.x + tileX * tileSize,
                        gridOrigin.y + tileY * tileSize,
                        tileSize,
                        tileSize);

                    if (PolygonOverlapsRectangle(groundPolygon, tileRectangle))
                        result.Add(new Vector2Int(tileX, tileY));
                }
            }
        }
        
        private static bool PolygonOverlapsRectangle(
            List<Vector3> polygon,
            Rect rectangle)
        {
            // The rectangle's own separating axes.
            if (!OverlapsOnAxis(polygon, rectangle, Vector2.right))
                return false;

            if (!OverlapsOnAxis(polygon, rectangle, Vector2.up))
                return false;

            // The polygon's separating axes.
            for (var i = 0; i < polygon.Count; i++)
            {
                var current = polygon[i];
                var next = polygon[(i + 1) % polygon.Count];

                var edge = new Vector2(
                    next.x - current.x,
                    next.z - current.z);

                if (edge.sqrMagnitude <= Mathf.Epsilon)
                    continue;

                var perpendicularAxis = new Vector2(-edge.y, edge.x);

                if (!OverlapsOnAxis(polygon, rectangle, perpendicularAxis))
                    return false;
            }

            return true;
        }
        
        private static bool OverlapsOnAxis(
            List<Vector3> polygon,
            Rect rectangle,
            Vector2 axis)
        {
            var firstPoint = polygon[0];

            var polygonMin =
                firstPoint.x * axis.x +
                firstPoint.z * axis.y;

            var polygonMax = polygonMin;

            for (var i = 1; i < polygon.Count; i++)
            {
                var point = polygon[i];

                var projection =
                    point.x * axis.x +
                    point.z * axis.y;

                polygonMin = Mathf.Min(polygonMin, projection);
                polygonMax = Mathf.Max(polygonMax, projection);
            }

            var rectangleCentre = rectangle.center;
            var rectangleExtents = rectangle.size * 0.5f;

            var rectangleCentreProjection =
                rectangleCentre.x * axis.x +
                rectangleCentre.y * axis.y;

            var rectangleProjectionRadius =
                rectangleExtents.x * Mathf.Abs(axis.x) +
                rectangleExtents.y * Mathf.Abs(axis.y);

            var rectangleMin =
                rectangleCentreProjection - rectangleProjectionRadius;

            var rectangleMax =
                rectangleCentreProjection + rectangleProjectionRadius;

            return polygonMax >= rectangleMin &&
                   rectangleMax >= polygonMin;
        }

    }
}

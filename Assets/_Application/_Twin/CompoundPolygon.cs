using System;
using System.Collections.Generic;
using Netherlands3D.SelectionTools;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class CompoundPolygon
    {
        public List<Vector2[]> Paths = new List<Vector2[]>();
        public Vector2[] SolidPolygon => Paths[0];
        public List<Vector2[]> Holes => Paths.Count > 1 ? Paths.GetRange(1, Paths.Count - 1) : new List<Vector2[]>();
        public Bounds Bounds { get; private set; }

        public CompoundPolygon()
        {
            Paths = new List<Vector2[]>();
            Bounds = new Bounds();
        }

        public CompoundPolygon(CompoundPolygon original)
        {
            Paths = new List<Vector2[]>(original.Paths);
            RecalculateBounds();
        }

        public CompoundPolygon(List<Vector2[]> paths)
        {
            Paths = paths;
            RecalculateBounds();
        }

        private void RecalculateBounds()
        {
            Vector3[] solidPolygon3D = new Vector3[SolidPolygon.Length];
            for (int i = 0; i < SolidPolygon.Length; i++)
            {
                Vector2 p = SolidPolygon[i];
                solidPolygon3D[i] = new Vector3(p.x, 0, p.y);
            }

            Bounds = GeometryUtility.CalculateBounds(solidPolygon3D, Matrix4x4.identity);
        }

        public CompoundPolygon(Vector2[] solidPolygon)
        {
            SetSolidPolygon(solidPolygon);
        }

        public CompoundPolygon(Vector2[] solidPolygon, List<Vector2[]> holes)
        {
            SetSolidPolygon(solidPolygon);
            Paths.AddRange(holes);
        }

        public void SetSolidPolygon(Vector2[] polygon)
        {
            if (Paths.Count == 0)
                Paths.Add(polygon);
            else
                Paths[0] = polygon;

            RecalculateBounds();
        }

        public void AddHole(Vector2[] hole)
        {
            if (Paths.Count == 0)
                throw new Exception("Cannot add a hole to polygon if it has no solid part. Add a solid polygon first");

            foreach (var p in hole)
                if (!PolygonCalculator.ContainsPoint(SolidPolygon, p))
                    throw new Exception("Cannot add hole because it contains at least one point outside of the solid polygon: " + p);

            Paths.Add(hole);
        }

        public void RemoveHole(Vector2[] hole)
        {
            if (Paths.Count < 2)
                throw new Exception("Polygon has no holes to remove");

            Paths.Remove(hole);
        }


        public static Vector2[] GenerateGridPoints(Bounds bounds, float cellSize, float angle)
        {
            return GenerateGridPoints(bounds, cellSize, angle, out _);
        }

        public static Vector2[] GenerateGridPoints(Bounds bounds, float cellSize, float angle, out Bounds expandedBounds)
        {
            // Increase the bounds size to ensure coverage after rotation
            float diagonalLength = bounds.size.magnitude;
            float expandedBoundsSizeSide = diagonalLength;
            var expandedBoundsSize = new Vector3(expandedBoundsSizeSide + cellSize, bounds.size.y, expandedBoundsSizeSide + cellSize);
            expandedBounds = new Bounds(bounds.center, expandedBoundsSize);

            var offsetX = (expandedBoundsSizeSide % cellSize) / 2 + cellSize / 2; //offset half the remainder of the grid + half a cell size to center it
            var offsetY = (expandedBoundsSizeSide % cellSize) / 2 + cellSize / 2;

            Vector2 bottomLeft = new Vector2(expandedBounds.center.x, expandedBounds.center.z) - new Vector2(expandedBounds.extents.x, expandedBounds.extents.z) + new Vector2(offsetX, offsetY);

            Vector2 rotationCenter = new Vector2(expandedBounds.center.x, expandedBounds.center.z);

            // Calculate the number of iterations for x and y
            int numXIterations = Mathf.CeilToInt((expandedBounds.max.x - bottomLeft.x) / cellSize);
            int numYIterations = Mathf.CeilToInt((expandedBounds.max.z - bottomLeft.y) / cellSize);

            var capacity = numXIterations * numYIterations;
            var array = new Vector2[capacity];

            float angleRad = angle * Mathf.Deg2Rad;
            float sinAngle = Mathf.Sin(angleRad);
            float cosAngle = Mathf.Cos(angleRad);

            var rotatedPoint = new Vector2(); //define a variable to use so the constructor isn't called every time as an optimisation for when processing many points.
            for (int x = 0; x < numXIterations; x++)
            {
                for (int y = 0; y < numYIterations; y++)
                {
                    // Translate point relative to rotation center
                    float translatedX = bottomLeft.x + x * cellSize - rotationCenter.x;
                    float translatedY = bottomLeft.y + y * cellSize - rotationCenter.y;

                    // Rotate point using Isine and cosine
                    float rotatedX = translatedX * cosAngle - translatedY * sinAngle;
                    float rotatedY = translatedX * sinAngle + translatedY * cosAngle;

                    // Translate back to original position
                    rotatedPoint.x = rotatedX + rotationCenter.x;
                    rotatedPoint.y = rotatedY + rotationCenter.y;

                    array[x * numYIterations + y] = rotatedPoint;
                }
            }

            return array;
        }

        public static List<Vector2> GenerateGridPointsInPolygonBoundingBox(CompoundPolygon compoundPolygon, float cellSize, float shear)
        {
            List<Vector2> gridPoints = new List<Vector2>();
            Bounds bounds = compoundPolygon.Bounds;
            var boundsMin = new Vector2(bounds.min.x, bounds.min.z);

            Vector2 size = new Vector2(bounds.size.x, bounds.size.z);

            // Calculate the dimensions of the grid
            int gridWidth = Mathf.CeilToInt(size.x / cellSize);
            int gridHeight = Mathf.CeilToInt(size.y / cellSize);

            // Calculate the offset of the top-left corner of the grid
            Vector2 offset = boundsMin;

            // Generate the grid of points
            for (int y = 0; y < gridHeight; y++)
            {
                // Calculate shear offset for the current row
                float shearOffset = shear * (y / (float)gridHeight) * bounds.size.x;

                for (int x = 0; x < gridWidth; x++)
                {
                    // Calculate wrapped X position with shear offset
                    float wrappedX = (x * cellSize + shearOffset) % bounds.size.x;
                    Vector2 position = new Vector2(wrappedX, y * cellSize) + offset;

                    // Add the position to the list of grid points
                    gridPoints.Add(position);
                }
            }

            return gridPoints;
        }

        public static void AddRandomOffset(Vector2[] points, float gridCellSize, float randomness)
        {
            var newPoint = new Vector2();
            for (var i = 0; i < points.Length; i++)
            {
                newPoint = points[i];
                float randomOffsetX = (UnityEngine.Random.value - 0.5f) * randomness * gridCellSize;
                float randomOffsetY = (UnityEngine.Random.value - 0.5f) * randomness * gridCellSize;

                newPoint.x += randomOffsetX;
                newPoint.y += randomOffsetY;

                points[i] = newPoint;
            }
        }

        public static List<Vector2> PrunePointsWithPolygon(List<Vector2> points, CompoundPolygon polygon, bool pruneInside = false)
        {
            var prunedList = new List<Vector2>(points.Count);
            for (int i = points.Count - 1; i >= 0; i--)
            {
                var point = points[i];
                if (IsPointInPolygon(point, polygon) ^ pruneInside) //logical xor to flip the result if the inside points should be pruned instead of the outside points
                    prunedList.Add(point);
            }

            return prunedList;
        }

        public static Vector2[] GenerateScatterPoints(CompoundPolygon polygon, float density, float scatter, float angle)
        {
            float cellSize = 1f / Mathf.Sqrt(density);

            var points = GenerateGridPoints(polygon.Bounds, cellSize, angle);
            AddRandomOffset(points, cellSize, scatter);
            // return PrunePointsWithPolygon(scatterPoints, polygon);
            return points;
        }

        public static bool IsPointInPolygon(Vector2 point, CompoundPolygon compoundPolygon)
        {
            //Vector2[] solidPolygon = compoundPolygon.SolidPolygon;
            // List<Vector2[]> holes = compoundPolygon.Holes;

            if (!PolygonCalculator.ContainsPoint(compoundPolygon.SolidPolygon, point))
            {
                return false;
            }

            for (int i = 1; i < compoundPolygon.Paths.Count; i++) // skip creating garbage if there are no holes
            {
                if (PolygonCalculator.ContainsPoint(compoundPolygon.Paths[i], point))
                {
                    return false;
                }
            }

            return true;
        }
    }
}

using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Functionalities.ObjectInformation
{
    public sealed class MappingTree
    {
        private class Node
        {
            public BoundingBox Bounds;
            public List<IMapping> Mappings = new();
            public Node[] Children;
            public bool IsLeaf => Children == null;

            public Node(BoundingBox bounds)
            {
                Bounds = bounds;
                Children = null;
            }
        }

        private readonly Node root;
        private readonly int maxMappings;
        private readonly int maxDepth;

        public MappingTree(BoundingBox bounds, int maxObjects = 4, int maxDepth = 10)
        {
            root = new Node(bounds);
            this.maxMappings = maxObjects;
            this.maxDepth = maxDepth;
        }

        public void RootInsert(IMapping obj) => Insert(root, obj, 0);

        private void Insert(Node node, IMapping obj, int depth)
        {
            if (!node.Bounds.Contains(obj.BoundingBox)) return;

            if (node.IsLeaf)
            {
                node.Mappings.Add(obj);
                if ((node.Mappings.Count > maxMappings && depth < maxDepth) || CouldFitInChild(node, obj))
                {
                    Subdivide(node);
                    ReinsertObjects(node);
                }
            }
            else
            {
                bool inserted = false;
                foreach (var child in node.Children)
                {
                    if (child.Bounds.Contains(obj.BoundingBox))
                    {
                        Insert(child, obj, depth + 1);
                        inserted = true;
                        break;
                    }
                }

                //no child fits so up 1 level
                if (!inserted)
                    node.Mappings.Add(obj);
            }
        }

        public void Remove(IMapping obj)
        {
            bool isRemoved = Remove(root, obj);
        }

        private bool Remove(Node node, IMapping obj)
        {
            if (!node.Bounds.Contains(obj.BoundingBox)) return false;

            if (node.Mappings.Remove(obj))
                return true;

            if (!node.IsLeaf)
            {
                foreach (var child in node.Children)
                {
                    if (Remove(child, obj))
                    {
                        MergeCheck(node);
                        return true;
                    }
                }
            }

            return false;
        }

        private void MergeCheck(Node node)
        {
            if (node.IsLeaf) return;

            int totalMappings = 0;

            foreach (var child in node.Children)
            {
                if (!child.IsLeaf)
                    return;

                totalMappings += child.Mappings.Count;

            }

            if (totalMappings == 0)
                node.Children = null;
        }

        public List<IMapping> Query<T>(BoundingBox area) where T : IMapping
        {
            List<IMapping> results = new();
            Query<T>(root, area, results);
            return results;
        }

        public List<IMapping> Query<T>(Coordinate coordinate) where T : IMapping
        {
            List<IMapping> results = new();
            Query<T>(root, coordinate, results);
            return results;
        }

        /// <summary>
        /// returns a list of featuremappings of featuremappings boundingboxes contain the input coordinate
        /// </summary>
        /// <param name="node"></param>
        /// <param name="coordinate"></param>
        /// <param name="results"></param>
        public List<IMapping> QueryMappingsContainingNode<T>(Coordinate coordinate) where T : IMapping
        {
            List<IMapping> results = new();
            QueryMappingsContainingNode<T>(root, coordinate, results);
            return results;
        }

        private void Query<T>(Node node, Coordinate coordinate, List<IMapping> results) where T : IMapping
        {
            if (!node.Bounds.Contains(coordinate)) return;

            foreach(IMapping map in node.Mappings)
                if(map is T)
                    results.Add(map);

            if (!node.IsLeaf)
            {
                foreach (var child in node.Children)
                    Query<T>(child, coordinate, results);
            }
        }

        private void Query<T>(Node node, BoundingBox area, List<IMapping> results) where T : IMapping
        {
            if (!node.Bounds.Intersects(area)) return;

            foreach (IMapping map in node.Mappings)
                if (map is T)
                    results.Add(map);

            if (!node.IsLeaf)
            {
                foreach (var child in node.Children)
                    Query<T>(child, area, results);
            }
        }

        /// <summary>
        /// returns a list of featuremappings of featuremappings boundingboxes contain the input coordinate
        /// </summary>
        /// <param name="node"></param>
        /// <param name="coordinate"></param>
        /// <param name="results"></param>
        private void QueryMappingsContainingNode<T>(Node node, Coordinate coordinate, List<IMapping> results) where T : IMapping
        {
            if (!node.Bounds.Contains(coordinate)) return;

            foreach (IMapping mapping in node.Mappings)
                if (mapping is T && mapping.BoundingBox.Contains(coordinate))
                    results.Add(mapping);

            if (!node.IsLeaf)
            {
                foreach (var child in node.Children)
                    QueryMappingsContainingNode<T>(child, coordinate, results);
            }
        }

        //lets keep the tree in a uniform coordinatesystem
        private void Subdivide(Node node)
        {
            BoundingBox bottomLeftCell;
            BoundingBox bottomRightCell;
            BoundingBox topLeftCell;
            BoundingBox topRightCell;
            GetSubdividedBoundingBoxes(node, out bottomLeftCell, out bottomRightCell, out topLeftCell, out topRightCell);

            node.Children = new[]
            {
                new Node(bottomLeftCell),
                new Node(bottomRightCell),
                new Node(topLeftCell),
                new Node(topRightCell)
            };
        }

        private bool CouldFitInChild(Node node, IMapping mapping)
        {
            BoundingBox bottomLeftCell;
            BoundingBox bottomRightCell;
            BoundingBox topLeftCell;
            BoundingBox topRightCell;
            GetSubdividedBoundingBoxes(node, out bottomLeftCell, out bottomRightCell, out topLeftCell, out topRightCell);

            return bottomLeftCell.Contains(mapping.BoundingBox) ||
                bottomRightCell.Contains(mapping.BoundingBox) ||
                topLeftCell.Contains(mapping.BoundingBox) ||
                topRightCell.Contains(mapping.BoundingBox);
        }

        private void GetSubdividedBoundingBoxes(Node node, out BoundingBox bottomLeft, out BoundingBox bottomRight, out BoundingBox topLeft, out BoundingBox topRight)
        {
            if (node.Bounds.CoordinateSystem != CoordinateSystem.WGS84_LatLon) //we need a 2 dimensional coordinatesystem to do the subdivision
                node.Bounds.Convert(CoordinateSystem.WGS84_LatLon);

            Coordinate bl = node.Bounds.BottomLeft;
            Coordinate tr = node.Bounds.TopRight;
            Coordinate center = node.Bounds.Center;
            Coordinate bottomCenter = new Coordinate(CoordinateSystem.WGS84_LatLon, center.value1, bl.value2);
            Coordinate rightCenter = new Coordinate(CoordinateSystem.WGS84_LatLon, tr.value1, center.value2);
            Coordinate leftCenter = new Coordinate(CoordinateSystem.WGS84_LatLon, bl.value1, center.value2);
            Coordinate topCenter = new Coordinate(CoordinateSystem.WGS84_LatLon, center.value1, tr.value2);

            bottomLeft = new BoundingBox(bl, center);
            bottomRight = new BoundingBox(bottomCenter, rightCenter);
            topLeft = new BoundingBox(leftCenter, topCenter);
            topRight = new BoundingBox(center, tr);
        }

        private void ReinsertObjects(Node node)
        {
            var objs = node.Mappings;
            node.Mappings = new List<IMapping>();
            foreach (var obj in objs)
                RootInsert(obj);
        }

        public void DebugTree()
        {
            DebugNode(root, true);
        }

        private void DebugNode(Node node, bool recursive)
        {
            node.Bounds.Debug(Color.green);
            foreach (IMapping mapping in node.Mappings)
                if(mapping is FeatureMapping)
                    Debug.DrawLine(mapping.BoundingBox.BottomLeft.ToUnity(), node.Bounds.BottomLeft.ToUnity(), Color.red);
                else if(mapping is MeshMapping)
                    Debug.DrawLine(mapping.BoundingBox.BottomLeft.ToUnity(), node.Bounds.BottomLeft.ToUnity(), Color.yellow);
                else if(mapping is MeshMappingItem)
                    Debug.DrawLine(mapping.BoundingBox.BottomLeft.ToUnity(), node.Bounds.BottomLeft.ToUnity(), Color.black);

            foreach (IMapping mapping in node.Mappings)
                if (mapping is FeatureMapping)
                    mapping.BoundingBox.Debug(Color.magenta);
                else if(mapping is MeshMapping)
                    mapping.BoundingBox.Debug(Color.cyan);
                else if(mapping is MeshMappingItem)
                    mapping.BoundingBox.Debug(Color.blue);

            if (recursive && !node.IsLeaf)
                foreach (Node child in node.Children)
                    DebugNode(child, recursive);
        }
    }
}

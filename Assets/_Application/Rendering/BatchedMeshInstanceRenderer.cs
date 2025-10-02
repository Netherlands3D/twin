using System;
using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons;
using UnityEngine;
using UnityEngine.Rendering;

namespace Netherlands3D.Twin.Rendering
{
    public class BatchColor
    {
        public Vector4[] Colors;
        public MaterialPropertyBlock MaterialPropertyBlock;

        public BatchColor(int capacity, Color defaultColor)
        {
            Colors = new Vector4[capacity];
            for (int i = 0; i < capacity; i++)
                Colors[i] = defaultColor;

            MaterialPropertyBlock = new MaterialPropertyBlock();
            MaterialPropertyBlock.SetVectorArray("_SegmentColors", Colors);
        }

        public void SetColor(int index, Color color)
        {
            Colors[index] = color;
            MaterialPropertyBlock.SetVectorArray("_SegmentColors", Colors);
        }

        public void SetAllColors(Color color)
        {
            for (int i = 0; i < Colors.Length; i++)
                Colors[i] = color;
            MaterialPropertyBlock.SetVectorArray("_SegmentColors", Colors);
        }
    }

    public abstract class BatchedMeshInstanceRenderer : MonoBehaviour
    {
        [Tooltip("The mesh to use for the points/joints")] [SerializeField]
        private Mesh pointMesh;

        [SerializeField] private Material pointMaterial; 

        [Tooltip("Force all point Y positions to 0")] [SerializeField]
        protected bool flattenY = false;

        [Tooltip("Offset the Y position of the points")] [SerializeField]
        protected float offsetY = 0.0f;

        [SerializeField] protected float pointMeshScale = 5f;

        protected List<List<Coordinate>> positionCollections = new();
        protected int pointCount; //cached amount of points in the PositionCollections, so this does not have to be recalculated every matrix update to increase performance.
        protected List<List<Matrix4x4>> pointTransformMatrixCache = new List<List<Matrix4x4>>();

        protected List<BatchColor> pointBatchColors = new();

        protected Camera renderCamera;
        [SerializeField] protected LayerMask layerMask = -1;

        public Mesh PointMesh
        {
            get => pointMesh;
            set => pointMesh = value;
        }

        public Material PointMaterial
        {
            get => pointMaterial;
            set
            {
                pointMaterial = value;
                if (pointMaterial != null)
                {
                    SetDefaultColors();
                }
            }
        }

        public float PointMeshScale
        {
            get => pointMeshScale;
            set
            {
                pointMeshScale = value;
                GenerateTransformMatrixCache();
            }
        }

        public bool FlattenY
        {
            get => flattenY;
            set
            {
                flattenY = value;
                GenerateTransformMatrixCache();
            }
        }

        public float OffsetY
        {
            get => offsetY;
            set
            {
                offsetY = value;
                GenerateTransformMatrixCache();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            GenerateTransformMatrixCache();
        }
#endif

        protected abstract void GenerateTransformMatrixCache(int collectionStartIndex = -1);
        protected void Start()
        {
            SetProjected(true);
        }

        public void SetProjected(bool projected)
        {
            if (projected)
            {
                renderCamera = ServiceLocator.GetService<PolygonDecalProjector>().ProjectionCamera;
                layerMask = LayerMask.NameToLayer("Projected");
            }
            else
            {
                renderCamera = Camera.main;
                layerMask = LayerMask.NameToLayer("Default");
            }
        }

        private void Update()
        {
            Draw();
        }

        protected virtual void Draw()
        {
            UpdateColorBuffers();
            for (var i = 0; i < pointTransformMatrixCache.Count; i++)
            {
                var batch = pointTransformMatrixCache[i];
                Graphics.DrawMeshInstanced(pointMesh, 0, pointMaterial, batch, pointBatchColors[i].MaterialPropertyBlock, ShadowCastingMode.Off, false, layerMask, renderCamera);
            }
        }

        protected virtual void OnDrawGizmos() //todo delete
        {
            Gizmos.color = Color.red;
            DrawGizmos(pointTransformMatrixCache);
        }

        protected void DrawGizmos(List<List<Matrix4x4>> transformMatrixCache)
        {
            for (var i = 0; i < transformMatrixCache.Count; i++)
            {
                var batch = transformMatrixCache[i];
                foreach (var point in batch)
                {
                    Gizmos.DrawSphere(point.GetPosition(), 5);
                }
            }
        }
        
        protected int GetFlattenedStartIndex(int collectionStartIndex)
        {
            if (collectionStartIndex < 0 || collectionStartIndex >= positionCollections.Count)
                return -1;

            int flattenedStartIndex = 0;
            for (int i = 0; i < collectionStartIndex; i++)
            {
                flattenedStartIndex += positionCollections[i].Count;
            }

            return flattenedStartIndex;
        }

        protected (int batchIndex, int matrixIndex) GetMatrixIndices(int flattenedStartIndex)
        {
            if (flattenedStartIndex < 0 || flattenedStartIndex >= pointCount)
                return (-1, -1);

            int totalJointsBeforeStartIndex = 0;
            int currentBatchSize = 1023;

            // Traverse through collections to calculate the cumulative count directly
            foreach (var collection in positionCollections)
            {
                if (flattenedStartIndex < collection.Count)
                {
                    totalJointsBeforeStartIndex += flattenedStartIndex;
                    break;
                }

                flattenedStartIndex -= collection.Count;
                totalJointsBeforeStartIndex += collection.Count;
            }

            return (totalJointsBeforeStartIndex / currentBatchSize, totalJointsBeforeStartIndex % currentBatchSize);
        }

        protected static void AppendMatrixToBatches(List<List<Matrix4x4>> batchList, ref int arrayIndex, ref int matrixIndex, Matrix4x4 valueToAdd)
        {
            //start a new batch if needed
            if (arrayIndex >= batchList.Count && matrixIndex == 0)
            {
                batchList.Add(new List<Matrix4x4>(1023));
            }

            //append new array if index overshoots
            if (matrixIndex >= 1023) //matrix index exceeds batch size, so add a new batch and reset the matrix index
            {
                arrayIndex++;
                batchList.Add(new List<Matrix4x4>(1023));
                matrixIndex -= 1023; //todo: account for matrixIndex larger than 2046
            }

            if (matrixIndex < batchList[arrayIndex].Count)
                batchList[arrayIndex][matrixIndex] = valueToAdd;
            else
                batchList[arrayIndex].Add(valueToAdd);
            matrixIndex++;
        }

        public virtual void Clear()
        {
            positionCollections.Clear();
            RecalculatePointCount();
            pointTransformMatrixCache = new List<List<Matrix4x4>>();
            pointBatchColors.Clear();
        }

        private void RecalculatePointCount()
        {
            pointCount = 0;
            foreach (var list in positionCollections)
            {
                pointCount += list.Count;
            }
        }

        /// <summary>
        /// Set the current list of lines (overwriting any previous list)
        /// </summary>
        public void SetPositionCollections(List<List<Coordinate>> collections)
        {
            Clear();
            AppendCollections(collections);
        }

        /// <summary>
        /// Append single line to the current list of lines
        /// </summary>
        public void AppendCollection(List<Coordinate> collection)
        {
            if (!IsValid(collection)) return;

            var startIndex = positionCollections.Count;
            positionCollections.Add(collection);
            RecalculatePointCount();
            GenerateTransformMatrixCache(startIndex);
        }
        
        /// <summary>
        /// Append multiple lines to the current list of lines
        /// </summary>
        public void AppendCollections(List<List<Coordinate>> collections)
        {
            if(collections == null)
                return;
            
            var startIndex = positionCollections.Count;
            // Collections.Count can have empty collections that will not be added, so we set the capacity to the current count + the potential added count instead of increasing the capacity by collections.Count.
            // In case this function will be called multiple times, we prevent the capacity by increasing too much.
            positionCollections.Capacity = positionCollections.Count + collections.Count;
            foreach (var collection in collections)
            {
                if(!IsValid(collection)) 
                    continue;
                positionCollections.Add(collection);
            }

            RecalculatePointCount();
            GenerateTransformMatrixCache(startIndex);
        }
        
        /// <summary>
        /// Remove a collection of points using list reference
        /// </summary>
        public void RemovePointCollection(List<Coordinate> points)
        {
            if (!IsValid(points))
                return;

            positionCollections.Remove(points);

            RecalculatePointCount();
            GenerateTransformMatrixCache(-1);
        }
        
        public void RemovePointCollections(List<List<Coordinate>> points)
        {
            foreach (var collection in points)
            {
                if (!IsValid(collection))
                    continue;
                
                positionCollections.Remove(collection);
            }

            RecalculatePointCount();
            GenerateTransformMatrixCache(-1);
        }
        
        protected virtual bool IsValid(List<Coordinate> collection)
        {
            if (collection == null || collection.Count == 0)
                return false;
            return true;
        }

        protected virtual void UpdateColorBuffers()
        {
            while (pointTransformMatrixCache.Count > pointBatchColors.Count)
            {
                var colorCache = new BatchColor(1023, PointMaterial.color);
                pointBatchColors.Add(colorCache);
            }
        }

        public virtual void SetDefaultColors()
        {
            Color defaultColor = PointMaterial.color;
            foreach (var batchColor in pointBatchColors)
            {
                batchColor.SetAllColors(defaultColor);
            }

            UpdateColorBuffers(); //fill in the missing colors with the default color after resetting the existing colors to avoid setting them twice.
        }
    }
}
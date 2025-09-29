using System;
using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons;
using UnityEngine;
using UnityEngine.Rendering;

namespace Netherlands3D.Twin.Rendering
{
    public class BatchedMeshInstanceRenderer2 : MonoBehaviour
    {
        [Tooltip("The mesh to use for the points/joints")] [SerializeField]
        private Mesh pointMesh;

        [SerializeField] private Material pointMaterial;

        [Tooltip("Force all point Y positions to 0")] [SerializeField]
        protected bool flattenY = false;

        [Tooltip("Offset the Y position of the points")] [SerializeField]
        protected float offsetY = 0.0f;

        [SerializeField] protected float pointMeshScale = 5f;

        protected List<List<Coordinate>> positionCollections;
        protected int pointCount; //cached amount of points in the PositionCollections, so this does not have to be recalculated every matrix update to increase performance.
        protected List<List<Matrix4x4>> pointTransformMatrixCache = new List<List<Matrix4x4>>();
        protected List<MaterialPropertyBlock> pointMaterialPropertyBlockCache = new List<MaterialPropertyBlock>();
        protected List<Vector4[]> pointColorCache = new List<Vector4[]>();

        protected Camera renderCamera;
        [SerializeField] protected LayerMask layerMask = -1;
        protected bool cacheReady = false;

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
                    SetDefaultColors();
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
            if (cacheReady)
                Draw();
        }

        protected virtual void Draw()
        {
                Debug.Log("drawing: " + pointCount);
            for (var i = 0; i < pointTransformMatrixCache.Count; i++)
            {
                var batch = pointTransformMatrixCache[i];
                Graphics.DrawMeshInstanced(pointMesh, 0, pointMaterial, batch, pointMaterialPropertyBlockCache[i], ShadowCastingMode.Off, false, layerMask, renderCamera);
            }
        }

        private void OnDrawGizmos()
        {
            for (var i = 0; i < pointTransformMatrixCache.Count; i++)
            {
                var batch = pointTransformMatrixCache[i];
                foreach (var point in batch)
                {
                    Debug.Log(point.GetPosition());
                    Gizmos.DrawSphere(point.GetPosition(), 1);
                }
            }
        }

        protected virtual void GenerateTransformMatrixCache(int startIndex = -1)
        {
            if (positionCollections == null || positionCollections.Count < 1) return;

            var batchCount = (pointCount / 1023) + 1; //x batches of 1023 + 1 for the remainder

            if (startIndex < 0) //reset cache completely
            {
                pointTransformMatrixCache = new List<List<Matrix4x4>>(batchCount);
                startIndex = 0;
            }

            pointTransformMatrixCache.Capacity = batchCount;

            var matrixIndices = GetMatrixIndices(positionCollections, startIndex); //each point in the line is a joint

            for (var i = startIndex; i < positionCollections.Count; i++)
            {
                var collection = positionCollections[i];
                for (int j = 0; j < collection.Count; j++)
                {
                    var currentPoint = collection[j].ToUnity();

                    // Flatten the Y axis if needed
                    currentPoint.y = (FlattenY ? 0 : currentPoint.y) + offsetY;

                    // Create the joint using a sphere aligned with the cylinder (with matching faces for smooth transition between the two)
                    var scale = new Vector3(PointMeshScale, PointMeshScale, PointMeshScale);
                    Matrix4x4 jointTransformMatrix = Matrix4x4.TRS(currentPoint, Quaternion.identity, scale); //todo: add serialized rotation?
                    AppendMatrixToBatches(pointTransformMatrixCache, ref matrixIndices.batchIndex, ref matrixIndices.matrixIndex, jointTransformMatrix);
                }
            }

            UpdateBuffers();

            cacheReady = true;
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

        private void UpdateBuffers()
        {
            while (pointTransformMatrixCache.Count > pointMaterialPropertyBlockCache.Count)
            {
                MaterialPropertyBlock props = new MaterialPropertyBlock();
                Vector4[] colorCache = new Vector4[1023];
                Color defaultColor = PointMaterial.color;
                for (int j = 0; j < colorCache.Length; j++)
                    colorCache[j] = defaultColor;
                pointColorCache.Add(colorCache);
                props.SetVectorArray("_SegmentColors", colorCache);
                pointMaterialPropertyBlockCache.Add(props);
            }
        }

        protected static (int batchIndex, int matrixIndex) GetMatrixIndices(List<List<Coordinate>> positionCollections, int startIndex)
        {
            if (startIndex < 0)
                return (-1, -1);

            int totalJointsBeforeStartIndex = 0;
            int currentBatchSize = 1023;

            // Traverse through collections to calculate the cumulative count directly
            foreach (var collection in positionCollections)
            {
                if (startIndex < collection.Count)
                {
                    totalJointsBeforeStartIndex += startIndex;
                    break;
                }

                startIndex -= collection.Count;
                totalJointsBeforeStartIndex += collection.Count;
            }

            return (totalJointsBeforeStartIndex / currentBatchSize, totalJointsBeforeStartIndex % currentBatchSize);
        }

        public void SetDefaultColors()
        {
            Color defaultColor = PointMaterial.color;
            for (int batchIndex = 0; batchIndex < pointTransformMatrixCache.Count; batchIndex++)
            {
                Vector4[] colors = pointColorCache[batchIndex];
                for (int segmentIndex = 0; segmentIndex < colors.Length; segmentIndex++)
                    colors[segmentIndex] = defaultColor;
                pointColorCache[batchIndex] = colors;
                MaterialPropertyBlock props = pointMaterialPropertyBlockCache[batchIndex];
                props.SetVectorArray("_SegmentColors", colors);
                pointMaterialPropertyBlockCache[batchIndex] = props;
            }
        }

        public virtual void Clear()
        {
            positionCollections.Clear();
            RecalculatePointCount();
            pointMaterialPropertyBlockCache.Clear();
            pointColorCache.Clear();
            pointTransformMatrixCache = new List<List<Matrix4x4>>();
            cacheReady = false;
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
            positionCollections = collections;
            RecalculatePointCount();
            GenerateTransformMatrixCache();
        }

        /// <summary>
        /// Append single line to the current list of lines
        /// </summary>
        public void AppendCollection(List<Coordinate> collection)
        {
            if (positionCollections == null)
                positionCollections = new List<List<Coordinate>>();

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
            if (positionCollections == null)
                positionCollections = new List<List<Coordinate>>();

            var startIndex = positionCollections.Count;
            positionCollections.AddRange(collections);
            RecalculatePointCount();
            GenerateTransformMatrixCache(startIndex);
        }

        /// <summary>
        /// Remove a collection of points using list reference
        /// </summary>
        public void RemovePointCollection(List<Coordinate> points)
        {
            if (positionCollections == null || positionCollections.Count < 1) return;

            positionCollections.Remove(points);

            RecalculatePointCount();
            GenerateTransformMatrixCache(-1);
        }
        
#region MoveToStylerClass //TODO: move this block to a styler class like CartesianLayerStyler
        
        /// <summary>
        /// Return the batch index and line index as a tuple of the closest point to a given point.
        /// Handy for selecting a line based on a click position.
        /// </summary>
        protected static (int batchIndex, int instanceIndex) GetClosestPointIndex(List<List<Matrix4x4>> transformCaches, Vector3 point)
        {
            int closestBatchIndex = -1;
            int closestLineIndex = -1;
            float closestSqrDistance = float.MaxValue;
            for (int i = 0; i < transformCaches.Count; i++)
            {
                var transformCache = transformCaches[i];
                for(int j = 0; j < transformCache.Count; j++)
                {
                    Vector3 linePoint = transformCache[j].GetPosition();
                    var sqrDistance = Vector3.SqrMagnitude(linePoint - point);
                    if (sqrDistance < closestSqrDistance)
                    {
                        closestSqrDistance = sqrDistance;
                        closestBatchIndex = i;
                        closestLineIndex = j;
                    }
                }
            }
            return (closestBatchIndex, closestLineIndex);
        }

        /// <summary>
        /// Set a specific line color by index of the line.
        /// May be used for 'highlighting' a line, in combination with the ClosestLineToPoint method.
        /// </summary>
        public void SetPointColorByBatchIndex(int batchIndex, int segmentIndex, Color color)
        {
            if (batchIndex >= pointMaterialPropertyBlockCache.Count)
            {
                Debug.LogError($"Index {batchIndex} is out of range");
                return;
            }
            UpdateBuffers();            
            pointColorCache[batchIndex][segmentIndex] = color;
            pointMaterialPropertyBlockCache[batchIndex].SetVectorArray("_SegmentColors", pointColorCache[batchIndex]);
        }

        /// <summary>
        /// Set specific point color for the point closest to a given point.
        /// </summary>
        public void SetPointColorClosestToPoint(Vector3 point, Color color)
        {
            var indexPosition = GetClosestPointIndex(pointTransformMatrixCache, point);
            if (indexPosition.Item1 == -1 || indexPosition.Item2 == -1)
            {
                Debug.LogError("No point found");
                return;
            }

            SetPointColorByBatchIndex(indexPosition.batchIndex, indexPosition.instanceIndex, color);            
        }        
#endregion
    }
}
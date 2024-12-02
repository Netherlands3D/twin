using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Coordinates;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Netherlands3D.Twin
{
    public class BatchedMeshInstanceRenderer : MonoBehaviour
    {
        [Tooltip("The mesh to use for the points")] [SerializeField]
        private Mesh mesh;

        [SerializeField] private Material material;

        [Tooltip("Force all point Y positions to 0")] [SerializeField]
        private bool flattenY = false;

        [Tooltip("Offset the Y position of the line")] [SerializeField]
        private float offsetY = 0.0f;

        [SerializeField] private float meshScale = 0.2f;

        public List<List<Coordinate>> PositionCollections { get; private set; }
        private int pointCount; //cached amount of points in the PositionCollections, so this does not have to be recalculated every matrix update to increase performance.
        private List<List<Matrix4x4>> transformMatrixCache = new List<List<Matrix4x4>>();

        private List<MaterialPropertyBlock> materialPropertyBlockCache = new List<MaterialPropertyBlock>();
        private List<Vector4[]> segmentColorCache = new List<Vector4[]>();

        private Camera projectionCamera;
        private LayerMask layerMask = -1;
        private bool cacheReady = false;

        public Mesh Mesh
        {
            get => mesh;
            set => mesh = value;
        }

        public Material Material
        {
            get => material;
            set => material = value;
        }

        public float MeshScale
        {
            get => meshScale;
            set
            {
                meshScale = value;
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
        
        private void Start()
        {
            projectionCamera = GameObject.FindWithTag("ProjectorCamera").GetComponent<Camera>();
            layerMask = LayerMask.NameToLayer("Projected");
        }
        private void Update()
        {
            if (cacheReady)
                DrawLines();
        }

        private void OnValidate()
        {
            GenerateTransformMatrixCache();
        }

        private void DrawLines()
        {
            for (var i = 0; i < transformMatrixCache.Count; i++)
            {
                var batch = transformMatrixCache[i];
                Graphics.DrawMeshInstanced(Mesh, 0, Material, batch, materialPropertyBlockCache[i], ShadowCastingMode.Off, false, layerMask, projectionCamera);
            }
        }

        /// <summary>
        /// Return the batch index and line index as a tuple of the closest point to a given point.
        /// Handy for selecting a line based on a click position.
        /// </summary>
        public (int batchIndex, int instanceIndex) GetClosestInstanceIndex(Vector3 point)
        {
            int closestBatchIndex = -1;
            int closestLineIndex = -1;
            float closestDistance = float.MaxValue;
            for (int i = 0; i < transformMatrixCache.Count; i++)
            {
                var lineTransforms = transformMatrixCache[i];
                for(int j = 0; j < lineTransforms.Count; j++)
                {
                    var linePoint = lineTransforms[j].GetColumn(3);
                    var distance = Vector3.Distance(linePoint, point);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestBatchIndex = i;
                        closestLineIndex = j;
                    }
                }
            }
            return (closestBatchIndex, closestLineIndex);
        }

        private void UpdateBuffers()
        {
            while (transformMatrixCache.Count > materialPropertyBlockCache.Count)
            {
                MaterialPropertyBlock props = new MaterialPropertyBlock();
                Vector4[] colorCache = new Vector4[1023];
                Color defaultColor = Material.GetColor("_Color");
                for (int j = 0; j < colorCache.Length; j++)
                    colorCache[j] = defaultColor;
                segmentColorCache.Add(colorCache);
                props.SetVectorArray("_SegmentColors", colorCache);
                materialPropertyBlockCache.Add(props);
            }
        }

        public void SetDefaultColors()
        {
            Color defaultColor = Material.GetColor("_Color");
            for (int batchIndex = 0; batchIndex < transformMatrixCache.Count; batchIndex++)
            {
                Vector4[] colors = segmentColorCache[batchIndex];
                for(int segmentIndex = 0; segmentIndex < colors.Length; segmentIndex++)
                    colors[segmentIndex] = defaultColor;
                segmentColorCache[batchIndex] = colors;
                MaterialPropertyBlock props = materialPropertyBlockCache[batchIndex];
                props.SetVectorArray("_SegmentColors", colors);
                materialPropertyBlockCache[batchIndex] = props;
            }
        }

        /// <summary>
        /// Set a specific line color by index of the line.
        /// May be used for 'highlighting' a line, in combination with the ClosestLineToPoint method.
        /// </summary>
        public void SetSpecificLineColorByIndex(int batchIndex, int segmentIndex, Color color)
        {
            if (batchIndex >= materialPropertyBlockCache.Count)
            {
                Debug.LogError($"Index {batchIndex} is out of range");
                return;
            }
            UpdateBuffers();            
            segmentColorCache[batchIndex][segmentIndex] = color;
            materialPropertyBlockCache[batchIndex].SetVectorArray("_SegmentColors", segmentColorCache[batchIndex]);
        }

        /// <summary>
        /// Set specific line color for the line closest to a given point.
        /// </summary>
        public void SetLineColorClosestToPoint(Vector3 point, Color color)
        {
            var indexPosition = GetClosestInstanceIndex(point);
            if (indexPosition.Item1 == -1 || indexPosition.Item2 == -1)
            {
                Debug.LogError("No line found");
                return;
            }

            SetSpecificLineColorByIndex(indexPosition.batchIndex, indexPosition.instanceIndex, color);            
        }        

        /// <summary>
        /// Set a single line (overwriting any previous lines)
        /// </summary>
        public void SetLine(List<Coordinate> linePoints)
        {
            PositionCollections = new List<List<Coordinate>> { linePoints };
            SetPositionCollections(PositionCollections);
        }

        /// <summary>
        /// Set the current list of lines (overwriting any previous list)
        /// </summary>
        public void SetPositionCollections(List<List<Coordinate>> collections)
        {
            PositionCollections = collections;
            RecalculatePointCount();
            GenerateTransformMatrixCache();
        }

        /// <summary>
        /// Append single line to the current list of lines
        /// </summary>
        public void AppendCollection(List<Coordinate> collection)
        {
            if (PositionCollections == null)
                PositionCollections = new List<List<Coordinate>>();

            var startIndex = PositionCollections.Count;
            PositionCollections.Add(collection);
            RecalculatePointCount();
            GenerateTransformMatrixCache(startIndex);
        }

        /// <summary>
        /// Append multiple lines to the current list of lines
        /// </summary>
        public void AppendCollections(List<List<Coordinate>> collections)
        {
            if (PositionCollections == null)
                PositionCollections = new List<List<Coordinate>>();

            var startIndex = PositionCollections.Count;
            PositionCollections.AddRange(collections);
            RecalculatePointCount();
            GenerateTransformMatrixCache(startIndex);
        }

        /// <summary>
        /// Remove a collection of points using list reference
        /// </summary>
        public void RemoveCollection(List<Coordinate> points)
        {
            if (PositionCollections == null || PositionCollections.Count < 1) return;

            PositionCollections.Remove(points);

            RecalculatePointCount();
            GenerateTransformMatrixCache(-1);
        }

        public void ClearLines()
        {
            PositionCollections.Clear();
            RecalculatePointCount();
            materialPropertyBlockCache.Clear();
            segmentColorCache.Clear();
            transformMatrixCache = new List<List<Matrix4x4>>();
            cacheReady = false;
        }

        public void GenerateTransformMatrixCache(int startIndex = -1)
        {
            if (PositionCollections == null || PositionCollections.Count < 1) return;

            var batchCount = (pointCount / 1023) + 1; //x batches of 1023 + 1 for the remainder

            if (startIndex < 0) //reset cache completely
            {
                transformMatrixCache = new List<List<Matrix4x4>>(batchCount);
                startIndex = 0;
            }

            transformMatrixCache.Capacity = batchCount;

            var matrixIndices = GetMatrixIndices(PositionCollections, startIndex); //each point in the line is a joint

            for (var i = startIndex; i < PositionCollections.Count; i++)
            {
                var collection = PositionCollections[i];
                for (int j = 0; j < collection.Count; j++)
                {
                    var currentPoint = collection[j].ToUnity();

                    // Flatten the Y axis if needed
                    currentPoint.y = (FlattenY ? 0 : currentPoint.y) + offsetY;

                    // Create the joint using a sphere aligned with the cylinder (with matching faces for smooth transition between the two)
                    var scale = new Vector3(MeshScale, MeshScale, MeshScale);
                    Matrix4x4 jointTransformMatrix = Matrix4x4.TRS(currentPoint, quaternion.identity, scale); //todo: add serialized rotation?
                    AppendMatrixToBatches(transformMatrixCache, ref matrixIndices.batchIndex, ref matrixIndices.matrixIndex, jointTransformMatrix);
                }
            }

            UpdateBuffers();

            cacheReady = true;
        }

        private static void AppendMatrixToBatches(List<List<Matrix4x4>> batchList, ref int arrayIndex, ref int matrixIndex, Matrix4x4 valueToAdd)
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

        private static (int batchIndex, int matrixIndex) GetMatrixIndices(List<List<Coordinate>> collections, int startIndex)
        {
            if (startIndex < 0)
                return (-1, -1);

            int totalJointsBeforeStartIndex = 0;
            int currentBatchSize = 1023;

            // Traverse through collections to calculate the cumulative count directly
            foreach (var collection in collections)
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
        
        private void RecalculatePointCount()
        {
            pointCount = 0;
            foreach (var list in PositionCollections)
            {
                pointCount += list.Count;
            }
        }
    }
}
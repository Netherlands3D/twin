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
        private List<List<Matrix4x4>> transformMatrixCache = new List<List<Matrix4x4>>();

        private Camera projectionCamera;
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
                Graphics.DrawMeshInstanced(Mesh, 0, Material, batch, null, ShadowCastingMode.Off, false, LayerMask.NameToLayer("Projected"), projectionCamera);
            }
        }

        /// <summary>
        /// Return the index of the closest point to a given point.
        /// Handy for selecting a line based on a click position.
        /// </summary>
        public int GetClosestLineIndex(Vector3 point)
        {
            int closestLineIndex = -1;
            float closestDistance = float.MaxValue;
            for (int i = 0; i < transformMatrixCache.Count; i++)
            {
                var lineTransforms = transformMatrixCache[i];
                foreach (var lineTransform in lineTransforms)
                {
                    var linePoint = lineTransform.GetColumn(3);
                    var distance = Vector3.Distance(linePoint, point);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestLineIndex = i;
                    }
                }
            }

            return closestLineIndex;
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
            GenerateTransformMatrixCache(startIndex);
        }

        public void ClearLines()
        {
            PositionCollections.Clear();
            transformMatrixCache = new List<List<Matrix4x4>>();
            cacheReady = false;
        }

        public void GenerateTransformMatrixCache(int startIndex = -1)
        {
            if (PositionCollections == null || PositionCollections.Count < 1) return;

            var pointCount = PositionCollections.SelectMany(list => list).Count(); //each position should have a matrix
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

            // Iterate over the Lines to find the total number of Vector3s before the startIndex
            int totalJointsBeforeStartIndex = collections.Take(startIndex).Sum(list => list.Count);

            return (totalJointsBeforeStartIndex / 1023, totalJointsBeforeStartIndex % 1023);
        }
    }
}
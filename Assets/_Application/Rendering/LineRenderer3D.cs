using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Coordinates;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons;
using UnityEngine;
using UnityEngine.Rendering;

namespace Netherlands3D.Twin.Rendering
{
    public class LineRenderer3D : BatchedMeshInstanceRenderer2
    {
        [Header("References")] 
        [Tooltip("The mesh to use for the line segments")] [SerializeField]
        private Mesh lineMesh;

        [SerializeField] private Material lineMaterial;
        [SerializeField] private Material lineSelectionMaterial; //todo: move to base

        [Header("Settings")] 
        [SerializeField] private bool drawJoints = true;
        [SerializeField] private float lineDiameter = 1f;
        
        private List<List<Matrix4x4>> lineTransformMatrixCache = new List<List<Matrix4x4>>();
        private List<MaterialPropertyBlock> segmentPropertyBlockCache = new List<MaterialPropertyBlock>();
        private List<Vector4[]> segmentColorCache = new List<Vector4[]>();

        private MaterialPropertyBlock selectedSegmentMaterialPropertyBlock;
        private MaterialPropertyBlock selectedJointMaterialPropertyBlock;
        private List<Matrix4x4> selectedLineTransforms = new List<Matrix4x4>();
        private List<Matrix4x4> selectedJointTransforms = new List<Matrix4x4>();
        private List<Vector4> selectedLineColorCache = new List<Vector4>();
        private List<Vector4> selectedJointColorCache = new List<Vector4>();
        private int selectedLineIndex = -1;

        public Mesh LineMesh
        {
            get => lineMesh;
            set => lineMesh = value;
        }

        public Material LineMaterial
        {
            get => lineMaterial;
            set
            {
                lineMaterial = value;
                if (lineMaterial != null)
                    SetDefaultColors();
            }
        }

        public bool DrawJoints
        {
            get => drawJoints;
            set => drawJoints = value;
        }

        public float LineDiameter
        {
            get => lineDiameter;
            set
            {
                lineDiameter = value;
                GenerateTransformMatrixCache();
            }
        }

        protected override void Draw()
        {
            if (DrawJoints)
            {
                base.Draw(); // joints are the same as points.
            }
            Debug.Log("drawing lines");

            for (var i = 0; i < lineTransformMatrixCache.Count; i++)
            {
                var lineTransforms = lineTransformMatrixCache[i];
                Graphics.DrawMeshInstanced(LineMesh, 0, LineMaterial, lineTransforms, segmentPropertyBlockCache[i], ShadowCastingMode.Off, false, layerMask, renderCamera);
            }

            if (selectedLineIndex >= 0)
            {
                Graphics.DrawMeshInstanced(LineMesh, 0, lineSelectionMaterial, selectedLineTransforms, selectedSegmentMaterialPropertyBlock, ShadowCastingMode.Off, false, layerMask, renderCamera);
                if (DrawJoints)
                    Graphics.DrawMeshInstanced(PointMesh, 0, lineSelectionMaterial, selectedJointTransforms, selectedJointMaterialPropertyBlock, ShadowCastingMode.Off, false, layerMask, renderCamera);
            }
        }

        /// <summary>
        /// Return the batch index and line index as a tuple of the closest point to a given point.
        /// Handy for selecting a line based on a click position.
        /// </summary>
        // public (int batchindex, int jointIndex) GetClosestJointIndex(Vector3 point)
        // {
        //     int closestBatchIndex = -1;
        //     int closestJointIndex = -1;
        //     float closestDistance = float.MaxValue;
        //     for (int i = 0; i < pointTransformMatrixCache.Count; i++)
        //     {
        //         var jointTransforms = pointTransformMatrixCache[i];
        //         for (int j = 0; j < jointTransforms.Count; j++)
        //         {
        //             Vector3 linePoint = jointTransforms[j].GetColumn(3);
        //             float distance = Vector3.SqrMagnitude(point - linePoint);
        //             if (distance < closestDistance * closestDistance)
        //             {
        //                 closestDistance = Mathf.Sqrt(distance);
        //                 closestBatchIndex = i;
        //                 closestJointIndex = j;
        //             }
        //         }
        //     }
        //
        //     return (closestBatchIndex, closestJointIndex);
        // }

        protected override void UpdateColorBuffers()
        {
            if (drawJoints)
            {
                base.UpdateColorBuffers(); // color joints
            }

            while (lineTransformMatrixCache.Count > segmentPropertyBlockCache.Count)
            {
                MaterialPropertyBlock props = new MaterialPropertyBlock();
                Vector4[] colorCache = new Vector4[1023];
                Color defaultColor = LineMaterial.color;
                for (int j = 0; j < colorCache.Length; j++)
                    colorCache[j] = defaultColor;
                segmentColorCache.Add(colorCache);
                props.SetVectorArray("_SegmentColors", colorCache);
                segmentPropertyBlockCache.Add(props);
            }

            // while (pointTransformMatrixCache.Count > pointMaterialPropertyBlockCache.Count)
            // {
            //     MaterialPropertyBlock props = new MaterialPropertyBlock();
            //     Vector4[] colorCache = new Vector4[1023];
            //     Color defaultColor = LineMaterial.color;
            //     for (int j = 0; j < colorCache.Length; j++)
            //         colorCache[j] = defaultColor;
            //     pointColorCache.Add(colorCache);
            //     props.SetVectorArray("_SegmentColors", colorCache);
            //     pointMaterialPropertyBlockCache.Add(props);
            // }
        }

        public void SetDefaultColors()
        {
            selectedLineIndex = -1;
            Color defaultColor = LineMaterial.color;
            for (int batchIndex = 0; batchIndex < lineTransformMatrixCache.Count; batchIndex++)
            {
                Vector4[] colors = segmentColorCache[batchIndex];
                Vector4[] colors2 = pointColorCache[batchIndex];
                for (int segmentIndex = 0; segmentIndex < colors.Length; segmentIndex++)
                {
                    colors[segmentIndex] = defaultColor;
                }

                for (int segmentIndex = 0; segmentIndex < colors2.Length; segmentIndex++)
                {
                    colors2[segmentIndex] = defaultColor;
                }

                segmentColorCache[batchIndex] = colors;
                pointColorCache[batchIndex] = colors2;
                MaterialPropertyBlock props = segmentPropertyBlockCache[batchIndex];
                props.SetVectorArray("_SegmentColors", colors);
                segmentPropertyBlockCache[batchIndex] = props;
                MaterialPropertyBlock props2 = pointMaterialPropertyBlockCache[batchIndex];
                props2.SetVectorArray("_SegmentColors", colors2);
                pointMaterialPropertyBlockCache[batchIndex] = props2;
            }
        }
        
        /// <summary>
        /// all vertices of the line mesh are needed as input to solve the issue of other lines having overlapping points
        /// problem, multiple feature meshes have points at the same position in the segmenttransformmatrixcache
        /// so we need to do an extra check which centroid of the both closest points are matching with all the points
        /// </summary>
        /// <param name="points"></param>
        /// <param name="color"></param>
        public void SetLineColorFromPoints(Vector3[] points, Color color) //todo: refactor
        {
            //calculate the centroid of the targeted line
            Vector3 selectionCentroid = Vector3.zero;
            for (int i = 0; i < points.Length; i++)
                selectionCentroid += points[i];
            selectionCentroid /= points.Length;

            //compare the centroids of other lines to be sure the line is matching as the closest selected line
            //todo, maybe cache the line centroids for optimisation but now only happens when selecting
            float closest = float.MaxValue;
            int lineStartIndex = -1;
            
            for (int i = 0; i < positionCollections.Count; i++)
            {
                Vector3 lineCentroid = Vector3.zero;
                for (int j = 0; j < positionCollections[i].Count; j++)
                    lineCentroid += positionCollections[i][j].ToUnity();
                lineCentroid /= positionCollections[i].Count;
                float dist = Vector3.SqrMagnitude(selectionCentroid - lineCentroid);
                if (dist < closest * closest)
                {
                    closest = Mathf.Sqrt(dist);
                    lineStartIndex = i;
                }
            }

            if (lineStartIndex < 0) return;

            if (selectedSegmentMaterialPropertyBlock == null)
            {
                selectedSegmentMaterialPropertyBlock = new MaterialPropertyBlock();
                for (int i = 0; i < 1023; i++)
                    selectedLineColorCache.Add(color);
            }

            if (selectedJointMaterialPropertyBlock == null)
            {
                selectedJointMaterialPropertyBlock = new MaterialPropertyBlock();
                for (int i = 0; i < 1023; i++)
                    selectedJointColorCache.Add(color);
            }

            selectedLineIndex = lineStartIndex;

            //using the cache positions directly does not work as some line segments are skipped
            int count = positionCollections[selectedLineIndex].Count;
            selectedLineTransforms.Clear();
            selectedJointTransforms.Clear();

            for (int i = 0; i < count; i++)
            {
                Vector3 vertex = positionCollections[selectedLineIndex][i].ToUnity();
                if (i < count - 1)
                {
                    Matrix4x4 segMatrix = GetClosestLineCentroid(vertex);
                    selectedLineTransforms.Add(segMatrix);
                }

                Matrix4x4 jntMatrix = GetClosestPoint(vertex);
                selectedJointTransforms.Add(jntMatrix);
            }

            selectedSegmentMaterialPropertyBlock.SetVectorArray("_SegmentColors", selectedLineColorCache);
            selectedJointMaterialPropertyBlock.SetVectorArray("_SegmentColors", selectedJointColorCache);

            if (positionCollections[lineStartIndex].Count > 1023)
                Debug.LogError("the selected line feature is over 1023 vertices, a fix is needed for buffer overflow");

            //todo take in account when overflowing the buffer, but probably not needed because no selected line is 1023 vertices
            var segmentIndices = GetLineMatrixIndices(lineStartIndex);
            for (int i = 0; i < positionCollections[lineStartIndex].Count - 1; i++) //-1 we dont want to color the last segment
            {
                segmentColorCache[segmentIndices.batchIndex][segmentIndices.matrixIndex + i] = color;
            }

            var jointIndices = GetPointMatrixIndices(lineStartIndex);
            for (int i = 0; i < positionCollections[lineStartIndex].Count; i++) //we do want to color the last segment
            {
                pointColorCache[jointIndices.batchIndex][jointIndices.matrixIndex + i] = color;
            }

            segmentPropertyBlockCache[segmentIndices.batchIndex].SetVectorArray("_SegmentColors", segmentColorCache[segmentIndices.batchIndex]);
            pointMaterialPropertyBlockCache[jointIndices.batchIndex].SetVectorArray("_SegmentColors", pointColorCache[jointIndices.batchIndex]);
        }

        public override void Clear()
        {
            base.Clear();
            segmentPropertyBlockCache.Clear();
            segmentColorCache.Clear();
            lineTransformMatrixCache = new List<List<Matrix4x4>>();
        }

        protected override void GenerateTransformMatrixCache(int startIndex = -1)
        {
            base.GenerateTransformMatrixCache(); // get the point matrices as joints
            
            if (positionCollections == null || positionCollections.Count < 1) return;
            
            var segmentCount = pointCount - positionCollections.Count; // each line one more joint than segments, so subtracting the lineCount will result in the total number of segments

            // var jointBatchCount = (pointCount / 1023) + 1; //x batches of 1023 + 1 for the remainder
            var segmentBatchCount = (segmentCount / 1023) + 1; //x batches of 1023 + 1 for the remainder

            if (startIndex < 0) //reset cache completely
            {
                // pointTransformMatrixCache = new List<List<Matrix4x4>>(jointBatchCount);
                lineTransformMatrixCache = new List<List<Matrix4x4>>(segmentBatchCount);
                startIndex = 0;
            }

            // pointTransformMatrixCache.Capacity = jointBatchCount;
            lineTransformMatrixCache.Capacity = segmentBatchCount;

            // var jointIndices = GetJointMatrixIndices(startIndex); //each point in the line is a joint
            var segmentIndices = GetLineMatrixIndices(startIndex);

            for (var i = startIndex; i < positionCollections.Count; i++)
            {
                var line = positionCollections[i];
                for (int j = 0; j < line.Count - 1; j++)
                {
                    var currentPoint = line[j].ToUnity();
                    var nextPoint = line[j + 1].ToUnity();

                    var direction = nextPoint - currentPoint;
                    float distance = direction.magnitude;
                    if(distance < 0.000001f)
                        continue;

                    // Flatten the Y axis if needed
                    currentPoint.y = (FlattenY ? 0 : currentPoint.y) + offsetY;
                    nextPoint.y = (FlattenY ? 0 : nextPoint.y) + offsetY;

                    direction.Normalize();

                    // Calculate the rotation based on the direction vector
                    var rotation = Quaternion.LookRotation(direction);

                    // Calculate the scale based on the distance
                    var scale = new Vector3(LineDiameter, LineDiameter, distance);

                    // Create a transform matrix for each line point
                    Matrix4x4 transformMatrix = Matrix4x4.TRS(currentPoint, rotation, scale);
                    AppendMatrixToBatches(lineTransformMatrixCache, ref segmentIndices.batchIndex, ref segmentIndices.matrixIndex, transformMatrix);

                    // Create the joint using a sphere aligned with the cylinder (with matching faces for smooth transition between the two)
                    // var jointScale = new Vector3(LineDiameter, LineDiameter, LineDiameter);
                    // Matrix4x4 jointTransformMatrix = Matrix4x4.TRS(currentPoint, rotation, jointScale);
                    // AppendMatrixToBatches(pointTransformMatrixCache, ref jointIndices.batchIndex, ref jointIndices.matrixIndex, jointTransformMatrix);

                    //Add the last joint to cap the line end
                    // if (j == line.Count - 2)
                    // {
                    //     jointTransformMatrix = Matrix4x4.TRS(nextPoint, rotation, jointScale);
                    //     AppendMatrixToBatches(pointTransformMatrixCache, ref jointIndices.batchIndex, ref jointIndices.matrixIndex, jointTransformMatrix);
                    // }
                }
            }

            UpdateColorBuffers();
        }

        /// <summary>
        /// Gets the closest line centroid to a given position
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private Matrix4x4 GetClosestLineCentroid(Vector3 position)
        {
            var indexPosition = GetClosestPointIndex(lineTransformMatrixCache, position);
            return lineTransformMatrixCache[indexPosition.batchIndex][indexPosition.instanceIndex];
        }

        /// <summary>
        /// Gets the closest line centroid to a given position
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private Matrix4x4 GetClosestPoint(Vector3 position)
        {
            var indexPosition = GetClosestPointIndex(pointTransformMatrixCache, position);
            return pointTransformMatrixCache[indexPosition.batchIndex][indexPosition.instanceIndex];
        }
        
        private (int batchIndex, int matrixIndex) GetLineMatrixIndices(int startIndex)
        {
            if (startIndex < 0)
                return (-1, -1);
        
            // Iterate over the Lines to find the total number of Vector3s before the startIndex
            int totalJointsBeforeStartIndex = positionCollections.Take(startIndex).Sum(list => list.Count) - startIndex; // each line has one more joint than segments, so subtracting the startIndex will result in the total number of segments
            return (totalJointsBeforeStartIndex / 1023, totalJointsBeforeStartIndex % 1023);
        }

        #region MoveToStylerClass //TODO: move this block to a styler class like CartesianLayerStyler

        #endregion
    }
}
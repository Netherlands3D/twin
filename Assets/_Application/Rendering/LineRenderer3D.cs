using System.Collections.Generic;
using Netherlands3D.Coordinates;
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
        protected List<BatchColor> lineBatchColors = new();

        // private List<MaterialPropertyBlock> segmentPropertyBlockCache = new List<MaterialPropertyBlock>();
        // private List<Vector4[]> segmentColorCache = new List<Vector4[]>();

        // private MaterialPropertyBlock selectedSegmentMaterialPropertyBlock;
        // private MaterialPropertyBlock selectedJointMaterialPropertyBlock;
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

        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
            DrawGizmos(lineTransformMatrixCache);
        }

        protected override void Draw()
        {
            UpdateColorBuffers();

            for (var i = 0; i < lineTransformMatrixCache.Count; i++)
            {
                var lineTransforms = lineTransformMatrixCache[i];
                Graphics.DrawMeshInstanced(LineMesh, 0, LineMaterial, lineTransforms, lineBatchColors[i].MaterialPropertyBlock, ShadowCastingMode.Off, false, layerMask, renderCamera);
            }
            
            if (!DrawJoints) return;
            for (var i = 0; i < pointTransformMatrixCache.Count; i++)
            {
                var batch = pointTransformMatrixCache[i];
                Graphics.DrawMeshInstanced(PointMesh, 0, PointMaterial, batch, pointBatchColors[i].MaterialPropertyBlock, ShadowCastingMode.Off, false, layerMask, renderCamera);
            }
        }

        protected override void UpdateColorBuffers()
        {
            if (drawJoints)
            {
                base.UpdateColorBuffers(); // color joints
            }

            while (lineTransformMatrixCache.Count > lineBatchColors.Count)
            {
                var colorCache = new BatchColor(1023, LineMaterial.color);
                lineBatchColors.Add(colorCache);
            }
        }

        public override void SetDefaultColors()
        {
            Color defaultLineColor = LineMaterial.color;
            foreach (var batchColor in lineBatchColors)
            {
                batchColor.SetAllColors(defaultLineColor);
            }
            foreach (var batchColor in pointBatchColors)
            {
                batchColor.SetAllColors(defaultLineColor);
            }
            UpdateColorBuffers(); //fill in the missing colors with the default color after resetting the existing colors to avoid setting them twice.
        }
        
        /// <summary>
        /// all vertices of the line mesh are needed as input to solve the issue of other lines having overlapping points
        /// problem, multiple feature meshes have points at the same position in the segmenttransformmatrixcache
        /// so we need to do an extra check which centroid of the both closest points are matching with all the points
        /// </summary>
        /// <param name="points"></param>
        /// <param name="color"></param>
        public void SetLineColorFromPoints(Vector3[] points, Color color)
        {
            // We will check if the passed points[] is equal to a stored line by comparing centroids.
            // TODO: instead of passing a point[], pass the index of the line so we can directly get it from positionCollections
            var selectionCentroid = CalculateCentroid(points);
            
            int lineIndex = -1;
            float closest = float.MaxValue;

            for (var i = 0; i < positionCollections.Count; i++)
            {
                var line = positionCollections[i];
                if (points.Length == line.Count)
                {
                    var lineCentroid = CalculateCentroid(line);
                    float dist = Vector3.SqrMagnitude(selectionCentroid - lineCentroid);
                    if (dist < closest)
                    {
                        closest = dist;
                        lineIndex = i;
                    }
                }
            }
            if (lineIndex < 0) return;

            var flattenedStartIndex = GetFlattenedStartIndex(lineIndex);

            var jointBatchIndices = GetMatrixIndices(flattenedStartIndex);
            var lineBatchIndices = GetMatrixIndices(flattenedStartIndex - lineIndex);

            for (int i = 0; i < positionCollections[lineIndex].Count; i++)
            {
                pointBatchColors[jointBatchIndices.batchIndex].SetColor(jointBatchIndices.matrixIndex, color);
                lineBatchColors[lineBatchIndices.batchIndex].SetColor(lineBatchIndices.matrixIndex, color);
                
                IncrementBatchIndices(ref jointBatchIndices.batchIndex, ref jointBatchIndices.matrixIndex);
                IncrementBatchIndices(ref lineBatchIndices.batchIndex, ref lineBatchIndices.matrixIndex);
            }
        }

        private void IncrementBatchIndices(ref int batchIndex, ref int matrixIndex)
        {
            matrixIndex++;
            if (matrixIndex >= 1023) //matrix index exceeds batch size, so add a new batch and reset the matrix index
            {
                batchIndex++;
                matrixIndex -= 1023; //todo: account for matrixIndex larger than 2046
            }
        }

        private static Vector3 CalculateCentroid(Vector3[] line)
        {
            if(line == null || line.Length == 0) return Vector3.zero;

            Vector3 selectionCentroid = line[0];
            for (int i = 1; i < line.Length; i++)
                selectionCentroid += line[i];
            
            selectionCentroid /= line.Length;
            
            return selectionCentroid;
        }

        private static Vector3 CalculateCentroid(List<Coordinate> line)
        {
            if(line == null || line.Count == 0) return Vector3.zero;
            
            Vector3 lineCentroid = line[0].ToUnity();
            for (int i = 1; i < line.Count; i++)
                lineCentroid += line[i].ToUnity();
            
            lineCentroid /= line.Count;
            
            return lineCentroid;
        }

        public override void Clear()
        {
            base.Clear();
            lineTransformMatrixCache = new List<List<Matrix4x4>>();
            lineBatchColors.Clear();
        }

        protected override void GenerateTransformMatrixCache(int collectionStartIndex = -1)
        {
            // For efficiency, we combine the point and line calculation in a single loop

            var jointCount = pointCount; //each point should have a joint
            var segmentCount = jointCount - positionCollections.Count; // each line one more joint than segments, so subtracting the lineCount will result in the total number of segments

            var jointBatchCount = (jointCount / 1023) + 1; //x batches of 1023 + 1 for the remainder
            var lineBatchCount = (segmentCount / 1023) + 1; //x batches of 1023 + 1 for the remainder

            if (collectionStartIndex < 0) //reset cache completely
            {
                pointTransformMatrixCache = new List<List<Matrix4x4>>(jointBatchCount);
                lineTransformMatrixCache = new List<List<Matrix4x4>>(lineBatchCount);
                collectionStartIndex = 0;
            }

            pointTransformMatrixCache.Capacity = jointBatchCount;
            lineTransformMatrixCache.Capacity = lineBatchCount;

            var flattenedStartIndex = GetFlattenedStartIndex(collectionStartIndex);
            
            var jointIndices = GetMatrixIndices(flattenedStartIndex); //each point in the line is a joint
            var lineIndices = GetMatrixIndices(flattenedStartIndex - collectionStartIndex); //each line has one less segment than points, so we subtract the startIndex to account for the amount of segments before the start index
            
            for (var i = collectionStartIndex; i < positionCollections.Count; i++)
            {
                var line = positionCollections[i];
                for (int j = 0; j < line.Count - 1; j++)
                {
                    var currentPoint = line[j].ToUnity();
                    var nextPoint = line[j + 1].ToUnity();

                    var direction = nextPoint - currentPoint;
                    float distance = direction.magnitude;

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
                    AppendMatrixToBatches(lineTransformMatrixCache, ref lineIndices.batchIndex, ref lineIndices.matrixIndex, transformMatrix);

                    // Create the joint using a sphere aligned with the cylinder (with matching faces for smooth transition between the two)
                    var jointScale = new Vector3(LineDiameter, LineDiameter, LineDiameter);
                    Matrix4x4 jointTransformMatrix = Matrix4x4.TRS(currentPoint, rotation, jointScale);
                    AppendMatrixToBatches(pointTransformMatrixCache, ref jointIndices.batchIndex, ref jointIndices.matrixIndex, jointTransformMatrix);

                    //Add the last joint to cap the line end
                    if (j == line.Count - 2)
                    {
                        jointTransformMatrix = Matrix4x4.TRS(nextPoint, rotation, jointScale);
                        AppendMatrixToBatches(pointTransformMatrixCache, ref jointIndices.batchIndex, ref jointIndices.matrixIndex, jointTransformMatrix);
                    }
                }
            }
        }
    }
}
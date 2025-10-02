using System.Collections.Generic;
using Netherlands3D.Coordinates;
using UnityEngine;
using UnityEngine.Rendering;

namespace Netherlands3D.Twin.Rendering
{
    public class LineRenderer3D : BatchedMeshInstanceRenderer
    {
        [Header("References")] 
        [Tooltip("The mesh to use for the line segments")] [SerializeField]
        private Mesh lineMesh;

        [SerializeField] private Material lineMaterial;

        [Header("Settings")] 
        [SerializeField] private bool drawJoints = true;
        [SerializeField] private float lineDiameter = 1f;
        
        private List<List<Matrix4x4>> lineTransformMatrixCache = new List<List<Matrix4x4>>();
        private List<BatchColor> lineBatchColors = new();
        
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
            Gizmos.color = Color.cyan;
            DrawGizmos(pointTransformMatrixCache);
            Gizmos.color = Color.blue;
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

        protected override bool IsValid(List<Coordinate> line)
        {
            {
                if (line == null) 
                    return false;
                if (line.Count < 2)
                {
                    Debug.LogWarning("A line should have at least 2 points");
                    return false;
                }

                return true;
            }
        }
    }
}
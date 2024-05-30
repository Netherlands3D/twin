using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace Netherlands3D.Twin
{
    public class LineRenderer3D : MonoBehaviour
    {
        [Header("References")] [Tooltip("The mesh to use for the line segments")] [SerializeField]
        private Mesh lineMesh;

        [Tooltip("The mesh to use for the joints to get smooth corners")] [SerializeField]
        private Mesh jointMesh;

        [SerializeField] private Material lineMaterial;

        [Header("Settings")] [SerializeField] private bool drawJoints = true;

        [Tooltip("Force all point Y positions to 0")] [SerializeField]
        private bool flattenY = false;

        [Tooltip("Offset the Y position of the line")] [SerializeField]
        private float offsetY = 0.0f;

        [SerializeField] private float lineDiameter = 0.2f;

        public List<List<Vector3>> Lines { get; private set; }

        private List<List<Matrix4x4>> segmentTransformMatrixCache = new List<List<Matrix4x4>>();

        private List<List<Matrix4x4>> jointsTransformMatrixCache = new List<List<Matrix4x4>>();
        // private Matrix4x4[][] segmentTransformMatrixCache;
        // private Matrix4x4[][] jointsTransformMatrixCache;

        private List<MaterialPropertyBlock> materialPropertyBlockCache;
        private bool cacheReady = false;
        private bool hasColors = false;

        public Mesh LineMesh
        {
            get => lineMesh;
            set => lineMesh = value;
        }

        public Mesh JointMesh
        {
            get => jointMesh;
            set => jointMesh = value;
        }

        public Material LineMaterial
        {
            get => lineMaterial;
            set => lineMaterial = value;
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
            for (var i = 0; i < segmentTransformMatrixCache.Count; i++)
            {
                var lineTransforms = segmentTransformMatrixCache[i];
                var lineJointTransforms = jointsTransformMatrixCache[i];
                if (hasColors)
                {
                    MaterialPropertyBlock props = materialPropertyBlockCache[i];
                    Graphics.DrawMeshInstanced(LineMesh, 0, LineMaterial, lineTransforms, props);

                    if (DrawJoints)
                        Graphics.DrawMeshInstanced(JointMesh, 0, LineMaterial, lineJointTransforms, props);

                    continue;
                }

                Graphics.DrawMeshInstanced(LineMesh, 0, LineMaterial, lineTransforms);
                if (DrawJoints)
                    Graphics.DrawMeshInstanced(JointMesh, 0, LineMaterial, lineJointTransforms);
            }
        }

        /// <summary>
        /// Return the index of the closest line to a given point.
        /// Handy for selecting a line based on a click position.
        /// </summary>
        public int GetClosestLineIndex(Vector3 point)
        {
            int closestLineIndex = -1;
            float closestDistance = float.MaxValue;
            for (int i = 0; i < segmentTransformMatrixCache.Count; i++)
            {
                var lineTransforms = segmentTransformMatrixCache[i];
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
        /// Set specific line color for the line closest to a given point.
        /// </summary>
        public int SetLineColorClosestToPoint(Vector3 point, Color color)
        {
            int closestLineIndex = GetClosestLineIndex(point);
            if (closestLineIndex == -1)
            {
                Debug.LogWarning("No line found");
                return -1;
            }

            SetSpecificLineColorByIndex(closestLineIndex, color);
            return closestLineIndex;
        }

        /// <summary>
        /// Set a specific line color by index of the line.
        /// May be used for 'highlighting' a line, in combination with the ClosestLineToPoint method.
        /// </summary>
        public void SetSpecificLineColorByIndex(int index, Color color)
        {
            if (index >= materialPropertyBlockCache.Count)
            {
                Debug.LogWarning($"Index {index} is out of range");
                return;
            }

            MaterialPropertyBlock props = materialPropertyBlockCache[index];
            props.SetColor("_BaseColor", color);
            materialPropertyBlockCache[index] = props;
        }

        /// <summary>
        /// Set a single line (overwriting any previous lines)
        /// </summary>
        public void SetLine(List<Vector3> linePoints)
        {
            var validLine = ValidateLine(linePoints);
            if (!validLine) return;

            Lines = new List<List<Vector3>> { linePoints };
            SetLines(Lines);
        }

        /// <summary>
        /// Set the current list of lines (overwriting any previous list)
        /// </summary>
        public void SetLines(List<List<Vector3>> lines)
        {
            foreach (List<Vector3> line in lines)
            {
                var validLine = ValidateLine(line);
                if (!validLine) return;
            }

            this.Lines = lines;
            GenerateTransformMatrixCache();
        }

        /// <summary>
        /// Append single line to the current list of lines
        /// </summary>
        public void AppendLine(List<Vector3> linePoints)
        {
            var validLine = ValidateLine(linePoints);
            if (!validLine) return;

            if (Lines == null)
                Lines = new List<List<Vector3>>();

            var startIndex = Lines.Count;
            Lines.Add(linePoints);
            GenerateTransformMatrixCache(startIndex);
        }

        /// <summary>
        /// Append multiple lines to the current list of lines
        /// </summary>
        public void AppendLines(List<List<Vector3>> lines)
        {
            foreach (List<Vector3> line in lines)
            {
                var validLine = ValidateLine(line);
                if (!validLine) return;
            }

            if (this.Lines == null)
                this.Lines = new List<List<Vector3>>();

            var startIndex = Lines.Count;
            this.Lines.AddRange(lines);
            GenerateTransformMatrixCache(startIndex);
        }

        /// <summary>
        /// Give every line a random color
        /// </summary>
        [ContextMenu("Randomize line colors")]
        public void SetRandomLineColors()
        {
            Color[] colors = new Color[Lines.Count];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Random.ColorHSV();
            }

            SetSpecificLineMaterialColors(colors);
        }

        /// <summary>
        /// Set specific colors for each line
        /// </summary>
        /// <param name="colors">Array of colors matching the list of lines length</param>
        public void SetSpecificLineMaterialColors(Color[] colors)
        {
            if (colors.Length != Lines.Count)
            {
                Debug.LogWarning($"The amount of colors ({colors.Length}) should match the amount of lines {Lines.Count}");
                return;
            }

            materialPropertyBlockCache = new List<MaterialPropertyBlock>();
            foreach (Color color in colors)
            {
                MaterialPropertyBlock props = new();
                props.SetColor("_BaseColor", color);
                materialPropertyBlockCache.Add(props);
            }

            hasColors = true;
        }

        public bool ValidateLine(List<Vector3> line)
        {
            if (line.Count < 2)
            {
                Debug.LogWarning("A line should have at least 2 points");
                return false;
            }

            return true;
        }

        public void ClearLines()
        {
            Lines.Clear();
            ClearColors();

            segmentTransformMatrixCache = new List<List<Matrix4x4>>();
            jointsTransformMatrixCache = new List<List<Matrix4x4>>();
            cacheReady = false;
        }

        public void ClearColors()
        {
            if (hasColors)
                materialPropertyBlockCache.Clear();
            hasColors = false;
        }

        private void GenerateTransformMatrixCache(int lineStartIndex = -1)
        {
            if (Lines == null || Lines.Count < 1) return;

            var jointCount = Lines.SelectMany(list => list).Count(); //each point should have a joint
            var segmentCount = jointCount - Lines.Count; // each line one more joint than segments, so subtracting the lineCount will result in the total number of segments

            var jointBatchCount = (jointCount / 1023) + 1; //x batches of 1023 + 1 for the remainder
            var segmentBatchCount = (segmentCount / 1023) + 1; //x batches of 1023 + 1 for the remainder

            if (lineStartIndex < 0) //reset cache completely
            {
                segmentTransformMatrixCache = new List<List<Matrix4x4>>(jointBatchCount);
                jointsTransformMatrixCache = new List<List<Matrix4x4>>(segmentBatchCount);
                lineStartIndex = 0;
            }

            jointsTransformMatrixCache.Capacity = jointBatchCount;
            segmentTransformMatrixCache.Capacity = segmentBatchCount;

            var jointIndices = GetJointMatrixIndices(lineStartIndex); //each point in the line is a joint
            var segmentIndices = GetSegmentMatrixIndices(lineStartIndex);

            for (var i = lineStartIndex; i < Lines.Count; i++)
            {
                var line = Lines[i];
                for (int j = 0; j < line.Count - 1; j++)
                {
                    var currentPoint = line[j];
                    var nextPoint = line[j + 1];

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
                    AppendMatrixToBatches(segmentTransformMatrixCache, ref segmentIndices.batchIndex, ref segmentIndices.matrixIndex, transformMatrix, " segments");

                    // Create the joint using a sphere aligned with the cylinder (with matching faces for smooth transition between the two)
                    var jointScale = new Vector3(LineDiameter, LineDiameter, LineDiameter);
                    Matrix4x4 jointTransformMatrix = Matrix4x4.TRS(currentPoint, rotation, jointScale);
                    AppendMatrixToBatches(jointsTransformMatrixCache, ref jointIndices.batchIndex, ref jointIndices.matrixIndex, jointTransformMatrix, " joints ");
                    
                    //Add the last joint to cap the line end
                    if (j == line.Count - 2)
                    {
                        jointTransformMatrix = Matrix4x4.TRS(nextPoint, rotation, jointScale);
                        AppendMatrixToBatches(jointsTransformMatrixCache, ref jointIndices.batchIndex, ref jointIndices.matrixIndex, jointTransformMatrix, " joints ");
                    }
                }
            }

            cacheReady = true;
        }
        
        private void AppendMatrixToBatches(List<List<Matrix4x4>> batchList, ref int arrayIndex, ref int matrixIndex, Matrix4x4 valueToAdd, string test)
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

        private (int batchIndex, int matrixIndex) GetJointMatrixIndices(int lineStartIndex)
        {
            if (lineStartIndex < 0)
                return (-1, -1);

            // Iterate over the Lines to find the total number of Vector3s before the startIndex
            int totalJointsBeforeStartIndex = Lines.Take(lineStartIndex).Sum(list => list.Count);

            return (totalJointsBeforeStartIndex / 1023, totalJointsBeforeStartIndex % 1023);
        }

        private (int batchIndex, int matrixIndex) GetSegmentMatrixIndices(int lineStartIndex)
        {
            if (lineStartIndex < 0)
                return (-1, -1);

            // Iterate over the Lines to find the total number of Vector3s before the startIndex
            int totalJointsBeforeStartIndex = Lines.Take(lineStartIndex).Sum(list => list.Count) - lineStartIndex; // each line has one more joint than segments, so subtracting the startIndex will result in the total number of segments
            return (totalJointsBeforeStartIndex / 1023, totalJointsBeforeStartIndex % 1023);
        }
    }
}

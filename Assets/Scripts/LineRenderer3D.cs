using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Netherlands3D.Coordinates;
using UnityEngine;
using UnityEngine.Rendering;

namespace Netherlands3D.Twin
{
    public class LineRenderer3D : MonoBehaviour
    {
        [Header("References")] [Tooltip("The mesh to use for the line segments")] [SerializeField]
        private Mesh lineMesh;

        [Tooltip("The mesh to use for the joints to get smooth corners")] [SerializeField]
        private Mesh jointMesh;

        [SerializeField] private Material lineMaterial;
        [SerializeField] private Material lineSelectionMaterial;

        [Header("Settings")] [SerializeField] private bool drawJoints = true;

        [Tooltip("Force all point Y positions to 0")] [SerializeField]
        private bool flattenY = false;

        [Tooltip("Offset the Y position of the line")] [SerializeField]
        private float offsetY = 0.0f;

        [SerializeField] private float lineDiameter = 0.2f;

        public List<List<Coordinate>> Lines { get; private set; }

        private List<List<Matrix4x4>> segmentTransformMatrixCache = new List<List<Matrix4x4>>();

        private List<List<Matrix4x4>> jointsTransformMatrixCache = new List<List<Matrix4x4>>();

        private List<MaterialPropertyBlock> materialPropertyBlockCache = new List<MaterialPropertyBlock>();
        private List<Vector4[]> segmentColorCache = new List<Vector4[]>();

        private MaterialPropertyBlock selectedMaterialPropertyBlock;
        private List<Matrix4x4> selectedLineTransforms = new List<Matrix4x4>();
        private List<Matrix4x4> selectedJointTransforms = new List<Matrix4x4>();
        private List<Vector4> selectedLineColorCache = new List<Vector4>();

        private Camera projectionCamera;
        private LayerMask layerMask = -1;
        private bool cacheReady = false;

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
            for (var i = 0; i < segmentTransformMatrixCache.Count; i++)
            {
                var lineTransforms = segmentTransformMatrixCache[i];
                Graphics.DrawMeshInstanced(LineMesh, 0, LineMaterial, lineTransforms, materialPropertyBlockCache[i], ShadowCastingMode.Off, false, layerMask, projectionCamera);
            }

            if (DrawJoints)
            {
                for (var i = 0; i < jointsTransformMatrixCache.Count; i++)
                {
                    var lineJointTransforms = jointsTransformMatrixCache[i];
                    Graphics.DrawMeshInstanced(JointMesh, 0, LineMaterial, lineJointTransforms, materialPropertyBlockCache[i], ShadowCastingMode.Off, false, layerMask, projectionCamera);
                }
            }
            if (selectedLineIndex >= 0)
            {               
                Graphics.DrawMeshInstanced(LineMesh, 0, lineSelectionMaterial, selectedLineTransforms, selectedMaterialPropertyBlock, ShadowCastingMode.Off, false, layerMask, projectionCamera);
                if(DrawJoints)
                    Graphics.DrawMeshInstanced(JointMesh, 0, lineSelectionMaterial, selectedJointTransforms, selectedMaterialPropertyBlock, ShadowCastingMode.Off, false, layerMask, projectionCamera);
            }
        }

        private Matrix4x4 GetSegmentMatrixFromPosition(Vector3 position)
        {
            var indexPosition = GetClosestLineIndex(position);
            return segmentTransformMatrixCache[indexPosition.batchindex][indexPosition.lineIndex];
        }

        private Matrix4x4 GetJointMatrixFromPosition(Vector3 position)
        {
            var indexPosition = GetClosestJointIndex(position);
            return jointsTransformMatrixCache[indexPosition.batchindex][indexPosition.jointIndex];
        }

        /// <summary>
        /// Return the batch index and line index as a tuple of the closest point to a given point.
        /// Handy for selecting a line based on a click position.
        /// </summary>
        public (int batchindex, int lineIndex) GetClosestLineIndex(Vector3 point)
        {
            int closestBatchIndex = -1;
            int closestLineIndex = -1;
            float closestDistance = float.MaxValue;
            for (int i = 0; i < segmentTransformMatrixCache.Count; i++)
            {
                var lineTransforms = segmentTransformMatrixCache[i];
                for (int j = 0; j < lineTransforms.Count; j++)
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

            //GameObject testPoint = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //testPoint.transform.position = (Vector3)segmentTransformMatrixCache[closestBatchIndex][closestLineIndex].GetColumn(3) + Vector3.up * 15;

            return (closestBatchIndex, closestLineIndex);
        }

        /// <summary>
        /// Return the batch index and line index as a tuple of the closest point to a given point.
        /// Handy for selecting a line based on a click position.
        /// </summary>
        public (int batchindex, int jointIndex) GetClosestJointIndex(Vector3 point)
        {
            int closestBatchIndex = -1;
            int closestJointIndex = -1;
            float closestDistance = float.MaxValue;
            for (int i = 0; i < jointsTransformMatrixCache.Count; i++)
            {
                var jointTransforms = jointsTransformMatrixCache[i];
                for (int j = 0; j < jointTransforms.Count; j++)
                {
                    var linePoint = jointTransforms[j].GetColumn(3);
                    var distance = Vector3.Distance(linePoint, point);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestBatchIndex = i;
                        closestJointIndex = j;
                    }
                }
            }

            //GameObject testPoint = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //testPoint.transform.position = (Vector3)segmentTransformMatrixCache[closestBatchIndex][closestLineIndex].GetColumn(3) + Vector3.up * 15;

            return (closestBatchIndex, closestJointIndex);
        }

        private void UpdateBuffers()
        {
            while (Lines.Count > materialPropertyBlockCache.Count)
            {
                MaterialPropertyBlock props = new MaterialPropertyBlock();
                Vector4[] colorCache = new Vector4[1023];
                Color defaultColor = LineMaterial.GetColor("_Color");
                for (int i = 0; i < Lines.Count; i++)
                {
                    segmentColorCache.Add(colorCache);
                    for (int j = 0; j < colorCache.Length; j++)
                        colorCache[j] = defaultColor;
                }
                props.SetVectorArray("_SegmentColors", colorCache);
                materialPropertyBlockCache.Add(props);
            }
        }

        public void SetDefaultColors()
        {
            selectedLineIndex = -1;
            Color defaultColor = LineMaterial.GetColor("_Color");
            for (int batchIndex = 0; batchIndex < Lines.Count; batchIndex++)
            {
                Vector4[] colors = segmentColorCache[batchIndex];
                for (int segmentIndex = 0; segmentIndex < colors.Length; segmentIndex++)
                {
                    colors[segmentIndex] = defaultColor;
                }
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
                Debug.LogWarning($"Index {batchIndex} is out of range");
                return;
            }
            UpdateBuffers();
            segmentColorCache[batchIndex][segmentIndex] = color;
            materialPropertyBlockCache[batchIndex].SetVectorArray("_SegmentColors", segmentColorCache[batchIndex]);
        }

        public void SetSpecificLineHeightByIndex(int batchIndex, int segmentIndex, float height)
        {
            Vector3 position = segmentTransformMatrixCache[batchIndex][segmentIndex].GetColumn(3); // Get the current position as a Vector3
            position.y = height;
            segmentTransformMatrixCache[batchIndex][segmentIndex].SetColumn(3, new Vector4(position.x, position.y, position.z, 1.0f));
        }

        /// <summary>
        /// Set specific line color for the line closest to a given point.
        /// </summary>
        public void SetLineColorClosestToPoint(Vector3 point, Color color)
        {
            var indexPosition = GetClosestLineIndex(point);
            if (indexPosition.batchindex == -1 || indexPosition.lineIndex == -1)
            {
                Debug.LogWarning("No line found");
                return;
            }
            SetSpecificLineColorByIndex(indexPosition.batchindex, indexPosition.lineIndex, color);
        }

        private int selectedLineIndex = -1;
        public void SetLineColorFromPoint(Vector3 point, Color color)
        {
            SetDefaultColors();
            float closest = float.MaxValue;
            int lineStartIndex = -1;
            for (int i = 0; i < Lines.Count; i++)
            {
                for (int j = 0; j < Lines[i].Count; j++)
                {
                    float dist = Vector3.Distance(point, Lines[i][j].ToUnity());
                    if (dist < closest)
                    {
                        closest = dist;
                        lineStartIndex = i;
                    }
                }
            }

            List<Coordinate> tempLine = Lines[lineStartIndex];
            RemoveLine(Lines[lineStartIndex]);
            lineStartIndex = AppendLine(tempLine);

            if (lineStartIndex < 0) return;

            //if(selectedMaterialPropertyBlock == null)
            //    selectedMaterialPropertyBlock = new MaterialPropertyBlock();

            //selectedLineIndex = lineStartIndex;
            //var positions = GetSegmentMatrixIndices(selectedLineIndex);
            //int count = Lines[selectedLineIndex].Count;
            //selectedLineTransforms.Clear();
            //selectedJointTransforms.Clear();
            //selectedLineColorCache.Clear();

            //for (int i = 0; i < count; i++)
            //{
            //    //Vector3 vertex = Lines[selectedLineIndex][i].ToUnity();
            //    ////if(i < count - 1)
            //    //    selectedLineTransforms.Add(GetSegmentMatrixFromPosition(vertex));
            //    //selectedJointTransforms.Add(GetJointMatrixFromPosition(vertex));
            //    selectedLineTransforms.Add(segmentTransformMatrixCache[positions.batchIndex][positions.matrixIndex + i]);
            //    selectedJointTransforms.Add(jointsTransformMatrixCache[positions.batchIndex][positions.matrixIndex + i]);
            //    selectedLineColorCache.Add(color);
            //}
            //selectedMaterialPropertyBlock.SetVectorArray("_SegmentColors", selectedLineColorCache);

            ////todo take in account when overflowing the buffer
            var segmentIndices = GetSegmentMatrixIndices(lineStartIndex);
            for (int i = 0; i < Lines[lineStartIndex].Count - 1; i++) //-1 we dont want to color the last segment
            {
                //SetSpecificLineColorByIndex(segmentIndices.batchIndex, segmentIndices.matrixIndex + i, color);
                segmentColorCache[segmentIndices.batchIndex][segmentIndices.matrixIndex + i] = color;
            }
            materialPropertyBlockCache[segmentIndices.batchIndex].SetVectorArray("_SegmentColors", segmentColorCache[segmentIndices.batchIndex]);

        }

        /// <summary>
        /// Set a single line (overwriting any previous lines)
        /// </summary>
        public void SetLine(List<Coordinate> linePoints)
        {
            var validLine = ValidateLine(linePoints);
            if (!validLine) return;

            Lines = new List<List<Coordinate>> { linePoints };
            SetLines(Lines);
        }

        /// <summary>
        /// Set the current list of lines (overwriting any previous list)
        /// </summary>
        public void SetLines(List<List<Coordinate>> lines)
        {
            foreach (List<Coordinate> line in lines)
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
        public int AppendLine(List<Coordinate> linePoints)
        {
            var validLine = ValidateLine(linePoints);
            if (!validLine) return -1;

            if (Lines == null)
                Lines = new List<List<Coordinate>>();

            var startIndex = Lines.Count;
            Lines.Add(linePoints);
            GenerateTransformMatrixCache(startIndex);

            return startIndex;
        }

        //public void  InsertLine(List<Coordinate> linePoints)
        //{
        //    var validLine = ValidateLine(linePoints);
        //    if (!validLine) return;

        //    if (Lines == null)
        //        Lines = new List<List<Coordinate>>(); 
            
        //    Lines.Insert(0, linePoints);
        //    GenerateTransformMatrixCache(0);
        //}

        /// <summary>
        /// Remove a line using start and length
        /// </summary>
        public void RemoveLine(List<Coordinate> linePoints)
        {
            Lines.Remove(linePoints);

            GenerateTransformMatrixCache(-1);
        }

        public void RemoveLines(List<List<Coordinate>> lines)
        {
            foreach (List<Coordinate> line in lines)
                Lines.Remove(line);
                
            GenerateTransformMatrixCache(-1);
        }


        /// <summary>
        /// Append multiple lines to the current list of lines
        /// </summary>
        public int AppendLines(List<List<Coordinate>> lines)
        {
            foreach (List<Coordinate> line in lines)
            {
                var validLine = ValidateLine(line);
                if (!validLine) return -1;
            }

            if (this.Lines == null)
                this.Lines = new List<List<Coordinate>>();

            var startIndex = Lines.Count;
            this.Lines.AddRange(lines);
            GenerateTransformMatrixCache(startIndex);

            return startIndex;
        }

        public bool ValidateLine(List<Coordinate> line)
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
            materialPropertyBlockCache.Clear();
            segmentColorCache.Clear();
            segmentTransformMatrixCache = new List<List<Matrix4x4>>();
            jointsTransformMatrixCache = new List<List<Matrix4x4>>();
            cacheReady = false;
        }

        private void RemoveTransformMatrixCacheForLine(int lineStartIndex, int length)
        {
            var jointIndices = GetJointMatrixIndices(lineStartIndex); 
            var segmentIndices = GetSegmentMatrixIndices(lineStartIndex);       

            //Use joinIndices.batchIndex to determine the first batch where this line starts. Remove the range. If the range exceeds the batchs, make sure to go through all batches untill line is removed
            for (int i = jointIndices.batchIndex; i < jointsTransformMatrixCache.Count; i++)
            {
                if (length <= 0) break;

                if (i == jointIndices.batchIndex)
                {
                    jointsTransformMatrixCache[i].RemoveRange(jointIndices.matrixIndex, Mathf.Min(jointsTransformMatrixCache[i].Count - jointIndices.matrixIndex, length));
                    length -= jointsTransformMatrixCache[i].Count - jointIndices.matrixIndex;
                }
                else
                {
                    jointsTransformMatrixCache[i].RemoveRange(0, Mathf.Min(jointsTransformMatrixCache[i].Count, length));
                    length -= jointsTransformMatrixCache[i].Count;
                }
            }
        }

        public void GenerateTransformMatrixCache(int lineStartIndex = -1)
        {
            if (Lines == null || Lines.Count < 1) return;

            var jointCount = Lines.SelectMany(list => list).Count(); //each point should have a joint
            var segmentCount = jointCount - Lines.Count; // each line one more joint than segments, so subtracting the lineCount will result in the total number of segments

            var jointBatchCount = (jointCount / 1023) + 1; //x batches of 1023 + 1 for the remainder
            var segmentBatchCount = (segmentCount / 1023) + 1; //x batches of 1023 + 1 for the remainder

            if (lineStartIndex < 0) //reset cache completely
            {
                jointsTransformMatrixCache = new List<List<Matrix4x4>>(jointBatchCount);
                segmentTransformMatrixCache = new List<List<Matrix4x4>>(segmentBatchCount);
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
                    AppendMatrixToBatches(segmentTransformMatrixCache, ref segmentIndices.batchIndex, ref segmentIndices.matrixIndex, transformMatrix);

                    // Create the joint using a sphere aligned with the cylinder (with matching faces for smooth transition between the two)
                    var jointScale = new Vector3(LineDiameter, LineDiameter, LineDiameter);
                    Matrix4x4 jointTransformMatrix = Matrix4x4.TRS(currentPoint, rotation, jointScale);
                    AppendMatrixToBatches(jointsTransformMatrixCache, ref jointIndices.batchIndex, ref jointIndices.matrixIndex, jointTransformMatrix);

                    //Add the last joint to cap the line end
                    if (j == line.Count - 2)
                    {
                        jointTransformMatrix = Matrix4x4.TRS(nextPoint, rotation, jointScale);
                        AppendMatrixToBatches(jointsTransformMatrixCache, ref jointIndices.batchIndex, ref jointIndices.matrixIndex, jointTransformMatrix);
                    }
                }
            }

            UpdateBuffers();

            cacheReady = true;
        }

        private void AppendMatrixToBatches(List<List<Matrix4x4>> batchList, ref int arrayIndex, ref int matrixIndex, Matrix4x4 valueToAdd)
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
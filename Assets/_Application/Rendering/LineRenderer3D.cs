using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Coordinates;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons;
using UnityEngine;
using UnityEngine.Rendering;

namespace Netherlands3D.Twin.Rendering
{
    public class LineRenderer3D : MonoBehaviour
    {
        [Header("References")] [Tooltip("The mesh to use for the line segments")] [SerializeField]
        private Mesh lineMesh;

        [Tooltip("The mesh to use for the joints to get smooth corners")] [SerializeField]
        private Mesh jointMesh;

        [SerializeField] private Material lineMaterial;
        [SerializeField] private Material jointMaterial;
        [SerializeField] private Material lineSelectionMaterial;
        [SerializeField] private Material jointSelectionMaterial;

        [Header("Settings")] [SerializeField] private bool drawJoints = true;

        [Tooltip("Force all point Y positions to 0")] [SerializeField]
        private bool flattenY = false;

        [Tooltip("Offset the Y position of the line")] [SerializeField]
        private float offsetY = 0.0f;

        [SerializeField] private float lineDiameter = 0.2f;

        public List<List<Coordinate>> Lines { get; private set; }

        private List<List<Matrix4x4>> segmentTransformMatrixCache = new List<List<Matrix4x4>>();
        private List<List<Matrix4x4>> jointsTransformMatrixCache = new List<List<Matrix4x4>>();

        private List<MaterialPropertyBlock> segmentPropertyBlockCache = new List<MaterialPropertyBlock>();
        private List<MaterialPropertyBlock> jointPropertyBlockCache = new List<MaterialPropertyBlock>();
        private List<Vector4[]> segmentColorCache = new List<Vector4[]>();
        private List<Vector4[]> jointColorCache = new List<Vector4[]>();

        private MaterialPropertyBlock selectedSegmentMaterialPropertyBlock;
        private MaterialPropertyBlock selectedJointMaterialPropertyBlock;
        private List<Matrix4x4> selectedLineTransforms = new List<Matrix4x4>();
        private List<Matrix4x4> selectedJointTransforms = new List<Matrix4x4>();
        private List<Vector4> selectedLineColorCache = new List<Vector4>();
        private List<Vector4> selectedJointColorCache = new List<Vector4>();
        private int selectedLineIndex = -1;

        private Camera projectionCamera;
        private LayerMask layerMask = -1;
        private bool cacheReady = false;

        //can be used to see if the transformcache is used correctly when selecting
        private static bool testLinePointsEanbled = false;
        private static GameObject testObjectPoint, testObjectPoint2 = null;
        private static List<GameObject> testLinePoints = new List<GameObject>();

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
            projectionCamera = ServiceLocator.GetService<PolygonDecalProjector>().ProjectionCamera;
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
                Graphics.DrawMeshInstanced(LineMesh, 0, LineMaterial, lineTransforms, segmentPropertyBlockCache[i], ShadowCastingMode.Off, false, layerMask, projectionCamera);
            }

            if (DrawJoints)
            {
                for (var i = 0; i < jointsTransformMatrixCache.Count; i++)
                {
                    var lineJointTransforms = jointsTransformMatrixCache[i];
                    Graphics.DrawMeshInstanced(JointMesh, 0, jointMaterial, lineJointTransforms, jointPropertyBlockCache[i], ShadowCastingMode.Off, false, layerMask, projectionCamera);
                }
            }
            if (selectedLineIndex >= 0)
            {
                Graphics.DrawMeshInstanced(LineMesh, 0, lineSelectionMaterial, selectedLineTransforms, selectedSegmentMaterialPropertyBlock, ShadowCastingMode.Off, false, layerMask, projectionCamera);
                if (DrawJoints)
                    Graphics.DrawMeshInstanced(JointMesh, 0, jointSelectionMaterial, selectedJointTransforms, selectedJointMaterialPropertyBlock, ShadowCastingMode.Off, false, layerMask, projectionCamera);
            }
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
                    Vector3 linePoint = lineTransforms[j].GetColumn(3);
                    float distance = Vector3.SqrMagnitude(point - linePoint);
                    if (distance < closestDistance * closestDistance)
                    {
                        closestDistance = Mathf.Sqrt(distance);
                        closestBatchIndex = i;
                        closestLineIndex = j;
                    }
                }
            }
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
                    Vector3 linePoint = jointTransforms[j].GetColumn(3);
                    float distance = Vector3.SqrMagnitude(point - linePoint);
                    if (distance < closestDistance * closestDistance)
                    {
                        closestDistance = Mathf.Sqrt(distance);
                        closestBatchIndex = i;
                        closestJointIndex = j;
                    }
                }
            }
            return (closestBatchIndex, closestJointIndex);
        }

        private void UpdateBuffers()
        {
            while (segmentTransformMatrixCache.Count > segmentPropertyBlockCache.Count)
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
            while (jointsTransformMatrixCache.Count > jointPropertyBlockCache.Count)
            {
                MaterialPropertyBlock props = new MaterialPropertyBlock();
                Vector4[] colorCache = new Vector4[1023];
                Color defaultColor = LineMaterial.color;
                for (int j = 0; j < colorCache.Length; j++)
                    colorCache[j] = defaultColor;
                jointColorCache.Add(colorCache);
                props.SetVectorArray("_SegmentColors", colorCache);
                jointPropertyBlockCache.Add(props);
            }
        }

        public void SetDefaultColors()
        {
            selectedLineIndex = -1;
            Color defaultColor = LineMaterial.color;
            for (int batchIndex = 0; batchIndex < segmentTransformMatrixCache.Count; batchIndex++)
            {
                Vector4[] colors = segmentColorCache[batchIndex];
                Vector4[] colors2 = jointColorCache[batchIndex];
                for (int segmentIndex = 0; segmentIndex < colors.Length; segmentIndex++)
                {
                    colors[segmentIndex] = defaultColor;
                }
                for (int segmentIndex = 0; segmentIndex < colors2.Length; segmentIndex++)
                {                    
                    colors2[segmentIndex] = defaultColor;
                }
                segmentColorCache[batchIndex] = colors;
                jointColorCache[batchIndex] = colors2;
                MaterialPropertyBlock props = segmentPropertyBlockCache[batchIndex];
                props.SetVectorArray("_SegmentColors", colors);
                segmentPropertyBlockCache[batchIndex] = props;
                MaterialPropertyBlock props2 = jointPropertyBlockCache[batchIndex];
                props2.SetVectorArray("_SegmentColors", colors2);
                jointPropertyBlockCache[batchIndex] = props2;
            }
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
            //calculate the centroid of the targeted line
            Vector3 selectionCentroid = Vector3.zero;
            for (int i = 0; i < points.Length; i++)
                selectionCentroid += points[i];
            selectionCentroid /= points.Length;

            if (testLinePointsEanbled)
            {
                if (testObjectPoint != null)
                    Destroy(testObjectPoint);
                if (testObjectPoint2 != null)
                    Destroy(testObjectPoint2);
                if (testLinePoints.Count > 0)
                {
                    for (int i = testLinePoints.Count - 1; i >= 0; i--)
                        Destroy(testLinePoints[i]);
                    testLinePoints.Clear();
                }
                testObjectPoint = GameObject.CreatePrimitive(PrimitiveType.Cube);
                testObjectPoint.transform.position = selectionCentroid + Vector3.up * 35;
                testObjectPoint.GetComponent<MeshRenderer>().material.color = Color.green;
            }

            //compare the centroids of other lines to be sure the line is matching as the closest selected line
            //todo, maybe cache the line centroids for optimisation but now only happens when selecting
            float closest = float.MaxValue;
            int lineStartIndex = -1;
            Vector3 testTarget = Vector3.zero;
            for (int i = 0; i < Lines.Count; i++)
            {
                Vector3 lineCentroid = Vector3.zero;
                for (int j = 0; j < Lines[i].Count; j++)
                    lineCentroid += Lines[i][j].ToUnity();
                lineCentroid /= Lines[i].Count;                
                float dist = Vector3.SqrMagnitude(selectionCentroid - lineCentroid);
                if(dist < closest * closest)
                {
                    closest = Mathf.Sqrt(dist);
                    lineStartIndex = i;
                    testTarget = lineCentroid;
                }
            }

            if (testLinePointsEanbled && lineStartIndex >= 0)
            {
                testObjectPoint2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                testObjectPoint2.transform.position = testTarget + Vector3.up * 30;
                testObjectPoint2.GetComponent<MeshRenderer>().material.color = Color.magenta;
            }

            //move the line to the end of the render batch (keep this here incase the Zwrite value of the shader needs to be replaced)
            //List<Coordinate> tempLine = Lines[lineStartIndex];
            //RemoveLine(Lines[lineStartIndex]);
            //lineStartIndex = AppendLine(tempLine);
           
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
            //var segPositions = GetSegmentMatrixIndices(selectedLineIndex);
            //var jntPositions = GetJointMatrixIndices(selectedLineIndex);
            int count = Lines[selectedLineIndex].Count;
            selectedLineTransforms.Clear();
            selectedJointTransforms.Clear();
            //selectedLineColorCache.Clear();
            //selectedJointColorCache.Clear();
                        
            for (int i = 0; i < count; i++)
            {
                Vector3 vertex = Lines[selectedLineIndex][i].ToUnity();
                if (i < count - 1)
                {
                    Matrix4x4 segMatrix = GetSegmentMatrixFromPosition(vertex);
                    selectedLineTransforms.Add(segMatrix);
                    //selectedLineColorCache.Add(color);
                    if (testLinePointsEanbled)
                    {
                        GameObject testObjectPoint3 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        testObjectPoint3.transform.position = (Vector3)segMatrix.GetColumn(3) + Vector3.up * 25;
                        testObjectPoint3.GetComponent<MeshRenderer>().material.color = Color.red;
                        testLinePoints.Add(testObjectPoint3);
                    }
                }
                Matrix4x4 jntMatrix = GetJointMatrixFromPosition(vertex);
                selectedJointTransforms.Add(jntMatrix);
                //selectedJointColorCache.Add(color);
            }
            selectedSegmentMaterialPropertyBlock.SetVectorArray("_SegmentColors", selectedLineColorCache);
            selectedJointMaterialPropertyBlock.SetVectorArray("_SegmentColors", selectedJointColorCache);

            if (Lines[lineStartIndex].Count > 1023)
                Debug.LogError("the selected line feature is over 1023 vertices, a fix is needed for buffer overflow");

            //todo take in account when overflowing the buffer, but probably not needed because no selected line is 1023 vertices
            var segmentIndices = GetSegmentMatrixIndices(lineStartIndex);
            for (int i = 0; i < Lines[lineStartIndex].Count - 1; i++) //-1 we dont want to color the last segment
            {
                segmentColorCache[segmentIndices.batchIndex][segmentIndices.matrixIndex + i] = color;
            }
            var jointIndices = GetJointMatrixIndices(lineStartIndex);
            for (int i = 0; i < Lines[lineStartIndex].Count; i++) //we do want to color the last segment
            {
                jointColorCache[jointIndices.batchIndex][jointIndices.matrixIndex + i] = color;
            }            
            segmentPropertyBlockCache[segmentIndices.batchIndex].SetVectorArray("_SegmentColors", segmentColorCache[segmentIndices.batchIndex]);
            jointPropertyBlockCache[jointIndices.batchIndex].SetVectorArray("_SegmentColors", jointColorCache[jointIndices.batchIndex]);

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
            segmentPropertyBlockCache.Clear();
            segmentColorCache.Clear();
            jointPropertyBlockCache.Clear();
            jointColorCache.Clear();
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

        /// <summary>
        /// this may work incorrectly as some lines have overlapping vertices and need to be checked by centroid
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private Matrix4x4 GetSegmentMatrixFromPosition(Vector3 position)
        {
            var indexPosition = GetClosestLineIndex(position);
            return segmentTransformMatrixCache[indexPosition.batchindex][indexPosition.lineIndex];
        }

        /// <summary>
        /// this may work incorrectly as some joints have overlapping vertices and need to be checked by centroid
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private Matrix4x4 GetJointMatrixFromPosition(Vector3 position)
        {
            var indexPosition = GetClosestJointIndex(position);
            return jointsTransformMatrixCache[indexPosition.batchindex][indexPosition.jointIndex];
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
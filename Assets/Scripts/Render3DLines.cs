using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Netherlands3D.Twin
{
    public class LineRenderer3D : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The mesh to use for the line segments")]
        [SerializeField] private Mesh lineMesh;
        [Tooltip("The mesh to use for the joints to get smooth corners")]
        [SerializeField] private Mesh jointMesh;
        [SerializeField] private Material lineMaterial;
        
        [Header("Settings")]
        [SerializeField] private bool drawJoints = true;
        [Tooltip("Force all point Y positions to 0")] 
        [SerializeField] private bool flattenY = false;
        [Tooltip("Offset the Y position of the line")]
        [SerializeField] private float offsetY = 0.0f;

        private List<List<Vector3>> lines;
        private List<List<Matrix4x4>> lineTransformMatrixCache; 
        private List<List<Matrix4x4>> jointsTransformMatrixCache; 
        private List<MaterialPropertyBlock> materialPropertyBlockCache;
        private bool cacheReady = false;
        private float lineDiameter = 0.2f;
        private bool hasColors = false;

        public Mesh LineMesh { get => lineMesh; set => lineMesh = value; }
        public Mesh JointMesh { get => jointMesh; set => jointMesh = value; }
        public Material LineMaterial { get => lineMaterial; set => lineMaterial = value; }
        public bool DrawJoints { get => drawJoints; set => drawJoints = value; }
        public bool FlattenY { get => flattenY; set{
            flattenY = value;
            GenerateTransformMatrixCache();  
        }}
        public float OffsetY { get => offsetY; set{
            offsetY = value;
            GenerateTransformMatrixCache();  
        }}
        public float LineDiameter { get => lineDiameter; set => lineDiameter = value; }

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
            //Seperate line draws so we can use different colors for each line
            for (int i = 0; i < lineTransformMatrixCache.Count; i++)
            {
                var lineTransforms = lineTransformMatrixCache[i];
                var lineJointTransforms = jointsTransformMatrixCache[i];
                if(hasColors)
                {
                    MaterialPropertyBlock props = materialPropertyBlockCache[i];
                    Graphics.DrawMeshInstanced(LineMesh, 0, LineMaterial, lineTransforms, props);
                    
                    if(DrawJoints)
                        Graphics.DrawMeshInstanced(JointMesh, 0, LineMaterial, lineJointTransforms, props);

                    return;
                }

                Graphics.DrawMeshInstanced(LineMesh, 0, LineMaterial, lineTransforms);
                if(DrawJoints)
                    Graphics.DrawMeshInstanced(JointMesh, 0, LineMaterial, lineJointTransforms);
            }
        }

        public void SetLine(List<Vector3> linePoints)
        {
            var validLine = ValidateLine(linePoints);
            if(!validLine) return;

            lines = new List<List<Vector3>> { linePoints };
            SetLines(lines);
        }

        public void SetLines(List<List<Vector3>> lines)
        {
            foreach(List<Vector3> line in lines)
            {
                var validLine = ValidateLine(line);
                if(!validLine) return;
            }

            this.lines = lines;
            GenerateTransformMatrixCache();
        }

        [ContextMenu("Randomize line colors")]
        public void SetRandomLineColors()
        {
            Color[] colors = new Color[lines.Count];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Random.ColorHSV();
            }
            SetSpecificLineMaterialColors(colors);
        }

        public void SetSpecificLineMaterialColors(Color[] colors)
        {
            if(colors.Length != lines.Count){
                Debug.LogWarning($"The amount of colors ({colors.Length}) should match the amount of lines {lines.Count}");
                return;
            }

            materialPropertyBlockCache = new List<MaterialPropertyBlock>();
            foreach (Color color in colors)
            {
                MaterialPropertyBlock props = new();
                props.SetColor("_Color", color);
                materialPropertyBlockCache.Add(props);
            }

            hasColors = true;
        }

        public void ClearColors()
        {
            hasColors = false;
            materialPropertyBlockCache.Clear();
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
            lines.Clear();
            ClearColors();

            lineTransformMatrixCache.Clear();
            jointsTransformMatrixCache.Clear();
            cacheReady = false;
        }

        private void GenerateTransformMatrixCache()
        {
            if(lines == null || lines.Count < 1) return;

            lineTransformMatrixCache = new List<List<Matrix4x4>>(); // Updated to nested List<Matrix4x4>
            jointsTransformMatrixCache = new List<List<Matrix4x4>>();

            foreach (List<Vector3> line in lines)
            {
                List<Matrix4x4> lineTransforms = new();
                List<Matrix4x4> jointTransforms = new();
                for (int i = 0; i < line.Count - 1; i++)
                {
                    var currentPoint = line[i];
                    var nextPoint = line[i + 1];
                    
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
                    lineTransforms.Add(transformMatrix);

                    // Create the joint using a sphere aligned with the cylinder (with matching faces for smooth transition between the two)
                    var jointScale = new Vector3(LineDiameter, LineDiameter, LineDiameter);
                    Matrix4x4 jointTransformMatrix = Matrix4x4.TRS(currentPoint, rotation, jointScale);
                    jointTransforms.Add(jointTransformMatrix);

                    //Add the last joint to cap the line end
                    if(i == line.Count - 2)
                    {
                        jointTransformMatrix = Matrix4x4.TRS(nextPoint, rotation, jointScale);
                        jointTransforms.Add(jointTransformMatrix);
                    }
                }

                lineTransformMatrixCache.Add(lineTransforms);
                jointsTransformMatrixCache.Add(jointTransforms);
            }

            cacheReady = true;
        }
    }
}

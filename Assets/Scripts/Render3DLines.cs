using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Netherlands3D.Twin
{
    public class LineRenderer3D : MonoBehaviour
    {
        [Tooltip("The mesh to use for the line segments")]
        public Mesh cylinderMesh;
        [Tooltip("The mesh to use for the joints to get smooth corners")]
        public Mesh jointMesh;
        public bool DrawSphereJoints = true;
        public Material lineMaterial;

        private List<List<Vector3>> linesPoints;
        private List<List<Matrix4x4>> lineTransformMatrixCache; 
        private List<List<Matrix4x4>> jointsTransformMatrixCache; 
        private List<MaterialPropertyBlock> materialPropertyBlockCache;
        private bool cacheReady = false;
        private float lineDiameter = 0.2f;

        public void SetLine(List<Vector3> linePoints)
        {
            var validLine = ValidateLine(linePoints);
            if(!validLine) return;

            linesPoints = new List<List<Vector3>> { linePoints };
            SetLines(linesPoints);
        }

        public void SetLines(List<List<Vector3>> linesLists)
        {
            foreach(List<Vector3> line in linesLists)
            {
                var validLine = ValidateLine(line);
                if(!validLine) return;
            }

            linesPoints = linesLists;
            GenerateTransformMatrixCache();
            DrawLines();
        }

        [ContextMenu("Randomize line colors")]
        public void SetRandomLineColors()
        {
            materialPropertyBlockCache = new List<MaterialPropertyBlock>();
            foreach (List<Vector3> line in linesPoints)
            {
                MaterialPropertyBlock props = new();
                props.SetColor("_Color", Random.ColorHSV());
                materialPropertyBlockCache.Add(props);
            }
        }

        public void SetSpecificLineMaterialColors(Color[] colors)
        {
            if(colors.Length != linesPoints.Count-1){
                Debug.LogWarning($"The amount of colors ({colors.Length}) should match the amount of lines {linesPoints.Count-1}");
                return;
            }

            materialPropertyBlockCache = new List<MaterialPropertyBlock>();
            foreach (Color color in colors)
            {
                MaterialPropertyBlock props = new();
                props.SetColor("_Color", color);
                materialPropertyBlockCache.Add(props);
            }
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

        public void SetLineDiameter(float diameter)
        {
            lineDiameter = diameter;
        }

        public void ClearLines()
        {
            linesPoints.Clear();
            lineTransformMatrixCache.Clear();
            jointsTransformMatrixCache.Clear();
            cacheReady = false;
        }

        private void GenerateTransformMatrixCache()
        {
            lineTransformMatrixCache = new List<List<Matrix4x4>>(); // Updated to nested List<Matrix4x4>
            jointsTransformMatrixCache = new List<List<Matrix4x4>>();

            foreach (List<Vector3> line in linesPoints)
            {
                List<Matrix4x4> lineTransforms = new List<Matrix4x4>();
                List<Matrix4x4> jointTransforms = new List<Matrix4x4>();
                for (int i = 0; i < line.Count - 1; i++)
                {
                    var currentPoint = line[i];
                    var nextPoint = line[i + 1];
                    var direction = nextPoint - currentPoint;
                    float distance = direction.magnitude;

                    direction.Normalize();

                    // Calculate the rotation based on the direction vector
                    var rotation = Quaternion.LookRotation(direction);

                    // Calculate the scale based on the distance
                    var scale = new Vector3(lineDiameter, lineDiameter, distance);

                    // Create a transform matrix for each line point
                    Matrix4x4 transformMatrix = Matrix4x4.TRS(currentPoint, rotation, scale);
                    lineTransforms.Add(transformMatrix);

                    // Create the joint using a sphere aligned with the cylinder (with matching faces for smooth transition between the two)
                    var jointScale = new Vector3(lineDiameter, lineDiameter, lineDiameter);
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

        private void Update()
        {
            if (cacheReady)
                DrawLines();
        }

        private void DrawLines()
        {
            //Seperate line draws so we can use different colors for each line
            for (int i = 0; i < lineTransformMatrixCache.Count; i++)
            {
                var lineTransforms = lineTransformMatrixCache[i];
                var lineJointTransforms = jointsTransformMatrixCache[i];
                MaterialPropertyBlock props = materialPropertyBlockCache[i];
                Graphics.DrawMeshInstanced(cylinderMesh, 0, lineMaterial, lineTransforms, props);

                if(!DrawSphereJoints)  return;
                    Graphics.DrawMeshInstanced(jointMesh, 0, lineMaterial, lineJointTransforms, props);
            }
        }
    }
}

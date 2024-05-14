using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class Render3DLines : MonoBehaviour
    {
        public Mesh cylinderMesh;
        public Mesh jointMesh;
        private List<List<Vector3>> linesPoints;
        public Material lineMaterial;
        public bool DrawSphereJoints = true;

        private List<List<Matrix4x4>> lineTransformMatrixCache; 
        private List<Matrix4x4> jointsTransformMatrixCache; 
        private bool cacheReady = false;
        private float lineThickness = 0.2f;

        public void SetLine(List<Vector3> linePoints)
        {
            linesPoints = new List<List<Vector3>> { linePoints };
            SetLines(linesPoints);
        }

        public void SetLines(List<List<Vector3>> linesLists)
        {
            linesPoints = linesLists;
            GenerateTransformMatrixCache();
            DrawLines();
        }

        public void SetLineWidth(float width)
        {
            lineMaterial.SetFloat("_Width", width);
        }

        public void ClearLines()
        {
            linesPoints.Clear();
            lineTransformMatrixCache.Clear();
            cacheReady = false;
        }

        private void GenerateTransformMatrixCache()
        {
            lineTransformMatrixCache = new List<List<Matrix4x4>>(); // Updated to nested List<Matrix4x4>
            jointsTransformMatrixCache = new List<Matrix4x4>();

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
                    var scale = new Vector3(lineThickness, lineThickness, distance);

                    // Create a transform matrix for each line point
                    Matrix4x4 transformMatrix = Matrix4x4.TRS(currentPoint, rotation, scale);
                    lineTransforms.Add(transformMatrix);

                    // Create the joint using a sphere aligned with the cylinder (with matching faces for smooth transition between the two)
                    var jointScale = new Vector3(lineThickness, lineThickness, lineThickness);
                    Matrix4x4 jointTransformMatrix = Matrix4x4.TRS(currentPoint, rotation, jointScale);
                    jointTransforms.Add(jointTransformMatrix);
                }

                lineTransformMatrixCache.Add(lineTransforms);
                jointsTransformMatrixCache.AddRange(jointTransforms);
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
            //Seperate line draws in case we want to add multicolored lines support
            foreach (List<Matrix4x4> lineTransforms in lineTransformMatrixCache)
                Graphics.DrawMeshInstanced(cylinderMesh, 0, lineMaterial, lineTransforms);

            if(!DrawSphereJoints)  return;

            Graphics.DrawMeshInstanced(jointMesh, 0, lineMaterial, jointsTransformMatrixCache);
        }
    }
}

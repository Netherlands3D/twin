using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Collada;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class GameObjectToCollada : MonoBehaviour
    {   
        [ContextMenu("Export to Collada")]
        public void ExportToCollada()
        {
            Export(gameObject);     
        }

        public void ExportToCollada(GameObject targetGameObject)
        {
            Export(targetGameObject);
        }

        private void Export(GameObject targetGameObject)
        {
            var filePath = "";
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SaveFilePanel("Save Collada File", "", "ColladaExport.dae", "dae");
            #endif
            
            if (filePath.Length != 0)
            {
                // Create a new collada file in memory that we will construct
                var collada = new ColladaFile();
                
                // Add the mesh of the game object and all its children to the collada file
                AddMeshToColladaRecursive(targetGameObject, collada);

                // Finish the document
                collada.Finish();

                // Save the collada document as a local file
                collada.Save(filePath);
            }
            else
            {
                Debug.Log("No Collada file path selected");
            }   
        }

        private void AddMeshToColladaRecursive(GameObject targetGameObject, ColladaFile collada)
        {
            // Get the mesh filter component of the game object
            var meshFilter = targetGameObject.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                // Get the mesh of the game object
                var mesh = meshFilter.sharedMesh;
                if (mesh != null)
                {
                    // Get the vertices of the mesh
                    var vertices = mesh.vertices;
                    var triangles = mesh.triangles;

                    // Create a list of double arrays for the vertices
                    var vertexList = new List<double[]>();
                    for (int i = 0; i < triangles.Length; i += 3)
                    {
                        var vertex1 = vertices[triangles[i]];
                        var vertex2 = vertices[triangles[i + 1]];
                        var vertex3 = vertices[triangles[i + 2]];
                        vertexList.Add(new double[] { vertex1.x, vertex1.y, vertex1.z });
                        vertexList.Add(new double[] { vertex2.x, vertex2.y, vertex2.z });
                        vertexList.Add(new double[] { vertex3.x, vertex3.y, vertex3.z });
                    }

                    // Add the mesh to the collada file
                    collada.AddObjectTriangles(vertexList, targetGameObject.name, targetGameObject.GetComponent<MeshRenderer>().sharedMaterial);
                }
            }

            // Recursively add the mesh of all children of the game object
            for (int i = 0; i < targetGameObject.transform.childCount; i++)
            {
                var child = targetGameObject.transform.GetChild(i).gameObject;
                AddMeshToColladaRecursive(child, collada);
            }
        }
    }
}

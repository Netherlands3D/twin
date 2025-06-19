using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
// ReSharper disable once RedundantUsingDirective
using System.Text; //used in the pragma block
using UnityEngine;
using Netherlands3D.MeshClipping;
using Netherlands3D.Twin.Utility;
using UnityEditor;

namespace Netherlands3D.Collada
{
    public class ColladaCreation : ModelFormatCreation
    {
        [DllImport("__Internal")]
        private static extern void DownloadFileImmediate(string callbackGameObjectName, string callbackMethodName, string fileName, byte[] array, int byteLength);

        private BoundingBox boundingBox;
        
        protected override IEnumerator CreateFile(LayerMask includedLayers, Bounds selectedAreaBounds, float minClipBoundsHeight, bool destroyOnCompletion = true)
        {
            var colladaFile = new ColladaFile();
            var objects = GetExportData(includedLayers, selectedAreaBounds, minClipBoundsHeight);
            foreach (var obj in objects)
            {
                colladaFile.AddObjectTriangles(GetColladaVertexCoordinates(obj.Vertices), obj.Name, obj.Material);
            }
            
            //Finish collada file
            colladaFile.Finish();
            yield return null;

            //Save the file
#if UNITY_EDITOR
            var localFile = EditorUtility.SaveFilePanel("Save Collada", "", "export", "dae");
            if (localFile.Length > 0)
            {
                File.WriteAllText(localFile, colladaFile.GetColladaXML());
            }
#elif UNITY_WEBGL
                byte[] byteArray = Encoding.UTF8.GetBytes(colladaFile.GetColladaXML());
                DownloadFileImmediate(exportRunner.name, "", "Collada.dae", byteArray, byteArray.Length);
#endif

            if (destroyOnCompletion)
                Destroy(gameObject);
        }

        /// <summary>
        /// Return the list of vertices with Y and Z swapped, and origin in bottomleft of bounds
        /// </summary>
        /// <param name="vertices"></param>
        /// <returns></returns>
        public List<double[]> GetColladaVertexCoordinates(List<Vector3> vertices)
        {
            List<double[]> doubleVertices = new List<double[]>();

            // Swap Y and Z
            foreach (Vector3 vert in vertices)
            {
                doubleVertices.Add(new double[] { vert.x, vert.z, vert.y });
            }

            return doubleVertices;
        }
    }
}
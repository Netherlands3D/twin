using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Netherlands3D.Collada;
using Netherlands3D.MeshClipping;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin
{
    
    [CreateAssetMenu(fileName = "AreaSelection", menuName = "Netherlands3D/Data/AreaSelection", order = 1)]
    public class AreaSelection : ScriptableObject
    {
        public class Exporter : MonoBehaviour {};

        [DllImport("__Internal")]
        private static extern void DownloadFile(string callbackGameObjectName, string callbackMethodName, string fileName, byte[] array, int byteLength);

        private Bounds selectedAreaBounds;
        private List<Vector3> selectedArea;
        [HideInInspector] public List<Vector3> SelectedArea { get => selectedArea; private set => selectedArea = value; }
        [HideInInspector] public Bounds SelectedAreaBounds { get => selectedAreaBounds; private set => selectedAreaBounds = value; }

        [Header("Settings")]
        [SerializeField] private float minClipBoundsHeight = 1000.0f; 

        [Header("Invoke events")]
        public UnityEvent<List<Vector3>> OnSelectionAreaChanged = new();
        public UnityEvent<Bounds> OnSelectionAreaBoundsChanged = new();
        public UnityEvent<ExportFormat> OnExportFormatChanged = new();
        public UnityEvent<float> modelExportProgressChanged = new();
        public UnityEvent<string> modelExportStatusChanged = new();
        public UnityEvent OnSelectionCleared = new();

        private ExportFormat selectedExportFormat = ExportFormat.Collada;

        [SerializeField] private LayerMask includedLayers;

        public void SetSelectionAreaBounds(Bounds selectedAreaBounds)
        {
            this.SelectedAreaBounds = selectedAreaBounds;
            OnSelectionAreaBoundsChanged.Invoke(this.SelectedAreaBounds);
        }

        public void SetSelectionArea(List<Vector3> selectedArea)
        {
            var bounds = new Bounds();
            foreach(var point in selectedArea)
            {
                bounds.Encapsulate(point);
                bounds.Encapsulate(point + Vector3.up);
            }

            this.SelectedArea = selectedArea;
            OnSelectionAreaChanged.Invoke(this.SelectedArea);

            SetSelectionAreaBounds(bounds);
        }

        public void Download()
        {
            //Spawn exporting progress as monobehaviour to start a coroutine
            var exportGameObject = new GameObject("Export");
            var monoBehaviour = exportGameObject.AddComponent<Exporter>();    
                          
            switch(selectedExportFormat)
            {
                case ExportFormat.Collada:
                    //Slice and export using collada
                    Debug.Log("Exporting Collada of area bounds: " + selectedAreaBounds);
                    monoBehaviour.StartCoroutine(ExportCollada(monoBehaviour.gameObject));
                    break;
                case ExportFormat.AutodeskDXF:
                    //TODO: DXF export implementation
                    Debug.Log("Exporting Autodesk DXF of area bounds: " + selectedAreaBounds);
                    break;
            }
        }

        private IEnumerator ExportCollada(GameObject exportRunner)
        {
            var colladaFile = new ColladaFile();
            var meshClipper = new MeshClipper();

            //Find all gameobjects inside the includedLayers
            var meshFilters = FindObjectsOfType<MeshFilter>();

            //check if the meshfilter is inside the included layers
            var meshFiltersInLayers = new List<MeshFilter>();
            foreach (var meshFilter in meshFilters)
            {
                if (includedLayers == (includedLayers | (1 << meshFilter.gameObject.layer)))
                {
                    meshFiltersInLayers.Add(meshFilter);
                }
            }

            //Clip all the submeshes found whose object bounds overlap with the selected area bounds
            for(int i = 0; i < meshFiltersInLayers.Count; i++)
            {
                var mesh = meshFiltersInLayers[i].sharedMesh;
                for(int j = 0; j < mesh.subMeshCount; j++)
                {
                    var meshFilterGameObject = meshFiltersInLayers[i].gameObject;

                    //Set the object name
                    var subobjectName = meshFilterGameObject.name;
                    Material material = null;
                    if(meshFilterGameObject.TryGetComponent<MeshRenderer>(out var meshRenderer))
                    {
                        //Make sure renderer overlaps bounds
                        if(!meshRenderer.bounds.Intersects(selectedAreaBounds))
                        {
                            continue;
                        }

                        material = meshRenderer.sharedMaterials[j];
                        subobjectName = material.name.Replace(" (Instance)", "").Split(' ')[0];
                    }

                    //Fresh start for meshclipper
                    var clipBounds = selectedAreaBounds;
                    clipBounds.size = new Vector3(clipBounds.size.x, Mathf.Max(clipBounds.size.y, minClipBoundsHeight), clipBounds.size.z);
                    meshClipper.SetGameObject(meshFiltersInLayers[i].gameObject);
                    meshClipper.ClipSubMesh(clipBounds, j);

                    colladaFile.AddObjectTriangles(GetColladaVertexCoordinates(meshClipper.clippedVertices), subobjectName, material);
                    yield return null;
                }
                yield return null;
            }
            yield return null;

            //Finish collada file
            colladaFile.Finish();
            yield return null;

            //Save the file
            #if UNITY_EDITOR
            var localFile = UnityEditor.EditorUtility.SaveFilePanel("Save Collada", "", "export", "dae");
            if(localFile.Length > 0)
            {
                System.IO.File.WriteAllText(localFile, colladaFile.GetColladaXML());
            }
            #elif UNITY_WEBGL
                byte[] byteArray = Encoding.UTF8.GetBytes(colladaFile.GetColladaXML());
                DownloadFile("", "", Path.GetFileName(filePath), byteArray, byteArray.Length);
            }
            #endif


            Destroy(exportRunner); 
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

        public void SetExportFormat(ExportFormat format)
        {
            selectedExportFormat = format;
            OnExportFormatChanged.Invoke(selectedExportFormat);
        }
        public void SetExportFormat(int format)
        {
            //int to enum
            selectedExportFormat = (ExportFormat)format;
        }

        public void ClearSelection()
        {
            selectedAreaBounds = new Bounds(){
                center = Vector3.zero,
                size = Vector3.zero
            };
            selectedArea.Clear();

            OnSelectionCleared.Invoke();
        }
    }    
}

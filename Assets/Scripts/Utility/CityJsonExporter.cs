using System.IO;
using Netherlands3D.Coordinates;
using System.Runtime.InteropServices;
using RuntimeHandle;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Text;

namespace Netherlands3D.Twin
{
    public class CityJsonExporter : MonoBehaviour
    {
        [DllImport("__Internal")]
        private static extern void DownloadFileImmediate(string callbackGameObjectName, string callbackMethodName, string fileName, byte[] array, int byteLength);

        void Update()
        {
            // Using new inputsystem to detect if shift+E is pressed
            if (Keyboard.current.leftShiftKey.isPressed && Keyboard.current.cKey.wasPressedThisFrame)
            {
                Debug.Log("Exporting selected object to CityJSON");
                ExportSelectedObjectCityJSON();
            }
        }

        private void ExportSelectedObjectCityJSON()
        {
            var transformHandles = FindObjectOfType<RuntimeTransformHandle>();
            if(transformHandles && transformHandles.target != null)
            {
                ExportGameObjectToCityJSON();
            }
            Debug.Log("No target object selected to export to JSON");
        }

        private void ExportGameObjectToCityJSON()
        {
            var transformHandles = FindObjectOfType<RuntimeTransformHandle>();
            var target = transformHandles.target;

            if (target != null)
            {
                ExportToCityJSON(target.gameObject);
            }
        }
        
        /// <summary>
        /// StreamWrite CityJSON (makes it easier to add iterative Coroutine writer for large files later on)
        /// </summary>
        /// <param name="targetGameObject">GameObject with Mesh to export</param>
        public void ExportToCityJSON(GameObject targetGameObject)
        {
            StringWriter stringWriter = new StringWriter();
            Mesh mesh = targetGameObject.GetComponentInChildren<MeshFilter>().sharedMesh;
            if(!mesh)
            {
                Debug.LogError("No mesh found on target object");
                return;
            }

            var unityCoordinate = new Coordinate(CoordinateSystem.Unity,
                targetGameObject.transform.position.x,
                targetGameObject.transform.position.y,
                targetGameObject.transform.position.z
            );
            var rdCoordinate = CoordinateConverter.ConvertTo(unityCoordinate, CoordinateSystem.RDNAP);
            var scale = targetGameObject.transform.localScale;


            // Create CityJSON header
            stringWriter.WriteLine("{");
            stringWriter.WriteLine("  \"type\": \"CityJSON\",");
            stringWriter.WriteLine("  \"version\": \"2.0\",");
            
            // Write transform
            stringWriter.WriteLine("  \"transform\": {");
            stringWriter.WriteLine($"    \"scale\": [{scale.x}, {scale.y}, {scale.z}],");
            stringWriter.WriteLine($"    \"translate\": [{rdCoordinate.Points[0]}, {rdCoordinate.Points[1]}, {rdCoordinate.Points[2]}]");
            stringWriter.WriteLine("  },");

            stringWriter.WriteLine("  \"CityObjects\": {");
            // Attributes with identificatie string same as key
            stringWriter.WriteLine("    \"" + targetGameObject.name + "\": {");
            stringWriter.WriteLine("    \"attributes\": {");
            stringWriter.WriteLine("      \"identificatie\": \"" + targetGameObject.name + "\"");
            stringWriter.WriteLine("    },");
            stringWriter.WriteLine("      \"geometry\": [{");
            stringWriter.WriteLine("      \"type\": \"MultiSurface\",");
            stringWriter.WriteLine("      \"lod\": \"2.2\",");
            // Export mesh triangles (with inverted winding order)
            stringWriter.WriteLine("      \"boundaries\": [");
            int[] triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                stringWriter.WriteLine("      [[" + triangles[i + 2] + ", " + triangles[i + 1] + ", " + triangles[i] + "]]" + (i < triangles.Length - 3 ? "," : ""));
            }
            // Geometry end
            stringWriter.WriteLine("      ]");
            stringWriter.WriteLine("    }],");
            stringWriter.WriteLine("    \"type\": \"Building\"");
            stringWriter.WriteLine("  }");
            stringWriter.WriteLine("},");

            // Export mesh vertices
            stringWriter.WriteLine("    \"vertices\": [");
            Vector3[] vertices = mesh.vertices;
            // Apply gameobject world rotation to vertices and make sure the list is reversed
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = targetGameObject.transform.rotation * vertices[i];
            }
            for (int i = 0; i < vertices.Length; i++)
            {
                stringWriter.WriteLine("      [" + vertices[i].x + ", " + vertices[i].z + ", " + vertices[i].y + "]" + (i < vertices.Length - 1 ? "," : ""));
            }
            stringWriter.WriteLine("    ]");
            stringWriter.WriteLine("}");

            var output = stringWriter.ToString();
            
#if UNITY_EDITOR
            string path = UnityEditor.EditorUtility.SaveFilePanel("Save CityJSON file", "", targetGameObject.name, "json");
            if (path.Length != 0)
            {
                File.WriteAllText(path, output);
            }
#elif !UNITY_EDITOR && UNITY_WEBGL
            byte[] byteArray = Encoding.UTF8.GetBytes(output);
            DownloadFileImmediate("", "", targetGameObject.name+".json", byteArray, byteArray.Length);
#endif
        }
    }
}

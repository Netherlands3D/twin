using System.IO;
using UnityEngine;
using UnityEngine.Events;
using Netherlands3D.DataTypeAdapters;
using Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;

namespace Netherlands3D.Functionalities.GLBImporter
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/GLBImportAdapter", fileName = "GLBImportAdapter", order = 0)]
    public class GLBImportAdapter : ScriptableObject, IDataTypeAdapter
    {
        [SerializeField] private GLBSpawner layerPrefab;

        public bool Supports(LocalFile localFile)
        {
            try
            {
                using (var fs = new FileStream(localFile.LocalFilePath, FileMode.Open, FileAccess.Read))
                {
                    byte[] header = new byte[4];
                    int bytesRead = fs.Read(header, 0, 4);

                    // Check for "glTF" ASCII signature
                    return bytesRead == 4 &&
                           header[0] == 0x67 && // 'g'
                           header[1] == 0x6C && // 'l'
                           header[2] == 0x54 && // 'T'
                           header[3] == 0x46;   // 'F'
                }
            }
            catch
            {
                return false;
            } 
        }

        public void Execute(LocalFile localFile)
        {            
            var fullPath = localFile.LocalFilePath;
            var fileName = Path.GetFileName(fullPath);
            GLBSpawner newLayer = Instantiate(layerPrefab);
            newLayer.gameObject.name = fileName;

            newLayer.SetGlbPathInPropertyData(fullPath);
        }
    }
}
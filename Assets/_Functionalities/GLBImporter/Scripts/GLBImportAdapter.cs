using System.IO;
using Netherlands3D.DataTypeAdapters;
using Netherlands3D.Functionalities.GLBImporter.LayerPresets;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Projects;
using UnityEngine;

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

                    // Check for "glTF" ASCII signature to determine this is a valid glTF file
                    // https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#glb-file-format-specification-structure
                    // magic MUST be equal to equal 0x46546C67 (little endian). It is ASCII string glTF and can be used to identify data as Binary glTF.
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
            var uri = AssetUriFactory.ConvertLocalFileToAssetUri(localFile);

            App.Layers.Add("gltf", new Gltf.Args(localFile.FileName, uri));
        }
    }
}
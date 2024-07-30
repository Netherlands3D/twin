using Netherlands3D.Twin.Projects;
using UnityEngine;

namespace Netherlands3D.Twin
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/NL3DImportAdapter", fileName = "NL3DImportAdapter", order = 0)]
    public class NL3DImportAdapter : ScriptableObject, IDataTypeAdapter
    {
        public bool Supports(LocalFile localFile)
        {
            return localFile.SourceUrl.ToLower().EndsWith(".nl3d");      
        }

        public void Execute(LocalFile localFile)
        {
            var projectDataHandler = FindObjectOfType<ProjectDataHandler>();
            if(!projectDataHandler)
            {
                Debug.LogError("Could not find ProjectDataHandler in scene. Cannot load NL3D file.");
                return;
            }

            projectDataHandler.LoadFromFile(localFile.LocalFilePath);
        }
    }
}

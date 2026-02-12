using Netherlands3D.DataTypeAdapters;
using Netherlands3D.Twin.Projects;
using UnityEngine;

namespace Netherlands3D.Twin.DataTypeAdapters
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/NL3DImportAdapter", fileName = "NL3DImportAdapter", order = 0)]
    public class NL3DImportAdapter : ScriptableObject, IDataTypeAdapter<ProjectData>
    {
        public bool Supports(LocalFile localFile)
        {
            return localFile.SourceUrl.ToLower().EndsWith(".nl3d");      
        }

        public ProjectData Execute(LocalFile localFile)
        {
            return ProjectDataHandler.Instance.LoadFromFile(localFile.LocalFilePath);
        }
    }
}

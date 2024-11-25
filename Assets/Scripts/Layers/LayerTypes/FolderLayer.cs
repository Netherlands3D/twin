using System.Runtime.Serialization;
using Netherlands3D.Twin.Projects;

namespace Netherlands3D.Twin.Layers
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers", Name = "Folder")]
    public class FolderLayer : LayerData
    {
        public FolderLayer(string name) : base(name)
        {
            ProjectData.Current.AddStandardLayer(this);
        }
    }
}
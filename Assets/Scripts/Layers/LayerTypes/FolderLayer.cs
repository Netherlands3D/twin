using Netherlands3D.Twin.Projects;

namespace Netherlands3D.Twin.UI.LayerInspector
{
    public class FolderLayer : LayerNL3DBase
    {
        public FolderLayer(string name) : base(name)
        {
            ProjectData.Current.AddStandardLayer(this);
        }
    }
}
using System;
using Netherlands3D.Twin.Projects;

namespace Netherlands3D.Twin.Layers
{
    [Serializable]
    public class FolderLayer : LayerData
    {
        public FolderLayer(string name) : base(name)
        {
            ProjectData.Current.AddStandardLayer(this);
        }
    }
}
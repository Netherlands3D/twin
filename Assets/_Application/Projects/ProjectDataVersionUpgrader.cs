using Netherlands3D.Twin.Layers;
using UnityEngine;

namespace Netherlands3D.Twin.Projects
{
    public class ProjectDataVersionUpgrader : MonoBehaviour
    {
        //Called in the inspector
        public void UpdateProjectVersion(ProjectData newProject)
        {
            if (newProject.Version < 2)
            {
                var groundLayer = App.Layers.Add(LayerBuilder.Create().NamedAs("Ondergrond").OfType("f60b3c7f11823a9ce86527101bac825b"));
                groundLayer.LayerData.SetParent(groundLayer.LayerData.ParentLayer, groundLayer.LayerData.ParentLayer.ChildrenLayers.Count);
                newProject.Version = 2;
            }
        }
    }
}

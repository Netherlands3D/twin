using Netherlands3D.Twin.Layers.UI.AddLayer;
using Netherlands3D.Twin.Layers.UI.HierarchyInspector;

namespace Netherlands3D.Functionalities.UrbanReLeaf
{
    public class SensorLayerToggle : StandardLayerToggle
    {
        protected override void OnEnable()
        {
            layerUIManager = FindObjectOfType<LayerUIManager>();
            base.OnEnable();
        }
    }
}

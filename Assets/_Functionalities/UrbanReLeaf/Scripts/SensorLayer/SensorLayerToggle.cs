using Netherlands3D.Twin.UI.LayerInspector;

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

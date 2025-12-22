using Netherlands3D.Twin.Layers;

namespace Netherlands3D.Twin.Services
{
    public record Layer
    {
        public readonly LayerData LayerData;
        public LayerGameObject LayerGameObject { get; private set; }

        public Layer(LayerData layerData)
        {
            LayerData = layerData;
        }

        public void SetVisualization(LayerGameObject layerGameObject)
        {
            LayerGameObject = layerGameObject;
        }
    }
}

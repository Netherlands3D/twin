using System.Threading.Tasks;
using Netherlands3D.Twin.Layers;

namespace Netherlands3D.Twin.Services
{
    public record Layer
    {
        public readonly LayerData LayerData;
        public Task<LayerGameObject> LayerGameObjectTask { get; private set; }

        public Layer(LayerData layerData)
        {
            LayerData = layerData;
        }

        public void SetVisualizationTask(Task<LayerGameObject> layerGameObjectTask)
        {
            LayerGameObjectTask = layerGameObjectTask;
        }
    }
}

using System.Threading.Tasks;
using Netherlands3D.Twin.Layers.LayerTypes;
using UnityEngine;

namespace Netherlands3D.Twin.Layers
{
    public interface ILayerSpawner
    {
        public Task<LayerGameObject> Spawn(LayerData layerData);

        public Task<LayerGameObject> Spawn(
            LayerData layerData,
            Vector3 position,
            Quaternion rotation
        );
    }
}
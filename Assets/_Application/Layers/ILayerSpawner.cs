using System.Threading.Tasks;
using Netherlands3D.Twin.Layers.LayerTypes;
using UnityEngine;

namespace Netherlands3D.Twin.Layers
{
    public interface ILayerSpawner
    {
        public Task<LayerGameObject> Spawn(ReferencedLayerData layerData);

        public Task<LayerGameObject> Spawn(
            ReferencedLayerData layerData,
            Vector3 position,
            Quaternion rotation
        );
    }
}
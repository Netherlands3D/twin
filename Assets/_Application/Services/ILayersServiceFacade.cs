using System;
using System.Threading.Tasks;
using Netherlands3D.Credentials.StoredAuthorization;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerPresets;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Services
{
    public interface ILayersServiceFacade
    {
        public Layer Add(LayerPresetArgs layerBuilder, UnityAction<LayerGameObject> callback = null);
        public Layer Add(ILayerBuilder layerBuilder, UnityAction<LayerGameObject> callback = null);
        public Task<Layer[]> AddFromUrl(Uri uri, StoredAuthorization authorization);
        public Layer VisualizeData(LayerData layerData, UnityAction<LayerGameObject> callback = null);
        public Layer VisualizeAs(LayerData layerData, string prefabIdentifier, UnityAction<LayerGameObject> callback = null);
        public void Remove(LayerData layer);

        public UnityEvent<LayerData> LayerAdded { get; }
        public UnityEvent<LayerData> LayerRemoved { get; }
    }
}
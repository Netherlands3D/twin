using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Projects;
using UnityEngine;

namespace Netherlands3D.Twin.Layers
{
    public static class LayerConverter
    {
        public static void ConvertToLayer(LayerGameObject layerGameObject, string prefabId)
        {
            ReferencedLayerData data = layerGameObject.LayerData;
            var prefab = ProjectData.Current.PrefabLibrary.GetPrefabById(prefabId);
            var newLayer = GameObject.Instantiate(prefab);
            data.SetReference(newLayer);
            newLayer.OnConvert(layerGameObject);
            layerGameObject.DestroyLayerGameObject();
        }        
    }
}

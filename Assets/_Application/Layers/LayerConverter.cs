using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Projects;
using UnityEngine;

namespace Netherlands3D.Twin.Layers
{
    public static class LayerConverter
    {
        public static void ConvertToLayer(LayerGameObject layerGameObject, string prefabId, bool addSuffix = false)
        {
            ReferencedLayerData data = layerGameObject.LayerData;
            var prefab = ProjectData.Current.PrefabLibrary.GetPrefabById(prefabId);
            var newLayer = GameObject.Instantiate(prefab);
            data.SetReference(newLayer);
            newLayer.OnConvert(layerGameObject);

            if (!string.IsNullOrEmpty(layerGameObject.Suffix) && newLayer.Name.EndsWith(layerGameObject.Suffix))
                newLayer.Name = newLayer.Name.Substring(0, newLayer.Name.Length - layerGameObject.Suffix.Length);
            if(addSuffix)
                newLayer.Name = layerGameObject.Name + newLayer.Suffix;

            layerGameObject.DestroyLayerGameObject();
        }
    }
}

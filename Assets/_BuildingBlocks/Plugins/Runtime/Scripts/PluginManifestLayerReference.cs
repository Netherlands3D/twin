using System;
using Netherlands3D.Twin.Layers;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Netherlands3D.Plugins
{
    [Serializable]
    public class PluginManifestLayerReference
    {
        public AssetReferenceGameObject asset;
        public string identifier = "";
        public string layerName = "";
        
        public void UpdateFields()
        {
            identifier = "";
            if (asset == null  || string.IsNullOrEmpty(asset.AssetGUID)) return;
            
            string path = AssetDatabase.GUIDToAssetPath(asset.AssetGUID);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null) return;
                
            LayerGameObject layerComponent = prefab.GetComponent<LayerGameObject>();
            if (layerComponent == null) return;
                
            identifier = layerComponent.PrefabIdentifier;
            layerName = layerComponent.name;
        }
    }
}
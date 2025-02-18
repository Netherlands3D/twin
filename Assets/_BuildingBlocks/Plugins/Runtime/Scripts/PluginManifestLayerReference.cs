using System;
using UnityEngine.AddressableAssets;

namespace Netherlands3D.Plugins
{
    [Serializable]
    public class PluginManifestLayerReference
    {
        public AssetReferenceGameObject asset;
        public string identifier = "";
        public string layerName = "";
    }
}
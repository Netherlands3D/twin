using System;
using System.Collections.Generic;
using System.IO;
using Netherlands3D.Catalogs;
using Netherlands3D.Catalogs.Catalogs;
using Netherlands3D.Twin.Layers;
using UnityEngine;

namespace Netherlands3D.AssetLibrary.Entries
{
    [CreateAssetMenu(menuName = "Netherlands3D/Asset Library/Asset Bundle Entry")]
    public class AssetBundleAssetEntry : UrlAssetEntry
    {
        [SerializeField] private string bundleName;
        [SerializeField] private string prefabName;

        public string BundleName => bundleName;
        public string PrefabName => prefabName;

        public override ICatalogItem ToCatalogItem()
        {
            if (string.IsNullOrWhiteSpace(bundleName) || string.IsNullOrWhiteSpace(prefabName))
                return null;

            var resolved = $"streamingassets:///{Uri.EscapeDataString(bundleName)}/{Uri.EscapeDataString(prefabName)}";
            return InMemoryCatalog.CreateRecord(Id, Title, Description, new Uri(resolved, UriKind.Absolute));
        }
        
        public override IEnumerable<LayerGameObject> CollectPrefabs()
        {
            yield break;
        }
    }
}
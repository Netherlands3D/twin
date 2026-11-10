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
    public class AssetBundleAssetEntry : AssetLibraryEntry
    {
        //[Tooltip("If set, we export prefab-library:///[PrefabIdentifier].")]
        [SerializeField] private string bundleName;
        [SerializeField] private string prefabName;

        public string BundleName => bundleName;
        public string PrefabName => prefabName;

        public override ICatalogItem ToCatalogItem()
        {
            if (string.IsNullOrEmpty(bundleName)) return null;

            var resolved = $"assetbundle:///{Uri.EscapeDataString(bundleName)}";

            var uri = string.IsNullOrWhiteSpace(resolved) ? null : new Uri(resolved, UriKind.Absolute);
            return InMemoryCatalog.CreateRecord(Id, Title, Description, uri);
        }
        
        public override IEnumerable<LayerGameObject> CollectPrefabs()
        {
            yield break;
        }
    }
}
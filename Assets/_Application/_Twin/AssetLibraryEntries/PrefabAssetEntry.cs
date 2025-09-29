using System;
using Netherlands3D.Catalogs;
using Netherlands3D.Catalogs.Catalogs;
using Netherlands3D.Twin.Layers;
using UnityEngine;

namespace Netherlands3D._Application._Twin.AssetLibraryEntries
{
    [CreateAssetMenu(menuName = "Netherlands3D/Asset Library/Prefab Record")]
    public class PrefabAssetEntry : AssetLibraryEntry
    {
        [Tooltip("If set, we export prefab-library:///[PrefabIdentifier].")]
        [SerializeField] private LayerGameObject prefab;

        public override ICatalogItem ToCatalogItem()
        {
            if (!prefab)
            {
                return null;
            }
            
            var resolved = $"prefab-library:///{prefab.PrefabIdentifier}";

            var uri = string.IsNullOrWhiteSpace(resolved) ? null : new Uri(resolved, UriKind.Absolute);
            return InMemoryCatalog.CreateRecord(Id, Title, Description, uri);
        }
    }
}
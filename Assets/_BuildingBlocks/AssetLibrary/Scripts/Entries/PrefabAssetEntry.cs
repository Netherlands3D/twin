using System;
using System.Collections.Generic;
using Netherlands3D.Catalogs;
using Netherlands3D.Catalogs.Catalogs;
using Netherlands3D.Twin.Layers;
using UnityEngine;

namespace Netherlands3D.AssetLibrary.Entries
{
    [CreateAssetMenu(menuName = "Netherlands3D/Asset Library/Prefab Record")]
    public class PrefabAssetEntry : AssetLibraryEntry
    {
        [Tooltip("If set, we export prefab-library:///[PrefabIdentifier].")]
        [SerializeField] private LayerGameObject prefab;

        public LayerGameObject Prefab => prefab;

        public override ICatalogItem ToCatalogItem()
        {
            if (!prefab)
            {
                return null;
            }
            
            var resolved = $"{AssetLibrary.PREFAB_IDENTIFIER}:///{prefab.PrefabIdentifier}";

            var uri = string.IsNullOrWhiteSpace(resolved) ? null : new Uri(resolved, UriKind.Absolute);
            return InMemoryCatalog.CreateRecord(Id, Title, Description, uri);
        }
        
        public override IEnumerable<LayerGameObject> CollectPrefabs()
        {
            yield return prefab;
        }
    }
}
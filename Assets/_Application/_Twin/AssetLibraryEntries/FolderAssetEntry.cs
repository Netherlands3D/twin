using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Catalogs;
using Netherlands3D.Catalogs.Catalogs;
using Netherlands3D.Twin.Layers;
using UnityEngine;

namespace Netherlands3D._Application._Twin.AssetLibraryEntries
{
    [CreateAssetMenu(menuName = "Netherlands3D/Asset Library/Folder")]
    public class FolderAssetEntry : AssetLibraryEntry
    {
        [SerializeField] private List<AssetLibraryEntry> children = new();

        public override ICatalogItem ToCatalogItem()
        {
            return InMemoryCatalog.CreateFolder(
                Id,
                Title,
                Description,
                children.Where(c => c != null).Select(c => c.ToCatalogItem())
            );
        }

        public override IEnumerable<AssetLibraryEntry> GetChildren() => children;

        public override IEnumerable<ScriptableObject> CollectEvents()
        {
            return children.SelectMany(child => child.CollectEvents());
        }

        public override IEnumerable<LayerGameObject> CollectPrefabs()
        {
            return children.SelectMany(child => child.CollectPrefabs());
        }
    }
}
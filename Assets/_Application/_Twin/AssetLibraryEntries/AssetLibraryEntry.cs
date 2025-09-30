using System.Collections.Generic;
using Netherlands3D.Catalogs;
using Netherlands3D.Twin.Layers;
using UnityEngine;

namespace Netherlands3D._Application._Twin.AssetLibraryEntries
{
    /// <summary>
    /// Base ScriptableObject for all Asset Library entries.
    /// </summary>
    public abstract class AssetLibraryEntry : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string title;
        [TextArea] [SerializeField] private string description;

        public string Id => id;
        public string Title => title;
        public string Description => description;

        /// <summary>
        /// Convert this entry into a catalog item.
        /// </summary>
        public abstract ICatalogItem ToCatalogItem();

        /// <summary>
        /// For hierarchical entries (Folder/Dataset). Others may return empty.
        /// </summary>
        public virtual IEnumerable<AssetLibraryEntry> GetChildren()
        {
            yield break;
        }

        public virtual IEnumerable<LayerGameObject> CollectPrefabs()
        {
            yield break;
        }

        public virtual IEnumerable<ScriptableObject> CollectEvents()
        {
            yield break;
        }
    }
}
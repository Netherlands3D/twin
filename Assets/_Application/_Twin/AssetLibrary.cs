using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Catalogs;
using Netherlands3D.Catalogs.CatalogItems;
using Netherlands3D.Catalogs.Catalogs;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Layers;
using UnityEngine;

namespace Netherlands3D._Application._Twin
{
    /// <summary>
    /// This scriptable object is the basis for our application catalog and is, intentionally, a wrapper
    /// around an instance of InMemoryCatalog so that we can populate the InMemoryCatalog from within Unity instead of
    /// purely through code.
    ///
    /// Other services and instances can use the Import method in this ScriptableObject to enrich the application
    /// catalog in runtime.
    ///
    /// This catalog will feature all items that can be exposed in the Asset Library UI, and you can use the AssetLoader
    /// service to spawn them.
    /// </summary>
    [CreateAssetMenu(menuName = "Netherlands3D/ApplicationCatalog")]
    public class AssetLibrary : ScriptableObject
    {
        public enum EntryType
        {
            Url,
            Prefab,
            Folder,
            DataSet
        }

        [Serializable]
        public class Entry
        {
            public EntryType type;
            public string id;
            public string title;
            [TextArea] public string description;
            public string url;
            public LayerGameObject prefab;
            public List<Entry> children = new();

            public ICatalogItem ToCatalogItem()
            {
                switch (type)
                {
                    case EntryType.Url:
                        return InMemoryCatalog.CreateRecord(
                            id,
                            title,
                            description,
                            string.IsNullOrWhiteSpace(url) ? null : new Uri(url, UriKind.Absolute)
                        );

                    case EntryType.Prefab:
                        var resolved = url;
                        if (prefab)
                        {
                            resolved = $"prefab-library:///{prefab.PrefabIdentifier}";
                        }

                        return InMemoryCatalog.CreateRecord(
                            id,
                            title,
                            description,
                            string.IsNullOrWhiteSpace(resolved) ? null : new Uri(resolved, UriKind.Absolute)
                        );

                    case EntryType.Folder:
                        return InMemoryCatalog.CreateFolder(
                            id,
                            title,
                            description,
                            children.Select(c => c.ToCatalogItem())
                        );

                    case EntryType.DataSet:
                        return InMemoryCatalog.CreateDataset(
                            id,
                            title,
                            description,
                            children.Select(c => c.ToCatalogItem())
                        );

                    default: throw new ArgumentOutOfRangeException();
                }
            }
        }

        [SerializeField] private List<Entry> items = new();

        public InMemoryCatalog Catalog { get; private set; }

        private void OnEnable()
        {
            // By design - the entire catalog is rebuilt every time the asset is loaded. It is the entries in this
            // Scriptable Object that are persisted so that the catalog can be populated from the Unity editor
            Catalog = new InMemoryCatalog(
                "application",
                "Application",
                "Built from ApplicationCatalog asset"
            );
            items.ForEach(entry => Import(entry.ToCatalogItem()));
        }

        public void Import(ICatalogItem catalogItem)
        {
            Catalog.Add(catalogItem);
        }

        public async void Load(string id)
        {
            var item = await Catalog.GetAsync(id);
            var layerBuilder = CreateLayerBuilder(item);
            await App.Layers.Add(layerBuilder);
        }

        private ILayerBuilder CreateLayerBuilder(ICatalogItem item)
        {
            if (item is not RecordItem recordItem)
            {
                Debug.LogError("Attempting to load a catalog item that is not a record");
                return null;
            }
            
            return LayerBuilder.Create()
                .FromUrl(recordItem.Url)
                .NamedAs(recordItem.Title);
        }
    }
}
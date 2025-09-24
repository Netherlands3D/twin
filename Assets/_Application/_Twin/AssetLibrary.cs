using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Catalogs;
using Netherlands3D.Catalogs.CatalogItems;
using Netherlands3D.Catalogs.Catalogs;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Layers;
using UnityEngine;
using Object = UnityEngine.Object;

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
            Url = 0,
            Prefab = 1,
            Process = 4,
            ScriptableObjectEvent = 5,
            Folder = 2,
            DataSet = 3
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
            public ScriptableObject scriptableObjectEvent;
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

                    // Experimental feature to support calling processes, which may be scriptable events in our case
                    case EntryType.Process:
                        return new ProcessItem(
                            id, 
                            title, 
                            description,
                            processAddress: string.IsNullOrWhiteSpace(url) ? null : new Uri(url, UriKind.Absolute)
                        );

                    // Experimental feature to support calling processes, which may be scriptable events in our case
                    case EntryType.ScriptableObjectEvent:
                        var resolvedProcess = url;
                        if (scriptableObjectEvent)
                        {
                            resolvedProcess = $"event:///{scriptableObjectEvent.GetInstanceID()}";
                        }

                        return new ProcessItem(
                            id, 
                            title, 
                            description,
                            processAddress: string.IsNullOrWhiteSpace(resolvedProcess) 
                                ? null : new Uri(resolvedProcess, UriKind.Absolute)
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
            Load(item);
        }

        public async void Load(ICatalogItem catalogItem)
        {
            if (catalogItem is not RecordItem recordItem)
            {
                Debug.LogWarning(
                    "Unable to load catalog item of type " + catalogItem.GetType() + ", expected a RecordItem"
                );
                return;
            }
            var layerBuilder = CreateLayerBuilder(recordItem);
            await App.Layers.Add(layerBuilder);
        }

        private ILayerBuilder CreateLayerBuilder(ICatalogItem item)
        {
            if (item is null)
            {
                Debug.LogError("No catalog item was passed to create a layer from");
                return null;
            }
            if (item is not RecordItem recordItem)
            {
                Debug.LogError("Attempting to load a catalog item that is not a record, got " + item.GetType());
                return null;
            }
            
            // This uses the Import from URL flow, and thus automatically detects which type of service is imported
            // using the ImportAdapters. This is why there is no further specification here - the information below
            // is all we need to get the ball rolling.
            return LayerBuilder.Create()
                .FromUrl(recordItem.Url)
                .NamedAs(recordItem.Title);
        }
    }
}
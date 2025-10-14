using System.Collections.Generic;
using Netherlands3D.AssetLibrary.Entries;
using Netherlands3D.Catalogs;
using Netherlands3D.Catalogs.CatalogItems;
using Netherlands3D.Catalogs.Catalogs;
using Netherlands3D.Events;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Layers;
using UnityEngine;

namespace Netherlands3D.AssetLibrary
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
        [SerializeField] private List<AssetLibraryEntry> items = new();
        
        // Cached list of scriptable object events that have been registered
        private readonly Dictionary<int, ScriptableObject> scriptableObjectEvents = new();
        private readonly Dictionary<string, LayerGameObject> prefabs = new();

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
            
            // Register all top-level entries, the entries themselves will ensure their children are registered
            foreach (var entry in items)
            {
                RegisterEntry(entry);
            }
        }

        private void RegisterEntry(AssetLibraryEntry entry)
        {
            foreach (var prefab in entry.CollectPrefabs())
            {
                prefabs[prefab.PrefabIdentifier] = prefab;
            }
            foreach (var @event in entry.CollectEvents())
            {
                scriptableObjectEvents[@event.GetInstanceID()] = @event;
            }

            // Convert the entry and its children to catalog items
            var catalogItem = entry.ToCatalogItem();
            if (catalogItem == null)
            {
                Debug.LogError($"Couldn't find any catalog item for {entry.name} ({entry.Id})");
                return;
            }

            Import(catalogItem);
        }

        public void Import(ICatalogItem catalogItem)
        {
            if (catalogItem == null) return;

            Catalog.Add(catalogItem);
        }

        public async void Load(string id)
        {
            var item = await Catalog.GetAsync(id);
            if (item is RecordItem recordItem) Load(recordItem);
            if (item is DataService processItem) Trigger(processItem);
        }

        public async void Load(RecordItem recordItem)
        {
            var layerBuilder = CreateLayerBuilder(recordItem);
            await App.Layers.Add(layerBuilder);
        }

        public void Trigger(DataService dataService)
        {
            var endpoint = dataService.Endpoint;
            if (endpoint?.Scheme != "event")
            {
                Debug.LogWarning("Data services other than events are not supported yet");
                return;
            }

            var eventIdAsString = endpoint.AbsolutePath.Trim('/');
            if (!int.TryParse(eventIdAsString, out var eventId))
            {
                Debug.LogError("Event identifier was not an integer, found: " + eventIdAsString);
                return;
            }
            
            if (!scriptableObjectEvents.TryGetValue(eventId, out var soEvent))
            {
                Debug.LogError($"Event with identifier '{eventId}' could not be found");
                return;
            }

            if (soEvent is not TriggerEvent invoker)
            {
                Debug.LogError($"Event was not of type TriggerEvent, other types are not supported at the moment");
                return;
            }

            invoker.InvokeStarted();
        }

        private ILayerBuilder CreateLayerBuilder(ICatalogItem item)
        {
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
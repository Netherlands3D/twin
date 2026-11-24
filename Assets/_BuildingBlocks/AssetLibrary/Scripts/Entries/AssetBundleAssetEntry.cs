using System;
using System.Collections.Generic;
using Netherlands3D.Catalogs;
using Netherlands3D.Catalogs.Catalogs;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers;
using UnityEngine;

namespace Netherlands3D.AssetLibrary.Entries
{
    [CreateAssetMenu(menuName = "Netherlands3D/Asset Library/Asset Bundle Entry")]
    public class AssetBundleAssetEntry : UrlAssetEntry
    {
        [SerializeField] private string bundleName;
        [SerializeField] private string prefabName;
        [SerializeField] private Vector3 coordinate = Vector3.zero;
        [SerializeField] private CoordinateSystem coordinateSystem;
        [SerializeField] private Vector3 rotation;

        public override ICatalogItem ToCatalogItem()
        {
            if (string.IsNullOrWhiteSpace(bundleName) ||
                string.IsNullOrWhiteSpace(prefabName))
                return null;

            
            Dictionary<string, object> metaData = new Dictionary<string, object>();
            metaData.Add("coordinate", coordinate);
            metaData.Add("rotation", rotation);
            metaData.Add("coordinateSystem", coordinateSystem);
            
            var resolved = $"streamingassets:///{Uri.EscapeDataString(bundleName)}/{Uri.EscapeDataString(prefabName)}";

            return InMemoryCatalog.CreateRectordWithMetaData(
                Id,
                Title,
                metaData,
                Description,
                new Uri(resolved, UriKind.Absolute)
            );
        }
        
        public override IEnumerable<LayerGameObject> CollectPrefabs()
        {
            yield break;
        }
    }
}
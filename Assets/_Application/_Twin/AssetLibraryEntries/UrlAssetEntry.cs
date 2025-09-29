using System;
using Netherlands3D.Catalogs;
using Netherlands3D.Catalogs.Catalogs;
using UnityEngine;

namespace Netherlands3D._Application._Twin.AssetLibraryEntries
{
    [CreateAssetMenu(menuName = "Netherlands3D/Asset Library/URL Record")]
    public class UrlAssetEntry : AssetLibraryEntry
    {
        [SerializeField] private string url;

        public override ICatalogItem ToCatalogItem()
        {
            var uri = string.IsNullOrWhiteSpace(url) ? null : new Uri(url, UriKind.Absolute);
            return InMemoryCatalog.CreateRecord(Id, Title, Description, uri);
        }
    }
}
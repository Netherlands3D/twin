using System.IO;
using System.Threading.Tasks;
using Netherlands3D.Catalogs;
using Netherlands3D.Catalogs.Catalogs;
using Netherlands3D.DataTypeAdapters;
using Netherlands3D.Events;
using UnityEngine;

namespace Netherlands3D.Twin.Functionalities.Catalogs
{
    [CreateAssetMenu(
        menuName = "Netherlands3D/Adapters/OgcApiCatalogImportAdapter", 
        fileName = "OgcApiCatalogImportAdapter",
        order = 0
    )]
    public class OgcApiCatalogImportAdapter : ScriptableObject, IDataTypeAdapter<Task<ICatalogItem>>
    {
        [SerializeField] private AssetLibrary.AssetLibrary assetLibrary;
        [SerializeField] private TriggerEvent openAssetLibrary;

        public bool Supports(LocalFile localFile)
        {
            using var reader = new StreamReader(localFile.LocalFilePath);

            return ContentMatches.JsonObject(reader)
                && ContentMatches.JsonContainsLinkWithRelation(reader, "conformance");
        }

        public async Task<ICatalogItem> Execute(LocalFile localFile)
        {
            var catalogItem = await OgcApiCatalog.CreateAsync(localFile.SourceUrl);
            assetLibrary.Import(catalogItem);
            
            openAssetLibrary.Invoke();
            return catalogItem;
        }
    }
}
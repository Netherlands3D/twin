using System.IO;
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
    public class OgcApiCatalogImportAdapter : ScriptableObject, IDataTypeAdapter
    {
        [SerializeField] private AssetLibrary.AssetLibrary assetLibrary;
        [SerializeField] private TriggerEvent openAssetLibrary;

        public bool Supports(LocalFile localFile)
        {
            using var reader = new StreamReader(localFile.LocalFilePath);

            return ContentMatches.JsonObject(reader)
                && ContentMatches.JsonContainsLinkWithRelation(reader, "conformance");
        }

        public async void Execute(LocalFile localFile)
        {
            assetLibrary.Import(await OgcApiCatalog.CreateAsync(localFile.SourceUrl));
            
            openAssetLibrary.Invoke();
        }
    }
}
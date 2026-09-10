using System.IO;
using System.Threading.Tasks;
using Netherlands3D.Catalogs;
using Netherlands3D.Catalogs.Catalogs;
using Netherlands3D.DataTypeAdapters;
using Netherlands3D.Events;
using UnityEngine;

namespace Netherlands3D.Twin.Functionalities.Catalogs
{
    /// <summary>
    /// A data-type adapter that recognises an OGC CSW 2.0.2 GetCapabilities response and registers the
    /// corresponding endpoint as an <see cref="OgcCswCatalog"/> inside the application's asset library.
    ///
    /// Detection strategy (either condition is sufficient):
    /// <list type="bullet">
    ///   <item>
    ///     The source URL contains both <c>SERVICE=CSW</c> and <c>REQUEST=GetCapabilities</c>
    ///     (case-insensitive), and the cached body contains the word "Capabilities".
    ///   </item>
    ///   <item>
    ///     The cached body contains a CSW namespace URI (<c>http://www.opengis.net/cat/csw</c>)
    ///     and the text "Capabilities".
    ///   </item>
    /// </list>
    /// </summary>
    [CreateAssetMenu(
        menuName = "Netherlands3D/Adapters/OgcCswCatalogImportAdapter",
        fileName = "OgcCswCatalogImportAdapter",
        order = 0
    )]
    public class OgcCswCatalogImportAdapter : ScriptableObject, IDataTypeAdapter<Task<ICatalogItem>>
    {
        [SerializeField] private AssetLibrary.AssetLibrary assetLibrary;
        [SerializeField] private TriggerEvent openAssetLibrary;

        public bool Supports(LocalFile localFile)
        {
            var sourceUrl = localFile.SourceUrl ?? string.Empty;
            var bodyContents = File.ReadAllText(localFile.LocalFilePath);

            // Detect via URL query parameters (case-insensitive)
            var isCswGetCapabilitiesUrl =
                sourceUrl.IndexOf("SERVICE=CSW", System.StringComparison.OrdinalIgnoreCase) >= 0 &&
                sourceUrl.IndexOf("REQUEST=GetCapabilities", System.StringComparison.OrdinalIgnoreCase) >= 0;

            if (isCswGetCapabilitiesUrl)
                return bodyContents.Contains("Capabilities");

            // Detect via body content: look for the CSW namespace URI
            return bodyContents.Contains("http://www.opengis.net/cat/csw") &&
                   bodyContents.Contains("Capabilities");
        }

        public async Task<ICatalogItem> Execute(LocalFile localFile)
        {
            var catalogItem = await OgcCswCatalog.CreateAsync(localFile.SourceUrl);
            assetLibrary.Import(catalogItem);

            openAssetLibrary.InvokeStarted();
            return catalogItem;
        }
    }
}

using System.Threading.Tasks;

namespace Netherlands3D.Catalogs.Catalogs
{
    /// <summary>
    /// This is a proof of concept Catalog built against https://api.pdok.nl/catalogus/v1-demo/ to test how
    ///
    /// TODO: Should this be in the Catalogs package, or in the application itself? Or in a separate PDOK package?
    /// 
    /// </summary>
    public class PdokOgcApiCatalog : PyCswOgcApiCatalog
    {
        private PdokOgcApiCatalog(OgcApi.OgcApi ogcApi) : base(ogcApi)
        {
        }

        /// <summary>
        /// Catalogs have a static factory method to create them as async information may be needed to populate
        /// their basic properties.
        /// </summary>
        public static async Task<PdokOgcApiCatalog> CreateAsync()
        {
            var ogcApi = new OgcApi.OgcApi("https://api.pdok.nl/catalogus/v1-demo/");
            
            return new PdokOgcApiCatalog(ogcApi)
            {
                Title = await ogcApi.Title(),
                Description = await ogcApi.Description()
            };
        }

    }
}
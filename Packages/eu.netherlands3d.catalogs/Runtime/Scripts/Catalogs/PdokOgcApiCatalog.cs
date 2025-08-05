using System.Threading.Tasks;
using Netherlands3D.Catalogs.Catalogs.Strategies;

namespace Netherlands3D.Catalogs.Catalogs
{
    /// <summary>
    /// This is a proof of concept Catalog built against https://api.pdok.nl/catalogus/v1-demo/ to test how
    ///
    /// TODO: Should this be in the Catalogs package, or in the application itself? Or in a separate PDOK package?
    /// 
    /// </summary>
    public class PdokOgcApiCatalog : OgcApiCatalog
    {
        private PdokOgcApiCatalog(OgcApi.OgcApi ogcApi, OgcApiRecordsStrategy recordsStrategy) : base(ogcApi, recordsStrategy)
        {
        }

        /// <summary>
        /// Catalogs have a static factory method to create them as async information may be needed to populate
        /// their basic properties.
        /// </summary>
        public static async Task<PdokOgcApiCatalog> CreateAsync()
        {
            var ogcApi = new OgcApi.OgcApi("https://api.pdok.nl/catalogus/v1-demo/");
            var conformance = await ogcApi.Conformance();
            
            // We know PDOK is a PyCsw server, so we can use that to get the records strategy. We do need to include
            // the fallback strategy for features that do not match PyCSW heuristics (which is any feature that is not
            // a service definition with a recognized access point URL)
            var recordsStrategy = new OgcApiStrategyDispatcher(conformance, new OgcApiRecordsStrategy[]
            {
                // PDOK also defines some of their features as OGC api features with "download" link, so we have an extra
                // strategy for PDOK until we know whether this is PyCSW specific
                new PdokOgcApiRecordsStrategy(conformance),
                new PyCswOgcApiRecordsStrategy(conformance),
                new FallbackOgcApiRecordsStrategy(conformance),
            }); 

            var id = await ogcApi.Id();
            var title = await ogcApi.Title();
            var description = await ogcApi.Description();
            
            return new PdokOgcApiCatalog(ogcApi, recordsStrategy)
            {
                Id = id,
                Title = title,
                Description = description
            };
        }
    }
}
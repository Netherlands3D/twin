using Netherlands3D.OgcApi;
using Netherlands3D.OgcApi.Features;

namespace Netherlands3D.Catalogs.Catalogs.Strategies
{
    public class FallbackOgcApiRecordsStrategy : OgcApiRecordsStrategy
    {
        public FallbackOgcApiRecordsStrategy(ConformanceDeclaration conformance) : base(conformance)
        {
        }

        /// <summary>
        /// The fallback can handle any feature, you do get less information and service endpoints cannot be
        /// determined until more information on metadata structuring is available for OGC API Records.
        /// </summary>
        public override bool CanHandle(Feature feature)
        {
            return true;
        }
    }
}
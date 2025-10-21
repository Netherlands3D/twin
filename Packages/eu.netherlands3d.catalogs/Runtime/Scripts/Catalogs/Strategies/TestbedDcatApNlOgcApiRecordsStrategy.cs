using System;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Netherlands3D.Catalogs.CatalogItems;
using Netherlands3D.OgcApi;
using Netherlands3D.OgcApi.Features;

namespace Netherlands3D.Catalogs.Catalogs.Strategies
{
    public class TestbedDcatApNlOgcApiRecordsStrategy : OgcApiRecordsStrategy
    {
        public TestbedDcatApNlOgcApiRecordsStrategy(ConformanceDeclaration conformance) : base(conformance)
        {
        }

        public override bool CanHandle(Feature feature)
        {
            // No links? No dice.
            if (feature?.Links == null || feature.Links.Length == 0)
                return false;

            // We only support dcat:Dataset in DCAT-AP-NL at the moment due to confusion what datasets and services are
            if (!RecordIsDataset(feature)) {
                return false;
            }

            // If the record type is dcat:Dataset and has the right conformance class - instant GO time
            if (RecordHasConformanceClass(feature, "http://modellen.geostandaarden.nl/dcat-ap-nl/")) {
                return true;
            }

            // If the record type is dcat:Dataset and has the link with a describes clause, good enough
            return feature.Links.Any(IsDcatApNlDatasetLink);
        }

        private bool RecordHasConformanceClass(Feature feature, string conformanceClass) 
            => feature.Properties.TryGetValue("conformsTo", out var property) 
                   && property is string[] conformanceClasses 
                   && conformanceClasses.Contains(conformanceClass);

        private bool RecordIsDataset(Feature feature) 
            => feature.Properties.TryGetValue("type", out var property) && property is "dcat:Dataset";

        private bool IsDcatApNlDatasetLink(Link link) => link.Rel == "describes";

        public override async Task<ICatalogItem> ParseFeature(Feature feature)
        {
            var result = await base.ParseFeature(feature);

            // if it is not a record, we don't need to do anything else
            if (result is not RecordItem recordItem) return result;

            var endpoint = FindEndpointLink(feature);
            var type = DetermineLinkMediaType(endpoint);

            recordItem.WithEndpoint(
                endpoint != null ? new Uri(endpoint.Href) : null,
                type,
                endpoint?.Type
            );

            return recordItem;
        }

        [CanBeNull]
        private static Link FindEndpointLink(Feature item)
        {
            if (item.Links == null) return null;

            foreach (var link in item.Links.Where(link => link.Rel == "describes"))
            {
                return link;
            }

            return null;
        }

        private string DetermineLinkMediaType(Link endpoint)
        {
            if (endpoint.ExtensionData.TryGetValue("type", out var protocol))
            {
                return IsoCodeLists.ProtocolToMediaType((string)protocol);
            }

            return endpoint.Type;
        }
    }
}
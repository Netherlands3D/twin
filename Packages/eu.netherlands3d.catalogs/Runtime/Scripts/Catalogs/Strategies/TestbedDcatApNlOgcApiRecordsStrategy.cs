using System;
using System.Collections.Generic;
using System.Linq;
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

        public override bool TryParseFeature(Feature feature, out ICatalogItem catalogItem)
        {
            catalogItem = null;
            if (!CanHandle(feature)) return false;

            var distributionLinks = feature.Links.Where(link => link.Rel == "describes").ToList();
            return distributionLinks.Count switch
            {
                0 => false, // no distributions means no record item as it is useless to us
                1 => TryParseFeatureAsRecordItem(feature, out catalogItem),
                _ => TryParseFeatureAsDataSet(feature, out catalogItem, distributionLinks)
            };
        }

        private bool TryParseFeatureAsRecordItem(Feature feature, out ICatalogItem catalogItem)
        {
            if (!base.TryParseFeature(feature, out catalogItem)) return false;

            // if it is not a record, we don't need to do anything else
            if (catalogItem is not RecordItem recordItem) return true;

            var endpoint = FindEndpointLink(feature);
            var type = DetermineLinkMediaType(endpoint);

            recordItem.WithEndpoint(
                endpoint != null ? new Uri(endpoint.Href) : null,
                type,
                endpoint?.Type
            );

            return true;
        }

        private bool TryParseFeatureAsDataSet(
            Feature feature, 
            out ICatalogItem catalogItem,
            List<Link> distributions
        ) {
            feature.Properties.TryGetValue("title", out var title);
            feature.Properties.TryGetValue("description", out var description);

            catalogItem = InMemoryCatalog.CreateDataset(
                feature.Id,
                title as string,
                description as string,
                ConvertDistributionsIntoRecords(distributions)
            );
            foreach (var metadataRecord in feature.Properties.ToDictionary(kvp => kvp.Key, kvp => kvp.Value))
            {
                catalogItem.Metadata.Add(metadataRecord);
            }

            return true;
        }

        private List<ICatalogItem> ConvertDistributionsIntoRecords(List<Link> distributions)
        {
            var items = new List<ICatalogItem>();
            foreach (var distribution in distributions)
            {
                var recordItem = ConvertLinkToRecordItem(distribution);
                items.Add(recordItem);
            }

            return items;
        }

        private RecordItem ConvertLinkToRecordItem(Link distribution)
        {
            var recordItem = InMemoryCatalog.CreateRecord(
                Guid.NewGuid().ToString(), // Distributions do not have an id - so we generate one upon import
                distribution.Title
            );
            recordItem.WithEndpoint(
                new Uri(distribution.Href),
                DetermineLinkMediaType(distribution),
                distribution.Type
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
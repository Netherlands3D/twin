using System;
using System.Linq;
using Netherlands3D.Catalogs.CatalogItems;
using Netherlands3D.OgcApi;
using Netherlands3D.OgcApi.Features;

namespace Netherlands3D.Catalogs.Catalogs.Strategies
{
    public class PdokOgcApiRecordsStrategy : OgcApiRecordsStrategy
    {
        public PdokOgcApiRecordsStrategy(ConformanceDeclaration conformance) : base(conformance)
        {
        }

        public override bool CanHandle(Feature feature)
        {
            if (feature?.Links == null || feature.Links.Length == 0)
                return false;

            // Only handle features explicitly scoped as services because we do not know the shape of the dataset
            // scope yet
            if (!feature.Properties.TryGetValue("type", out var typeObj) 
                || typeObj is not string type 
                || !type.Equals(IsoCodeLists.Scope.Service, StringComparison.OrdinalIgnoreCase)
            ) {
                return false;
            }
            
            return ContainsPdokOgcApiFeaturesServiceLink(feature);
        }

        private static bool ContainsPdokOgcApiFeaturesServiceLink(Feature feature)
        {
            return feature.Links.Any(IsPdokOgcApiFeaturesServiceLink);
        }

        private static bool IsPdokOgcApiFeaturesServiceLink(Link link)
        {
            if (!(link.Rel?.Equals("download", StringComparison.OrdinalIgnoreCase) ?? false)) return false;

            if (!link.ExtensionData.TryGetValue("protocol", out var protocolObj)) return false;
            
            return ((string)protocolObj)?.Equals("OGC:API features", StringComparison.OrdinalIgnoreCase) ?? false;
        }

        public override bool TryParseFeature(Feature feature, out ICatalogItem catalogItem)
        {
            if (!base.TryParseFeature(feature, out catalogItem)) return false;
            
            feature.Properties.TryGetValue("protocol", out var protocol);

            var endpoint = FindEndpointLink(feature);
            var type = DetermineLinkMediaType(endpoint);

            catalogItem = new RecordItem
            {
                Url = endpoint != null ? new Uri(endpoint.Href) : null,
                MediaType = type,
                Protocol = protocol as string,
            };
            return true;
        }

        private static Link FindEndpointLink(Feature item)
        {
            if (item.Links == null) return null;

            foreach (var link in item.Links)
            {
                if (!link.ExtensionData.TryGetValue("protocol", out var protocol)) continue;

                var protocolAsString = (string)protocol;
                if (string.IsNullOrEmpty(protocolAsString)) continue;
                
                if (protocolAsString.Equals("OGC:API features", StringComparison.OrdinalIgnoreCase))
                {
                    return link;
                }
            }

            return null;
        }

        private string DetermineLinkMediaType(Link endpoint)
        {
            if (endpoint.ExtensionData.TryGetValue("protocol", out var protocol))
            {
                return IsoCodeLists.ProtocolToMediaType((string)protocol);
            }

            return endpoint.Type;
        }
    }
}
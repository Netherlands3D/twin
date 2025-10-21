using System;
using System.Linq;
using System.Threading.Tasks;
using Netherlands3D.Catalogs.CatalogItems;
using Netherlands3D.OgcApi;
using Netherlands3D.OgcApi.Features;
using UnityEngine;

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
            // Doesn't have a protocol attribute, then this is not what we are looking for
            if (!link.ExtensionData.TryGetValue("protocol", out var protocolObj)) return false;
            
            // The protocol attribute is not set, then this is not what we are looking for
            string protocol = (string)protocolObj;
            if (string.IsNullOrEmpty(protocol)) return false;
            
            return protocol.Equals("OGC:API features", StringComparison.OrdinalIgnoreCase);
        }

        public override async Task<ICatalogItem> ParseFeature(Feature feature)
        {
            // This strategy deals with API Features as supplied by PDOK - as such we do not bother with the
            // base implementation of Parse Feature but directly return a catalog link
            var endpoint = FindEndpointLink(feature);
            
            return await OgcApiCatalog.CreateAsync(endpoint.Href);
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
using System;
using System.Linq;
using JetBrains.Annotations;
using Netherlands3D.Catalogs.CatalogItems;
using Netherlands3D.OgcApi;
using Netherlands3D.OgcApi.Features;
using Newtonsoft.Json.Linq;

namespace Netherlands3D.Catalogs.Catalogs.Strategies
{
    public class PyCswOgcApiRecordsStrategy : OgcApiRecordsStrategy
    {
        public PyCswOgcApiRecordsStrategy(ConformanceDeclaration conformance) : base(conformance)
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
            
            return ContainsPyCswServiceLink(feature);
        }

        private static bool ContainsPyCswServiceLink(Feature feature)
        {
            return feature.Links.Any(IsPyCswServiceLink);
        }

        // PyCSW often has `rel` as null for service links and includes `protocol` instead
        private static bool IsPyCswServiceLink(Link link)
        {
            return link.Rel == null 
               && link.ExtensionData != null 
               && link.ExtensionData.TryGetValue("protocol", out var protocolObj) 
               && (((string)protocolObj)?.StartsWith("OGC:", StringComparison.OrdinalIgnoreCase) ?? false);
        }

        public override bool TryParseFeature(Feature feature, out ICatalogItem catalogItem)
        {
            if (!base.TryParseFeature(feature, out catalogItem)) return false;

            // if it is not a record, we don't need to do anything else
            if (catalogItem is not RecordItem recordItem) return true;


            var endpoint = FindEndpointLink(feature);
            var type = DetermineLinkMediaType(endpoint);

            JToken protocol = null;
            endpoint?.ExtensionData.TryGetValue("protocol", out protocol);
            
            recordItem.Url = endpoint != null ? new Uri(endpoint.Href) : null;
            recordItem.MediaType = type;
            recordItem.Protocol = (string)protocol;

            return true;
        }

        [CanBeNull]
        private static Link FindEndpointLink(Feature item)
        {
            if (item.Links == null) return null;

            Link result = null;
            foreach (var link in item.Links.Where(link => link.Rel == null))
            {
                var foundProtocol = link.ExtensionData.TryGetValue("protocol", out var protocol);
                    
                // the first link that doesn't have a rel or protocol attribute, might be our endpoint link, but
                // we prefer the one with protocol as that will inform us what type of endpoint it is.
                if (!foundProtocol && result == null)
                {
                    result = link;
                }
                    
                // We found an endpoint link with a protocol attribute pointing to an OGC protocol!
                if (protocol != null && ((string)protocol)!.StartsWith("OGC:"))
                {
                    return link;
                }
            }

            return result;
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
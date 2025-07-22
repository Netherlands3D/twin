using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GeoJSON.Net.Feature;
using KindMen.Uxios.Api;
using Netherlands3D.OgcApi.ExtensionMethods;
using Netherlands3D.OgcApi.JsonConverters;
using Netherlands3D.OgcApi.Pagination;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Netherlands3D.OgcApi
{
    [JsonObject]
    public class Collection
    {
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; }

        [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
        public string? Title { get; set; }

        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string? Description { get; set; }

        [JsonProperty("links", Required = Required.Always)]
        public Link[] Links { get; set; }

        [JsonProperty("extent", NullValueHandling = NullValueHandling.Ignore)]
        public Extent? Extent { get; set; }

        [JsonProperty("itemType", NullValueHandling = NullValueHandling.Ignore)]
        public string ItemType { get; set; } = CollectionTypes.Default;

        [JsonProperty("crs", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(NormalizeToArrayConverter<string>))]
        public string[]? Crs { get; set; } = {
            "http://www.opengis.net/def/crs/OGC/1.3/CRS84"
        };

        [JsonExtensionData]
        public IDictionary<string, JToken> ExtensionData { get; set; } = new Dictionary<string, JToken>();

        public async Task<Results<FeatureCollection>> Fetch(int? limit = null, int? offset = null)
        {
            var itemsLink = Links.FirstBy(RelationTypes.items, Formats.geojson)?.Href;

            Uri uri = null;
            if (itemsLink != null)
            {
                uri = new Uri(itemsLink);
            }
            
            // Some API's don't follow the spec, so we are going to infer as a last resort
            if (uri == null)
            {
                itemsLink = Links.FirstBy(RelationTypes.self, Formats.json)?.Href;
                
                if (itemsLink == null) throw new Exception("Unable to fetch collection items, no links found");

                var uriBuilder = new UriBuilder(itemsLink);
                uriBuilder.Path += "/items";
                uri = uriBuilder.Uri;
            }

            var resource = new Resource<Results<FeatureCollection>>(uri);
            if (offset != null) resource.With("offset", offset.ToString());
            if (limit != null) resource.With("limit", limit.ToString());

            return await resource.Value;
        }
    }
}
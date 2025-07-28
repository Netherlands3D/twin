using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using KindMen.Uxios.Api;
using Netherlands3D.OgcApi.ExtensionMethods;
using Netherlands3D.OgcApi.Features;
using Netherlands3D.OgcApi.JsonConverters;
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
        [CanBeNull]
        public string Title { get; set; }

        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        [CanBeNull]
        public string Description { get; set; }

        [JsonProperty("links", Required = Required.Always)]
        public Link[] Links { get; set; }

        [JsonProperty("extent", NullValueHandling = NullValueHandling.Ignore)]
        [CanBeNull]
        public Extent Extent { get; set; }

        [JsonProperty("itemType", NullValueHandling = NullValueHandling.Ignore)]
        public string ItemType { get; set; } = CollectionTypes.Default;

        [JsonProperty("crs", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(NormalizeToArrayConverter<Uri>))]
        [CanBeNull]
        public Uri[] Crs { get; set; } = {
            new("http://www.opengis.net/def/crs/OGC/1.3/CRS84")
        };

        [JsonExtensionData]
        public IDictionary<string, JToken> ExtensionData { get; set; } = new Dictionary<string, JToken>();

        public async Task<FeatureCollection> FetchItems(int? limit = null, int? offset = null)
        {
            var uri = GetItemsUriBuilder().Uri;

            var resource = new Resource<FeatureCollection>(uri);
            if (offset != null) resource.With("offset", offset.ToString());
            if (limit != null) resource.With("limit", limit.ToString());

            return await resource.Value;
        }

        public async Task<Feature> FetchItemById(string itemId)
        {
            var itemUri = GetItemUriBuilder(itemId).Uri;
            var resource = new Resource<Feature>(itemUri);

            return await resource.Value;
        }

        private UriBuilder GetItemsUriBuilder()
        {
            var itemsLink = Links.FirstBy(RelationTypes.items, Formats.geojson)?.Href;
            if (!string.IsNullOrEmpty(itemsLink)) return new UriBuilder(itemsLink);

            // Some API's don't follow the spec, so we are going to infer as a last resort
            itemsLink = Links.FirstBy(RelationTypes.self, Formats.json)?.Href;
            if (string.IsNullOrEmpty(itemsLink)) throw new Exception("Unable to fetch collection items, no links found");

            return AppendPathSegment(new UriBuilder(itemsLink), "items");
        }

        private UriBuilder GetItemUriBuilder(string itemId)
        {
            // Technically, we could iterate over the results of the FetchItems method, but because of the paging and
            // all this is more efficient and the url is predictable so no harm.
            return AppendPathSegment(GetItemsUriBuilder(), itemId);
        }

        private static UriBuilder AppendPathSegment(UriBuilder uriBuilder, string segment)
        {
            // Using UriBuilder to alter the path will ensure all other aspects - such as anchor and query string -
            // remain unaltered
            uriBuilder.Path += $"/{segment}";
            
            return uriBuilder;
        }
    }
}
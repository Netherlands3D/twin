using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Netherlands3D.Catalogs.CatalogItems
{
    public record RecordItem : BaseCatalogItem
    {
        /// <summary>
        /// https://docs.geostandaarden.nl/md/mdprofiel-iso19115/#url
        /// May also be a Uri to a locally embedded file using the `project://` scheme, or addressable asset using
        /// the prefix `addressable://`.
        /// </summary>
        [CanBeNull]
        public Uri Url { get; private set; }

        /// <summary>
        /// Describes the type of data associated to this record described by the Url.
        /// 
        /// https://docs.geostandaarden.nl/md/mdprofiel-iso19115/#protocol
        /// </summary>
        [CanBeNull]
        public string Protocol { get; private set; }

        /// <summary>
        /// Describes the media type of data associated to this record described by the Url.
        /// 
        /// https://docs.geostandaarden.nl/md/mdprofiel-iso19115/#codelist-mediatypes
        /// </summary>
        [CanBeNull]
        public string MediaType { get; private set; }

        public RecordItem(
            string id, 
            string title, 
            string description = null,
            IDictionary<string, object> metadata = null,
            [CanBeNull] Uri url = null, 
            [CanBeNull] string protocol = null, 
            [CanBeNull] string mediaType = null
        ) : base(id, title, description, metadata) {
            WithEndpoint(url, protocol, mediaType);
        }

        public void WithEndpoint(Uri uri, string mediaType, string protocol)
        {
            Url = uri;
            Protocol = protocol;
            MediaType = mediaType;
        }
    }
}
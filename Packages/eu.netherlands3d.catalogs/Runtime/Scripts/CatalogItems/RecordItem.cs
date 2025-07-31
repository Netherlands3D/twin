using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Netherlands3D.Catalogs.CatalogItems
{
    public class RecordItem : ICatalogItem
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }

        /// <summary>
        /// Arbitrary metadata dictionary for catalog-specific properties 
        /// (e.g. bounding box, keywords, service operations, etc.)
        /// </summary>
        public IDictionary<string, object> Metadata { get; } = new Dictionary<string, object>();

        /// <summary>
        /// https://docs.geostandaarden.nl/md/mdprofiel-iso19115/#url
        /// May also be a Uri to a locally embedded file using the `project://` scheme, or addressable asset using
        /// the prefix `addressable://`.
        /// </summary>
        [CanBeNull]
        public Uri Url { get; set; }

        /// <summary>
        /// Describes the type of data associated to this record described by the Url.
        /// 
        /// https://docs.geostandaarden.nl/md/mdprofiel-iso19115/#protocol
        /// </summary>
        [CanBeNull]
        public string Type { get; set; }
    }
}
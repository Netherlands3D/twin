using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Netherlands3D.Catalogs.CatalogItems
{
    /// <summary>
    /// Describes the catalog item for a service or process with which an action can be initiated.
    ///
    /// Warning: this item is experimental and susceptible to change once we add support for more processes.
    /// </summary>
    public record DataService : BaseCatalogItem
    {
        /// <summary>
        /// May also be a Uri to a locally embedded process using the `event://` scheme, or to a remote process using
        /// a well-formed url.
        /// </summary>
        [CanBeNull]
        public Uri Endpoint { get; private set; }

        public DataService(
            string id, 
            string title, 
            string description = null,
            IDictionary<string, object> metadata = null,
            [CanBeNull] Uri endpoint = null 
        ) : base(id, title, description, metadata) {
            WithEndpoint(endpoint);
        }

        public void WithEndpoint(Uri uri)
        {
            Endpoint = uri;
        }
    }
}
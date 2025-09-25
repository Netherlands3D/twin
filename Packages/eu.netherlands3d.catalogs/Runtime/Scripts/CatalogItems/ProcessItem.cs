using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Netherlands3D.Catalogs.CatalogItems
{
    /// <summary>
    /// Describes the catalog item for a process with which an action can be initiated.
    ///
    /// Warning: this item is experimental and susceptible to change once we add support for more processes.
    /// </summary>
    public record ProcessItem : BaseCatalogItem
    {
        /// <summary>
        /// May also be a Uri to a locally embedded process using the `event://` scheme, or to a remote process using
        /// a well-formed url.
        /// </summary>
        [CanBeNull]
        public Uri ProcessAddress { get; private set; }

        public ProcessItem(
            string id, 
            string title, 
            string description = null,
            IDictionary<string, object> metadata = null,
            [CanBeNull] Uri processAddress = null 
        ) : base(id, title, description, metadata) {
            WithAddress(processAddress);
        }

        public void WithAddress(Uri uri)
        {
            ProcessAddress = uri;
        }
    }
}
using System.Collections.Generic;

namespace Netherlands3D.Catalogs.CatalogItems
{
    public record BaseCatalogItem : ICatalogItem
    {
        public string Id { get; }
        public string Title { get; }
        public string Description { get; }
        
        /// <summary>
        /// Arbitrary metadata dictionary for catalog-specific properties 
        /// (e.g. bounding box, keywords, service operations, etc.)
        /// </summary>
        public IDictionary<string, object> Metadata { get; }

        public BaseCatalogItem(string id, string title, string description, IDictionary<string, object> metadata = null)
        {
            Id = id;
            Title = title;
            Description = description;
            Metadata = metadata ?? new Dictionary<string, object>();
        }
    }
}
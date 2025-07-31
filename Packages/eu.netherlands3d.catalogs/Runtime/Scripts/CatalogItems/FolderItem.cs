using System.Collections.Generic;
using System.Threading.Tasks;
using Netherlands3D.SerializableGisExpressions;

namespace Netherlands3D.Catalogs.CatalogItems
{
    /// <summary>
    /// A folder in the catalog that contains its own paginated children.
    /// Implements IPaginatedRecordCollection so you can directly page through it.
    /// </summary>
    public class FolderItem : ICatalogItem, ICatalogItemCollection
    {
        public string Id { get; }
        public string Title { get; }
        public string Description { get; }

        public IDictionary<string, object> Metadata { get; } = new Dictionary<string, object>();

        private readonly ICatalogItemCollection children;

        public FolderItem(
            string id,
            string title,
            string description,
            ICatalogItemCollection children)
        {
            Id = id;
            Title = title;
            Description = description;
            this.children = children;
        }

        public Task<IEnumerable<ICatalogItem>> GetItemsAsync() => children.GetItemsAsync();
        public Task<ICatalogItemCollection> SearchAsync(string query, Pagination pagination = null) 
            => children.SearchAsync(query, pagination);
        public Task<ICatalogItemCollection> SearchAsync(Expression expression, Pagination pagination = null) 
            => children.SearchAsync(expression, pagination);

        public bool HasNextPage => children.HasNextPage;
        public bool HasPreviousPage => children.HasPreviousPage;
        public bool IsFirstPage => children.IsFirstPage;
        public bool IsLastPage => children.IsLastPage;

        public Task<ICatalogItemCollection> GetNextPageAsync() => children.GetNextPageAsync();
        public Task<ICatalogItemCollection> GetPreviousPageAsync() => children.GetPreviousPageAsync();
    }
}
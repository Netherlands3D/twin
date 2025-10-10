using System.Collections.Generic;
using System.Threading.Tasks;
using Netherlands3D.SerializableGisExpressions;

namespace Netherlands3D.Catalogs.CatalogItems
{
    public record BaseCatalogItemWithChildren : BaseCatalogItem, ICatalogItemCollection
    {
        private ICatalogItemCollection children;

        public BaseCatalogItemWithChildren(
            string id,
            string title,
            string description,
            ICatalogItemCollection children
        ) : base(id, title, description)
        {
            this.children = children;
        }

        public bool HasNextPage => children.HasNextPage;
        public bool HasPreviousPage => children.HasPreviousPage;
        public bool IsFirstPage => children.IsFirstPage;
        public bool IsLastPage => children.IsLastPage;

        public Task<ICatalogItem> GetAsync(string id) => children.GetAsync(id);

        public Task<IEnumerable<ICatalogItem>> GetItemsAsync() => children.GetItemsAsync();

        public Task<ICatalogItemCollection> SearchAsync(string query, Pagination pagination = null) 
            => children.SearchAsync(query, pagination);

        public Task<ICatalogItemCollection> SearchAsync(Expression expression, Pagination pagination = null) 
            => children.SearchAsync(expression, pagination);

        public Task<ICatalogItemCollection> GetNextPageAsync() => children.GetNextPageAsync();
        public Task<ICatalogItemCollection> GetPreviousPageAsync() => children.GetPreviousPageAsync();
    }
}
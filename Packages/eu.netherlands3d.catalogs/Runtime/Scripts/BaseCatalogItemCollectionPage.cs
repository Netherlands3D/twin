using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Netherlands3D.SerializableGisExpressions;

namespace Netherlands3D.Catalogs
{
    public abstract class BaseCatalogItemCollectionPage<TSource> : ICatalogItemCollection
    {
        protected readonly TSource source;
        protected readonly Pagination pagination;

        public bool HasPreviousPage => pagination.Offset > 0;
        public bool HasNextPage => pagination.Offset + pagination.Limit < MaxNumberOfItems;

        public bool IsFirstPage => !HasPreviousPage;
        public bool IsLastPage => !HasNextPage;

        protected abstract int MaxNumberOfItems { get; }

        protected BaseCatalogItemCollectionPage(TSource source, Pagination pagination)
        {
            this.source = source;
            this.pagination = pagination ?? new Pagination();
        }

        public abstract Task<ICatalogItem> GetAsync(string id);

        public abstract Task<IEnumerable<ICatalogItem>> GetItemsAsync();
        
        public abstract Task<ICatalogItemCollection> SearchAsync(string query, Pagination pagination = null);

        public abstract Task<ICatalogItemCollection> SearchAsync(Expression expression, Pagination pagination = null);

        protected abstract Task<BaseCatalogItemCollectionPage<TSource>> CreatePageAsyncInternal(TSource source, Pagination pagination);

        public async Task<ICatalogItemCollection> GetNextPageAsync()
        {
            if (!HasNextPage) throw new InvalidOperationException("No next page available.");
            return await CreatePageAsyncInternal(source, pagination.Next());
        }

        public async Task<ICatalogItemCollection> GetPreviousPageAsync()
        {
            if (!HasPreviousPage) throw new InvalidOperationException("No previous page available.");
            return await CreatePageAsyncInternal(source, pagination.Previous());
        }
    }
}
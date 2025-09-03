using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Netherlands3D.Catalogs.CatalogItems;
using Netherlands3D.SerializableGisExpressions;
using ExpressionEvaluator = Netherlands3D.SerializableGisExpressions.ExpressionEvaluator;

namespace Netherlands3D.Catalogs.Catalogs
{
    public class InMemoryCatalog : IWritableCatalog, ISearchable
    {
        public string Id { get; }
        public string Title { get; }
        public string Description { get; }
        public IDictionary<string, object> Metadata { get; }
        
        private readonly List<ICatalogItem> allRecords;

        public InMemoryCatalog(
            string id, 
            string title, 
            string description = null, 
            IEnumerable<ICatalogItem> records = null
        ) {
            Id = id;
            Title = title;
            Description = description;
            Metadata = new Dictionary<string, object>();
            allRecords = (records ?? Array.Empty<ICatalogItem>()).ToList();
        }
        
        public Task<ICatalogItemCollection> BrowseAsync(Pagination pagination = null)
        {
            var page = new CatalogItemCollectionPage(allRecords, pagination);

            return Task.FromResult<ICatalogItemCollection>(page);
        }
        
        public Task<ICatalogItem> GetAsync(string id)
        {
            return Task.FromResult(allRecords.FirstOrDefault(item => item.Id == id));
        }

        public Task<ICatalogItemCollection> SearchAsync(string query, Pagination pagination = null)
        {
            // Simple search: match on title or part thereof
            return SearchAsync(Expression.In(query, Expression.Get("Title")), pagination);
        }

        public Task<ICatalogItemCollection> SearchAsync(Expression expression, Pagination pagination = null)
        {
            var page = new CatalogItemCollectionPage(FilteredItems(allRecords), pagination);
            
            return Task.FromResult<ICatalogItemCollection>(page);

            // use a function yielding results to avoid allocations and linq 
            IEnumerable<ICatalogItem> FilteredItems(IEnumerable<ICatalogItem> items)
            {
                // Prepare the context and catalog item feature once and reuse it to reduce allocations.
                var catalogItemFeature = new CatalogItemFeature();
                var context = new ExpressionContext(catalogItemFeature);

                foreach (var item in items)
                {
                    catalogItemFeature.ReplaceCatalogItem(item);

                    if (ExpressionEvaluator.Evaluate(expression, context)) yield return item;
                }
            }
        }

        public void Add(ICatalogItem record)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));

            allRecords.Add(record);
        }

        public bool Remove(string id)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            
            return allRecords.RemoveAll(r => r.Id == id) > 0;
        }

        public void Clear() => allRecords.Clear();

        public static RecordItem CreateRecord(string id, string title, string description, Uri uri = null)
        {
            return new RecordItem(id, title, description, url: uri);
        }

        public static FolderItem CreateFolder(
            string id, 
            string title, 
            string description, 
            IEnumerable<ICatalogItem> records,
            Pagination pagination = null
        ) {
            return new FolderItem(id, title, description, new CatalogItemCollectionPage(records, pagination));       
        }
        
        public static ICatalogItem CreateDataset(
            string id, 
            string title, 
            string description, 
            IEnumerable<ICatalogItem> records,
            Pagination pagination = null
        ) {
            return new DataSetItem(id, title, description, new CatalogItemCollectionPage(records, pagination));       
        }

        private class CatalogItemFeature : IFeatureForExpression
        {
            private ICatalogItem CatalogItem { get; set; } = null;

            public object Geometry => null;

            public Dictionary<string, string> Attributes => null;

            public void ReplaceCatalogItem(ICatalogItem item)
            {
                CatalogItem = item;
            }

            public string GetAttribute(string attributeKey)
            {
                if (CatalogItem == null) return null;

                switch (attributeKey)
                {
                    case "Id": return CatalogItem.Id;
                    case "Title": return CatalogItem.Title;
                    case "Description": return CatalogItem.Description;

                    // .. otherwise try to grab it from the metadata
                    default:
                        CatalogItem.Metadata.TryGetValue(attributeKey, out var haystackValue);

                        return haystackValue?.ToString();
                }
            }
        }

        private class CatalogItemCollectionPage : BaseCatalogItemCollectionPage<List<ICatalogItem>>
        {
            private readonly List<ICatalogItem> items;
            private readonly Filter filter;
            protected override int MaxNumberOfItems => source.Count;

            public CatalogItemCollectionPage(IEnumerable<ICatalogItem> source, Pagination pagination)
                : base(source.ToList(), pagination)
            {
                this.filter = new Filter();
                items = this.source
                    .Skip(this.pagination.Offset)
                    .Take(this.pagination.Limit)
                    .ToList();
            }

            public override Task<IEnumerable<ICatalogItem>> GetItemsAsync()
                => Task.FromResult<IEnumerable<ICatalogItem>>(items);
            
            public override Task<ICatalogItemCollection> SearchAsync(string query, Pagination pagination = null)
            {
                // Simple search: match on title or part thereof
                return SearchAsync(Expression.In(query, Expression.Get("Title")), pagination);
            }

            public override Task<ICatalogItemCollection> SearchAsync(Expression expression, Pagination pagination = null)
            {
                var filteredCatalogItems = filter.Perform(source, expression);
                var page = new CatalogItemCollectionPage(filteredCatalogItems, pagination);
            
                return Task.FromResult<ICatalogItemCollection>(page);
            }

            protected override Task<BaseCatalogItemCollectionPage<List<ICatalogItem>>> CreatePageAsyncInternal(List<ICatalogItem> src, Pagination p)
                => Task.FromResult<BaseCatalogItemCollectionPage<List<ICatalogItem>>>(new CatalogItemCollectionPage(src, p));
            
            private class Filter
            {
                // Prepare the context and catalog item feature once and reuse it to reduce allocations.
                private readonly CatalogItemFeature reusableCatalogItemFeature = new();
                private readonly ExpressionContext reusableExpressionContext;

                public Filter()
                {
                    reusableExpressionContext = new ExpressionContext(reusableCatalogItemFeature);
                }

                // use a function yielding results to avoid allocations and linq 
                public IEnumerable<ICatalogItem> Perform(IEnumerable<ICatalogItem> itemsToFilter, Expression expression)
                {
                    foreach (var item in itemsToFilter)
                    {
                        reusableCatalogItemFeature.ReplaceCatalogItem(item);

                        if (ExpressionEvaluator.Evaluate(expression, reusableExpressionContext)) yield return item;
                    }
                }
            }
        }
    }
}
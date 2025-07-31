using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Netherlands3D.SerializableGisExpressions;
using ExpressionEvaluator = Netherlands3D.SerializableGisExpressions.ExpressionEvaluator;

namespace Netherlands3D.Catalogs
{
    public class InMemoryCatalog : IWritableCatalog
    {
        public string Id { get; }
        public string Title { get; }
        public string Description { get; }
        public IDictionary<string, object> Metadata { get; }
        
        private readonly List<ICatalogItem> allRecords;

        public InMemoryCatalog(string id, string title, string description, IEnumerable<ICatalogItem> records)
        {
            Id = id;
            Title = title;
            Description = description;
            Metadata = new Dictionary<string, object>();
            allRecords = records.ToList();
        }
        
        public Task<IPaginatedRecordCollection> BrowseAsync(Pagination pagination = null)
        {
            var page = new RecordCollectionPage(allRecords, pagination);

            return Task.FromResult<IPaginatedRecordCollection>(page);
        }

        public Task<IPaginatedRecordCollection> SearchAsync(string query, Pagination pagination = null)
        {
            // Simple search: match on title or part thereof
            return SearchAsync(Expression.In(query, Expression.Get("Title")), pagination);
        }

        public Task<IPaginatedRecordCollection> SearchAsync(Expression expression, Pagination pagination = null)
        {
            var page = new RecordCollectionPage(FilteredItems(allRecords), pagination);
            
            return Task.FromResult<IPaginatedRecordCollection>(page);

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
            return new RecordItem
            {
                Id = id,
                Title = title,
                Description = description,
                Url = uri
            };
        }

        public static FolderItem CreateFolder(
            string id, 
            string title, 
            string description, 
            IEnumerable<ICatalogItem> records,
            Pagination pagination = null
        ) {
            return new FolderItem(id, title, description, new RecordCollectionPage(records, pagination));       
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

        private class RecordCollectionPage : IPaginatedRecordCollection
        {
            private readonly List<ICatalogItem> source;
            private readonly Pagination pagination;
            private readonly List<ICatalogItem> items;

            public RecordCollectionPage(IEnumerable<ICatalogItem> source, Pagination pagination = null)
            {
                pagination ??= new Pagination();

                this.source = source.ToList();
                this.pagination = pagination;
                items = this.source.Skip(pagination.Offset).Take(pagination.Limit).ToList();
            }

            public bool HasPreviousPage => pagination.Offset > 0;
            public bool HasNextPage => pagination.Offset + pagination.Limit < source.Count;

            public bool IsFirstPage => !HasPreviousPage;
            public bool IsLastPage => !HasNextPage;

            public Task<IEnumerable<ICatalogItem>> GetItemsAsync()
                => Task.FromResult<IEnumerable<ICatalogItem>>(items);

            public Task<IPaginatedRecordCollection> GetNextPageAsync()
            {
                if (!HasNextPage) throw new InvalidOperationException("No next page available.");

                var nextPage = new RecordCollectionPage(source, pagination.Next());
                return Task.FromResult<IPaginatedRecordCollection>(nextPage);
            }

            public Task<IPaginatedRecordCollection> GetPreviousPageAsync()
            {
                if (!HasPreviousPage) throw new InvalidOperationException("No previous page available.");

                var prevPage = new RecordCollectionPage(source, pagination.Previous());
                return Task.FromResult<IPaginatedRecordCollection>(prevPage);
            }
        }
    }
}
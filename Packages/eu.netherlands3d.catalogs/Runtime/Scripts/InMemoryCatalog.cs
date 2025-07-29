using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Netherlands3D.SerializableGisExpressions;

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
        
        public Task<IPaginatedRecordCollection> BrowseAsync(int limit = 50, int offset = 0)
        {
            var page = new RecordCollectionPage(allRecords, limit, offset);

            return Task.FromResult<IPaginatedRecordCollection>(page);
        }

        public Task<IPaginatedRecordCollection> SearchAsync(Expression expression, int limit = 50, int offset = 0)
            => throw new NotImplementedException();

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

        public static RecordItem CreateRecord(string id, string title, string description)
        {
            return new RecordItem
            {
                Id = id,
                Title = title,
                Description = description
            };
        }

        public static FolderItem CreateFolder(
            string id, 
            string title, 
            string description, 
            IEnumerable<ICatalogItem> records, 
            int limit = 25, 
            int offset = 0
        ) {
            return new FolderItem(id, title, description, new RecordCollectionPage(records, limit, offset));       
        }

        private class RecordCollectionPage : IPaginatedRecordCollection
        {
            private readonly List<ICatalogItem> source;
            private readonly int limit;
            private readonly int offset;
            private readonly List<ICatalogItem> items;

            public RecordCollectionPage(IEnumerable<ICatalogItem> source, int limit, int offset)
            {
                this.source = source.ToList();
                this.limit = Math.Max(1, limit);
                this.offset = Math.Max(0, offset);
                items = this.source.Skip(this.offset).Take(this.limit).ToList();
            }

            public bool HasPreviousPage => offset > 0;
            public bool HasNextPage => offset + limit < source.Count;

            public bool IsFirstPage => !HasPreviousPage;
            public bool IsLastPage => !HasNextPage;

            public Task<IEnumerable<ICatalogItem>> GetItemsAsync()
                => Task.FromResult<IEnumerable<ICatalogItem>>(items);

            public Task<IPaginatedRecordCollection> GetNextPageAsync()
            {
                if (!HasNextPage) throw new InvalidOperationException("No next page available.");

                var nextPage = new RecordCollectionPage(source, limit, offset + limit);
                return Task.FromResult<IPaginatedRecordCollection>(nextPage);
            }

            public Task<IPaginatedRecordCollection> GetPreviousPageAsync()
            {
                if (!HasPreviousPage) throw new InvalidOperationException("No previous page available.");

                var prevOffset = Math.Max(0, offset - limit);
                var prevPage = new RecordCollectionPage(source, limit, prevOffset);
                return Task.FromResult<IPaginatedRecordCollection>(prevPage);
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Netherlands3D.SerializableGisExpressions;

namespace Netherlands3D.Catalogs
{
    public class InMemoryCatalog : IWritableCatalog
    {
        private readonly List<Record> allRecords;

        public InMemoryCatalog(IEnumerable<Record> records)
        {
            allRecords = records.ToList();
        }

        public Task<IRecordCollection> BrowseAsync(int limit = 50, int offset = 0)
        {
            var page = new InMemoryPage(allRecords, limit, offset);

            return Task.FromResult<IRecordCollection>(page);
        }

        public Task<IRecordCollection> SearchAsync(Expression expression, int limit = 50, int offset = 0)
            => throw new NotImplementedException();

        public void Add(Record record)
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

        private class InMemoryPage : IPaginatedRecordCollection
        {
            private readonly List<Record> source;
            private readonly int limit;
            private readonly int offset;
            private readonly List<Record> items;

            public InMemoryPage(IEnumerable<Record> source, int limit, int offset)
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

            public Task<IEnumerable<Record>> GetItemsAsync()
                => Task.FromResult<IEnumerable<Record>>(items);

            public Task<IPaginatedRecordCollection> GetNextPageAsync()
            {
                if (!HasNextPage) throw new InvalidOperationException("No next page available.");

                var nextPage = new InMemoryPage(source, limit, offset + limit);
                return Task.FromResult<IPaginatedRecordCollection>(nextPage);
            }

            public Task<IPaginatedRecordCollection> GetPreviousPageAsync()
            {
                if (!HasPreviousPage) throw new InvalidOperationException("No previous page available.");

                var prevOffset = Math.Max(0, offset - limit);
                var prevPage = new InMemoryPage(source, limit, prevOffset);
                return Task.FromResult<IPaginatedRecordCollection>(prevPage);
            }
        }
    }
}
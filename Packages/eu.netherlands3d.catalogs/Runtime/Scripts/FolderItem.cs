using System.Collections.Generic;
using System.Threading.Tasks;

namespace Netherlands3D.Catalogs
{
    /// <summary>
    /// A folder in the catalog that contains its own paginated children.
    /// Implements IPaginatedRecordCollection so you can directly page through it.
    /// </summary>
    public class FolderItem : ICatalogItem, IPaginatedRecordCollection
    {
        public string Id { get; }
        public string Title { get; }
        public string Description { get; }

        public IDictionary<string, object> Metadata { get; } = new Dictionary<string, object>();

        private readonly IPaginatedRecordCollection children;

        public FolderItem(
            string id,
            string title,
            string description,
            IPaginatedRecordCollection children)
        {
            Id = id;
            Title = title;
            Description = description;
            this.children = children;
        }

        public Task<IEnumerable<ICatalogItem>> GetItemsAsync() => children.GetItemsAsync();

        public bool HasNextPage => children.HasNextPage;
        public bool HasPreviousPage => children.HasPreviousPage;
        public bool IsFirstPage => children.IsFirstPage;
        public bool IsLastPage => children.IsLastPage;

        public Task<IPaginatedRecordCollection> GetNextPageAsync() => children.GetNextPageAsync();
        public Task<IPaginatedRecordCollection> GetPreviousPageAsync() => children.GetPreviousPageAsync();
    }
}
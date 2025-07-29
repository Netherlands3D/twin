using System.Threading.Tasks;

namespace Netherlands3D.Catalogs
{
    public interface IPaginatedRecordCollection : IRecordCollection
    {
        /// <summary>
        /// Convenience property to check if this is the first page.
        /// </summary>
        public bool IsFirstPage => !HasPreviousPage;

        /// <summary>
        /// Convenience property to check if this is the last page.
        /// </summary>
        public bool IsLastPage => !HasNextPage;

        public bool HasNextPage { get; }
        public bool HasPreviousPage { get; }
        public Task<IPaginatedRecordCollection> GetNextPageAsync();
        public Task<IPaginatedRecordCollection> GetPreviousPageAsync();
    }
}
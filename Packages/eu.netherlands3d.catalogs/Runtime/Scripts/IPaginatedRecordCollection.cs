using System.Threading.Tasks;

namespace Netherlands3D.Catalogs
{
    /// <summary>
    /// Represents a single “page” of <see cref="Record"/> results returned by a catalog.
    /// </summary>
    /// <remarks>
    /// Different catalog implementations will supply their own paging logic:
    ///  – <b>InMemoryCatalog</b> uses <c>InMemoryRecordCollectionPage</c>, which slices a local list.
    ///  – <b>OgcApiCatalog</b> (for OGC API Records) will issue HTTP requests for each page.
    ///  – Other adapters may stream from disk, query a database, or chain multiple backends.
    ///  
    /// Consumers can always treat pages uniformly:
    /// ```csharp
    /// var page = await catalog.BrowseAsync();
    /// foreach(var record in await page.GetItemsAsync()) { … }
    /// if (page.HasNextPage)
    ///     page = await page.GetNextPageAsync();
    /// ```
    public interface IPaginatedRecordCollection : IRecordCollection
    {
        /// <summary>
        /// Indicates whether this page is the very first page of results.
        /// </summary>
        public bool IsFirstPage => !HasPreviousPage;

        /// <summary>
        /// Indicates whether this page is the very last page of results.
        /// </summary>
        public bool IsLastPage => !HasNextPage;

        /// <summary>
        /// True if there is another page of results after this one.
        /// Calling <see cref="GetNextPageAsync"/> is valid only when this is true.
        /// </summary>
        public bool HasNextPage { get; }

        /// <summary>
        /// True if there is a previous page of results before this one.
        /// Calling <see cref="GetPreviousPageAsync"/> is valid only when this is true.
        /// </summary>
        public bool HasPreviousPage { get; }
        
        /// <summary>
        /// Retrieves the next page of records.
        /// Implementations may:
        ///  – Slice a pre-loaded list (in-memory).
        ///  – Issue a web request to fetch the next page (OGC API, CSW, etc.).
        ///  – Query a database or other backend.
        /// </summary>
        /// <returns>
        /// A <see cref="Task{TResult}"/> that completes with the subsequent
        /// <see cref="IPaginatedRecordCollection"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <see cref="HasNextPage"/> is false.
        /// </exception>
        public Task<IPaginatedRecordCollection> GetNextPageAsync();
        
        /// <summary>
        /// Retrieves the previous page of records.
        /// </summary>
        /// <remarks>
        /// Some implementations (e.g. streaming or forward‐only APIs) may choose
        /// to throw <see cref="NotSupportedException"/> instead of providing backward paging.
        /// </remarks>
        /// <returns>
        /// A <see cref="Task{TResult}"/> that completes with the preceding
        /// <see cref="IPaginatedRecordCollection"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <see cref="HasPreviousPage"/> is false.
        /// </exception>
        public Task<IPaginatedRecordCollection> GetPreviousPageAsync();
    }
}
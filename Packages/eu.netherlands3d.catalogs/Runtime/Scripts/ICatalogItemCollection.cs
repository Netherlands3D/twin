using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Netherlands3D.Catalogs
{
    /// <summary>
    /// Represents a single “page” of <see cref="CatalogItem"/> results returned by a catalog.
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
    /// foreach(var catalogItem in await page.GetItemsAsync()) { … }
    /// if (page.HasNextPage)
    ///     page = await page.GetNextPageAsync();
    /// ```
    public interface ICatalogItemCollection : ISearchable
    {
        public Task<IEnumerable<ICatalogItem>> GetItemsAsync();

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
        /// Retrieves the next page of catalog items.
        /// Implementations may:
        ///  – Slice a pre-loaded list (in-memory).
        ///  – Issue a web request to fetch the next page (OGC API, CSW, etc.).
        ///  – Query a database or other backend.
        /// </summary>
        /// <returns>
        /// A <see cref="Task{TResult}"/> that completes with the subsequent
        /// <see cref="ICatalogItemCollection"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <see cref="HasNextPage"/> is false.
        /// </exception>
        public Task<ICatalogItemCollection> GetNextPageAsync();
        
        /// <summary>
        /// Retrieves the previous page of catalog items.
        /// </summary>
        /// <remarks>
        /// Some implementations (e.g. streaming or forward‐only APIs) may choose
        /// to throw <see cref="NotSupportedException"/> instead of providing backward paging.
        /// </remarks>
        /// <returns>
        /// A <see cref="Task{TResult}"/> that completes with the preceding
        /// <see cref="ICatalogItemCollection"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <see cref="HasPreviousPage"/> is false.
        /// </exception>
        public Task<ICatalogItemCollection> GetPreviousPageAsync();
    }
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;
using KindMen.Uxios.Api;
using Netherlands3D.Catalogs.CatalogItems;
using Netherlands3D.SerializableGisExpressions;
using UnityEngine;

namespace Netherlands3D.Catalogs.Catalogs
{
    /// <summary>
    /// An <see cref="ICatalog"/> implementation backed by an OGC Catalogue Service for the Web (CSW 2.0.2) endpoint.
    ///
    /// CSW is an OGC standard for exposing catalogues of geospatial resources over HTTP using XML request/response
    /// messages. This implementation uses the KVP (Key-Value Pair) HTTP GET binding.
    ///
    /// Supported operations:
    /// - GetCapabilities  – used during construction to populate Id, Title and Description.
    /// - GetRecords       – used to browse and search catalogue records.
    ///
    /// Keyword search is translated to a CQL_TEXT constraint of the form
    /// <c>AnyText LIKE '%keyword%'</c> which is widely supported across CSW implementations.
    ///
    /// Expression-based search (<see cref="SearchAsync(Expression, Pagination)"/>) is not supported and
    /// will throw a <see cref="NotSupportedException"/>.
    /// </summary>
    public class OgcCswCatalog : ICatalog
    {
        private const string CswVersion = "2.0.2";

        private readonly string _baseUrl;

        public string Id { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }
        public IDictionary<string, object> Metadata { get; } = new Dictionary<string, object>();

        private OgcCswCatalog(string baseUrl)
        {
            _baseUrl = baseUrl;
        }

        /// <summary>
        /// Asynchronously creates and initialises an <see cref="OgcCswCatalog"/> by fetching the service's
        /// GetCapabilities document. The <paramref name="url"/> may be a bare endpoint URL or a full
        /// GetCapabilities URL; any existing query parameters are stripped before appending the CSW ones.
        /// </summary>
        public static async Task<OgcCswCatalog> CreateAsync(string url)
        {
            var baseUrl = NormalizeToCswBaseUrl(url);
            var catalog = new OgcCswCatalog(baseUrl);
            await catalog.LoadCapabilitiesAsync();
            return catalog;
        }

        /// <summary>
        /// Strip any existing query string so we always talk to the bare CSW endpoint.
        /// </summary>
        private static string NormalizeToCswBaseUrl(string url)
        {
            var uriBuilder = new UriBuilder(url);
            uriBuilder.Query = string.Empty;
            return uriBuilder.Uri.ToString().TrimEnd('/');
        }

        private async Task LoadCapabilitiesAsync()
        {
            var uri = new Uri($"{_baseUrl}?SERVICE=CSW&REQUEST=GetCapabilities");

            try
            {
                var xml = await new Resource<string>(uri).Value;

                var doc = new XmlDocument();
                doc.LoadXml(xml);

                // Use local-name() so we are namespace-prefix-agnostic
                var serviceId = doc.SelectSingleNode("//*[local-name()='ServiceIdentification']");

                Id = _baseUrl;
                Title = serviceId?.SelectSingleNode("*[local-name()='Title']")?.InnerText ?? _baseUrl;
                Description = serviceId?.SelectSingleNode("*[local-name()='Abstract']")?.InnerText ?? string.Empty;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"OgcCswCatalog: Failed to parse GetCapabilities for '{_baseUrl}': {e.Message}");
                Id = _baseUrl;
                Title = _baseUrl;
                Description = string.Empty;
            }
        }

        /// <inheritdoc />
        public Task<ICatalogItemCollection> BrowseAsync(Pagination pagination = null)
        {
            pagination ??= new Pagination();
            return Task.FromResult<ICatalogItemCollection>(new RecordsPage(_baseUrl, keyword: null, pagination));
        }

        /// <inheritdoc />
        public Task<ICatalogItemCollection> SearchAsync(string query, Pagination pagination = null)
        {
            pagination ??= new Pagination();
            return Task.FromResult<ICatalogItemCollection>(new RecordsPage(_baseUrl, query, pagination));
        }

        /// <inheritdoc />
        /// <exception cref="NotSupportedException">
        /// CSW does not support structured expression queries; use <see cref="SearchAsync(string, Pagination)"/>
        /// for keyword-based searches instead.
        /// </exception>
        public Task<ICatalogItemCollection> SearchAsync(Expression expression, Pagination pagination = null)
        {
            throw new NotSupportedException(
                "OGC CSW does not support structured expression queries. " +
                "Use SearchAsync(string query) for keyword-based searches instead."
            );
        }

        private class RecordsPage : BaseCatalogItemCollectionPage<CswRecordsSource>
        {
            // Updated after the first GetItemsAsync() call
            private int _totalMatched;

            protected override int MaxNumberOfItems => _totalMatched;

            public RecordsPage(string baseUrl, string keyword, Pagination pagination)
                : base(new CswRecordsSource(baseUrl, keyword), pagination)
            {
            }

            private RecordsPage(CswRecordsSource source, Pagination pagination)
                : base(source, pagination)
            {
            }

            public override Task<ICatalogItem> GetAsync(string id)
            {
                throw new NotImplementedException();
            }

            public override async Task<IEnumerable<ICatalogItem>> GetItemsAsync()
            {
                var (records, matched) = await source.FetchAsync(pagination);
                _totalMatched = matched;
                return records;
            }

            public override Task<ICatalogItemCollection> SearchAsync(string query, Pagination page = null)
            {
                return Task.FromResult<ICatalogItemCollection>(
                    new RecordsPage(new CswRecordsSource(source.BaseUrl, query), page ?? new Pagination()));
            }

            public override Task<ICatalogItemCollection> SearchAsync(Expression expression, Pagination page = null)
            {
                throw new NotSupportedException(
                    "OGC CSW does not support structured expression queries. " +
                    "Use SearchAsync(string query) for keyword-based searches instead."
                );
            }

            protected override Task<BaseCatalogItemCollectionPage<CswRecordsSource>> CreatePageAsyncInternal(
                CswRecordsSource src, Pagination p)
            {
                return Task.FromResult<BaseCatalogItemCollectionPage<CswRecordsSource>>(new RecordsPage(src, p));
            }
        }

        private class CswRecordsSource
        {
            public string BaseUrl { get; }
            public string Keyword { get; }

            public CswRecordsSource(string baseUrl, string keyword)
            {
                BaseUrl = baseUrl;
                Keyword = keyword;
            }

            public async Task<(List<ICatalogItem> records, int totalMatched)> FetchAsync(Pagination pagination)
            {
                var uri = BuildGetRecordsUri(pagination);

                try
                {
                    var xml = await new Resource<string>(uri).Value;
                    return ParseGetRecordsResponse(xml);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"OgcCswCatalog: GetRecords request failed for '{BaseUrl}': {e.Message}");
                    return (new List<ICatalogItem>(), 0);
                }
            }

            /// <summary>
            /// Builds the KVP GetRecords request URI.
            /// CSW startPosition is 1-based; <see cref="Pagination.Offset"/> is 0-based.
            /// An optional CQL_TEXT keyword constraint is appended when <see cref="Keyword"/> is set.
            /// </summary>
            private Uri BuildGetRecordsUri(Pagination pagination)
            {
                var startPosition = pagination.Offset + 1;

                var query = $"SERVICE=CSW" +
                            $"&REQUEST=GetRecords" +
                            $"&VERSION={CswVersion}" +
                            $"&ElementSetName=full" +
                            $"&OUTPUTSCHEMA={Uri.EscapeDataString("http://www.opengis.net/cat/csw/2.0.2")}" +
                            $"&resultType=results" +
                            $"&typeNames={Uri.EscapeDataString("csw:Record")}" +
                            $"&maxRecords={pagination.Limit}" +
                            $"&startPosition={startPosition}";

                if (!string.IsNullOrWhiteSpace(Keyword))
                {
                    var constraint = Uri.EscapeDataString($"AnyText LIKE '%{Keyword}%'");
                    query += $"&CONSTRAINTLANGUAGE=CQL_TEXT" +
                             $"&CONSTRAINT_LANGUAGE_VERSION=1.1.0" +
                             $"&CONSTRAINT={constraint}";
                }

                return new Uri($"{BaseUrl}?{query}");
            }

            private static (List<ICatalogItem> records, int totalMatched) ParseGetRecordsResponse(string xml)
            {
                var doc = new XmlDocument();
                doc.LoadXml(xml);

                var searchResults = doc.SelectSingleNode("//*[local-name()='SearchResults']");
                if (searchResults == null)
                    return (new List<ICatalogItem>(), 0);

                int.TryParse(searchResults.Attributes?["numberOfRecordsMatched"]?.Value, out var totalMatched);

                var records = new List<ICatalogItem>();
                var recordNodes = searchResults.SelectNodes(
                    "*[local-name()='Record' or local-name()='SummaryRecord' or local-name()='BriefRecord']"
                );

                if (recordNodes != null)
                {
                    foreach (XmlNode record in recordNodes)
                    {
                        var item = ParseRecord(record);
                        if (item != null)
                            records.Add(item);
                    }
                }

                return (records, totalMatched);
            }

            /// <summary>
            /// Parses a single CSW record node (csw:Record / csw:SummaryRecord / csw:BriefRecord) into an
            /// <see cref="ICatalogItem"/>. Dublin Core elements are mapped to the standard catalog fields;
            /// all child elements are also stored in the metadata dictionary.
            /// </summary>
            private static ICatalogItem ParseRecord(XmlNode record)
            {
                var id = record.SelectSingleNode("*[local-name()='identifier']")?.InnerText
                         ?? Guid.NewGuid().ToString();
                var title = record.SelectSingleNode("*[local-name()='title']")?.InnerText;
                var description = record.SelectSingleNode("*[local-name()='abstract']")?.InnerText
                                  ?? record.SelectSingleNode("*[local-name()='description']")?.InnerText;

                Uri url = null;
                string protocol = null;
                string mediaType = null;
                var uriNode = record.SelectSingleNode("*[local-name()='URI']");
                if (uriNode != null)
                {
                    Uri.TryCreate(uriNode.InnerText.Trim(), UriKind.Absolute, out url);
                    protocol = uriNode.Attributes?["protocol"]?.Value;
                    mediaType = uriNode.Attributes?["name"]?.Value;
                }

                var metadata = new Dictionary<string, object>();
                foreach (XmlNode child in record.ChildNodes)
                {
                    if (!string.IsNullOrEmpty(child.LocalName) && !string.IsNullOrEmpty(child.InnerText))
                        metadata.TryAdd(child.LocalName, child.InnerText);
                }

                return new RecordItem(id, title, description, metadata, url, protocol, mediaType);
            }
        }
    }
}

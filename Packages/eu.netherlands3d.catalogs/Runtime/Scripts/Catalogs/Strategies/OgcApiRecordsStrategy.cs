using System;
using System.Linq;
using System.Threading.Tasks;
using Netherlands3D.Catalogs.CatalogItems;
using Netherlands3D.Catalogs.QueryTranslators;
using Netherlands3D.OgcApi;
using Netherlands3D.OgcApi.Features;
using Netherlands3D.SerializableGisExpressions;

namespace Netherlands3D.Catalogs.Catalogs.Strategies
{
    /// <summary>
    /// Represents a feature parsing and query strategy for an OGC API - Records implementation.
    ///
    /// OGC API - Records is a flexible and extensible standard, but in practice, many real-world
    /// implementations (such as PyCSW, GeoNetwork, CKAN, etc.) exhibit subtle or significant variations
    /// in how they expose their records, including:
    /// 
    /// - Link structure and semantics
    /// - Metadata encoding (e.g., ISO 19115, DCAT-AP, custom formats)
    /// - Filtering support and conformance claims
    ///
    /// This abstract base class defines the contract for a strategy that:
    /// 
    /// 1. Determines whether it can handle a given <see cref="Feature"/>.
    /// 2. Parses a feature into a normalized <see cref="ICatalogItem"/> (via <see cref="ParseFeature"/>).
    ///
    /// Strategy instances are typically used inside a dispatcher that selects the appropriate strategy
    /// based on heuristics applied to the feature data. This allows the same catalog to host and process
    /// records from different implementations without requiring hardcoded assumptions or prior knowledge
    /// about which variant is in use.
    ///
    /// ### Example Use Cases:
    /// 
    /// - A PyCSW-powered catalog may represent service endpoints using `links[]` without `rel` attributes,
    ///   but with a `protocol` field.
    /// - A GeoNetwork instance might use `rel="distribution"` with DCAT-style media types and missing `protocol`.
    /// - A CKAN instance might represent download links in a `resources[]` array rather than `links[]`.
    ///
    /// Implementations of this strategy should be registered with a dispatcher (e.g., <c>OgcApiStrategyDispatcher</c>)
    /// that selects the best strategy for each incoming record.
    ///
    /// <para>
    /// Typical strategy implementations:
    /// <list type="bullet">
    ///   <item><description><see cref="PyCswOgcApiStrategy"/></description></item>
    ///   <item><description><c>GeoNetworkOgcApiStrategy</c> (planned)</description></item>
    ///   <item><description><c>CkanOgcApiStrategy</c> (planned)</description></item>
    /// </list>
    /// </para>
    /// </summary>
    public abstract class OgcApiRecordsStrategy
    {
        private ConformanceDeclaration conformance;
        private readonly ExpressionToCqlJsonTranslator queryTranslator;

        public OgcApiRecordsStrategy(ConformanceDeclaration conformance)
        {
            this.conformance = conformance;

            queryTranslator = new ExpressionToCqlJsonTranslator();
        }

        public async Task<FeatureCollection> SearchAsync(Collection source, string keyword, Pagination pagination)
        {
            if (!conformance.Supports(
                    "http://www.opengis.net/spec/ogcapi-records-1/1.0/conf/record-core-query-parameters"))
            {
                return await SearchAsync(source, Expression.In(keyword, Expression.Get("Title")), pagination);
            }

            return await source.SearchUsingKeyword(
                keyword,
                pagination.Limit,
                pagination.Offset
            );
        }

        public async Task<FeatureCollection> SearchAsync(Collection source, Expression expression,
            Pagination pagination)
        {
            if (!conformance.Supports("http://www.opengis.net/spec/ogcapi-features-3/1.0/req/features-filter"))
            {
                throw new NotSupportedException("Filtering is not supported by this catalog.");
            }

            if (!conformance.Supports("http://www.opengis.net/spec/cql2/1.0/conf/cql2-json"))
            {
                throw new NotSupportedException("Search using CQL2/JSON is not supported by this catalog.");
            }

            var featureCollection = await source.SearchUsingCql(
                queryTranslator.ToQuery(expression),
                pagination.Limit,
                pagination.Offset
            );
            return featureCollection;
        }

        public abstract bool CanHandle(Feature feature);

        public virtual Task<ICatalogItem> ParseFeature(Feature feature)
        {
            if (!CanHandle(feature)) return Task.FromException<ICatalogItem>(new Exception("Unsupported feature type"));

            feature.Properties.TryGetValue("title", out var title);
            feature.Properties.TryGetValue("description", out var description);

            return Task.FromResult<ICatalogItem>(new RecordItem(
                feature.Id,
                title as string,
                description as string,
                feature.Properties.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            ));
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Netherlands3D.Catalogs.CatalogItems;
using Netherlands3D.Catalogs.QueryTranslators;
using Netherlands3D.OgcApi;
using Netherlands3D.OgcApi.Features;
using Netherlands3D.SerializableGisExpressions;
using UnityEngine;

namespace Netherlands3D.Catalogs.Catalogs
{
    /// <summary>
    /// Implementation of a PyCSW-flavoured OGC API.
    ///
    /// TODO: Once we start supporting more OGC API's, we need to extract the contents of this class to a new
    /// OgcApiCatalog and use "profiles" to determine how to read the metadata inside a Feature/FeatureCollection.
    /// At this moment, we postpone that until it becomes clearer how OGC API interactions will take place, and
    /// especially: how to determine which profile is used by an OGC API.
    /// 
    /// </summary>
    public class PyCswOgcApiCatalog : ICatalog
    {
        private readonly OgcApi.OgcApi ogcApi;
        public string Id { get; private set; }
        public string Title { get; protected set; }
        public string Description { get; protected set; }
        public IDictionary<string, object> Metadata { get; } = new Dictionary<string, object>();

        protected PyCswOgcApiCatalog(OgcApi.OgcApi ogcApi)
        {
            this.ogcApi = ogcApi;
        }

        /// <summary>
        /// Catalogs have a static factory method to create them as async information may be needed to populate
        /// their basic properties.
        /// </summary>
        public static async Task<PyCswOgcApiCatalog> CreateAsync(string url)
        {
            var ogcApi = new OgcApi.OgcApi(url);

            return new PyCswOgcApiCatalog(ogcApi)
            {
                Id = await ogcApi.Id(),
                Title = await ogcApi.Title(),
                Description = await ogcApi.Description()
            };
        }

        public async Task<ICatalogItemCollection> BrowseAsync(Pagination pagination = null)
        {
            return new LandingPage(
                this,
                await ogcApi.Collections(), 
                pagination
            );
        }

        public async Task<ICatalogItemCollection> SearchAsync(string query, Pagination pagination = null)
        {
            return await (await BrowseAsync(pagination)).SearchAsync(query, pagination);
        }

        public async Task<ICatalogItemCollection> SearchAsync(Expression expression, Pagination pagination = null)
        {
            return await (await BrowseAsync(pagination)).SearchAsync(expression, pagination);
        }
        
        private class LandingPage : BaseCatalogItemCollectionPage<Collections>
        {
            private readonly PyCswOgcApiCatalog catalog;
            private readonly Collection[] items;
            protected override int MaxNumberOfItems => items.Length;

            public LandingPage(
                PyCswOgcApiCatalog catalog, 
                Collections source, 
                Pagination pagination = null
            ) : base(source, pagination)
            {
                this.catalog = catalog;
                items = source.Items;
            }

            public override async Task<IEnumerable<ICatalogItem>> GetItemsAsync()
            {
                var result = new List<ICatalogItem>();
                foreach (var item in items)
                {
                    result.Add(
                        new FolderItem(
                            item.Id, 
                            item.Title, 
                            item.Description, 
                            await CollectionPage.CreateAsync(await catalog.ogcApi.Conformance(), item, pagination)
                        )
                    );
                }
                return result;
            }

            public override Task<ICatalogItemCollection> SearchAsync(string query, Pagination pagination = null)
            {
                throw new NotImplementedException();
            }
            
            public override Task<ICatalogItemCollection> SearchAsync(Expression expression, Pagination pagination = null)
            {
                throw new NotImplementedException();
            }
            
            protected override Task<BaseCatalogItemCollectionPage<Collections>> CreatePageAsyncInternal(Collections src, Pagination p)
                => Task.FromResult<BaseCatalogItemCollectionPage<Collections>>(new LandingPage(catalog, src, p));
        }

        private class CollectionPage : BaseCatalogItemCollectionPage<Collection>
        {
            private readonly ConformanceDeclaration conformance;
            private readonly FeatureCollection items;
            private readonly ExpressionToCqlJsonTranslator queryTranslator;
            protected override int MaxNumberOfItems => (int)items.NumberMatched;

            private CollectionPage(
                ConformanceDeclaration conformance, 
                Collection source, 
                FeatureCollection items, 
                Pagination pagination = null
            ) : base(source, pagination)
            {
                this.conformance = conformance;
                this.items = items;
                this.queryTranslator = new ExpressionToCqlJsonTranslator();
            }

            public static async Task<CollectionPage> CreateAsync(
                ConformanceDeclaration conformance, 
                Collection source, 
                Pagination pagination
            ) {
                return new CollectionPage(
                    conformance,
                    source, 
                    await source.FetchItems(pagination.Limit, pagination.Offset), 
                    pagination
                );
            }

            public override async Task<ICatalogItemCollection> SearchAsync(string keyword, Pagination pagination = null)
            {
                pagination ??= new Pagination();

                if (!conformance.Supports("http://www.opengis.net/spec/ogcapi-records-1/1.0/conf/record-core-query-parameters"))
                {
                    return await SearchAsync(Expression.In(keyword, Expression.Get("Title")), pagination);
                }

                var featureCollection = await source.SearchUsingKeyword(
                    keyword, 
                    pagination.Limit, 
                    pagination.Offset
                );

                return new CollectionPage(conformance, source, featureCollection, pagination);
            }

            public override async Task<ICatalogItemCollection> SearchAsync(Expression expression, Pagination pagination = null)
            {
                pagination ??= new Pagination();

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

                return new CollectionPage(conformance, source, featureCollection, pagination);

            }

            public override Task<IEnumerable<ICatalogItem>> GetItemsAsync()
            {
                var result = new List<ICatalogItem>();
                foreach (var item in items.Features)
                {
                    var endpoint = FindEndpointLink(item);
                    item.Properties.TryGetValue("title", out var title);
                    item.Properties.TryGetValue("description", out var description);
                    {
                        result.Add(new RecordItem
                        {
                            Id = item.Id,
                            Title = title as string,
                            Description = description as string,
                            Url = endpoint != null ? new Uri(endpoint.Href) : null
                        });
                    }
                }
                return Task.FromResult<IEnumerable<ICatalogItem>>(result);
            }

            protected override async Task<BaseCatalogItemCollectionPage<Collection>> CreatePageAsyncInternal(Collection src, Pagination p)
                => await CreateAsync(conformance, src, p);

            private static Link FindEndpointLink(Feature item)
            {
                if (item.Links == null) return null;
                
                return item.Links.SingleOrDefault(link =>
                    link.Rel == null &&
                    !link.ExtensionData.ContainsKey("protocol"));
            }
        }
    }
}
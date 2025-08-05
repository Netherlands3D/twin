using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Netherlands3D.Catalogs.CatalogItems;
using Netherlands3D.Catalogs.Catalogs.Strategies;
using Netherlands3D.OgcApi;
using Netherlands3D.OgcApi.Features;
using Netherlands3D.SerializableGisExpressions;

namespace Netherlands3D.Catalogs.Catalogs
{
    public class OgcApiCatalog : ICatalog
    {
        private readonly OgcApi.OgcApi ogcApi;
        private readonly OgcApiRecordsStrategy recordsStrategy;
        public string Id { get; protected set; }
        public string Title { get; protected set; }
        public string Description { get; protected set; }
        public IDictionary<string, object> Metadata { get; } = new Dictionary<string, object>();

        protected OgcApiCatalog(OgcApi.OgcApi ogcApi, OgcApiRecordsStrategy recordsStrategy)
        {
            this.ogcApi = ogcApi;
            this.recordsStrategy = recordsStrategy;
        }

        /// <summary>
        /// Catalogs have a static factory method to create them as async information may be needed to populate
        /// their basic properties.
        /// </summary>
        public static async Task<OgcApiCatalog> CreateAsync(string url)
        {
            var ogcApi = new OgcApi.OgcApi(url);
            var conformance = await ogcApi.Conformance();
            var recordsStrategy = new OgcApiStrategyDispatcher(conformance);

            var id = await ogcApi.Id();
            var title = await ogcApi.Title();
            var description = await ogcApi.Description();
            
            return new OgcApiCatalog(ogcApi, recordsStrategy) { Id = id, Title = title, Description = description };
        }

        public async Task<ICatalogItemCollection> BrowseAsync(Pagination pagination = null)
        {
            return new LandingPage(recordsStrategy, await ogcApi.Collections(), pagination);
        }

        public async Task<ICatalogItemCollection> SearchAsync(string query, Pagination pagination = null)
        {
            var catalogItemCollection = await BrowseAsync(pagination);
            
            return await catalogItemCollection.SearchAsync(query, pagination);
        }

        public async Task<ICatalogItemCollection> SearchAsync(Expression expression, Pagination pagination = null)
        {
            var catalogItemCollection = await BrowseAsync(pagination);
            
            return await catalogItemCollection.SearchAsync(expression, pagination);
        }
        
        private class LandingPage : BaseCatalogItemCollectionPage<Collections>
        {
            private readonly OgcApiRecordsStrategy recordsStrategy;
            private readonly Collection[] items;
            protected override int MaxNumberOfItems => items.Length;

            public LandingPage(
                OgcApiRecordsStrategy recordsStrategy, 
                Collections source, 
                Pagination pagination = null
            ) : base(source, pagination)
            {
                this.recordsStrategy = recordsStrategy;
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
                            await CollectionPage.CreateAsync(item, pagination, recordsStrategy)
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
                => Task.FromResult<BaseCatalogItemCollectionPage<Collections>>(new LandingPage(recordsStrategy, src, p));
        }

        private class CollectionPage : BaseCatalogItemCollectionPage<Collection>
        {
            private readonly ConformanceDeclaration conformance;
            private readonly FeatureCollection items;
            private readonly OgcApiRecordsStrategy recordsStrategy;
            protected override int MaxNumberOfItems => (int)items.NumberMatched;

            private CollectionPage(
                OgcApiRecordsStrategy recordsStrategy, 
                Collection source, 
                FeatureCollection items, 
                Pagination pagination = null
            ) : base(source, pagination)
            {
                this.items = items;
                this.recordsStrategy = recordsStrategy;
            }

            public static async Task<CollectionPage> CreateAsync(
                Collection source, 
                Pagination pagination,
                OgcApiRecordsStrategy recordsStrategy
            ) {
                return new CollectionPage(
                    recordsStrategy,
                    source, 
                    await source.FetchItems(pagination.Limit, pagination.Offset), 
                    pagination
                );
            }

            public override async Task<ICatalogItemCollection> SearchAsync(string keyword, Pagination pagination = null)
            {
                pagination ??= new Pagination();

                return new CollectionPage(
                    recordsStrategy, 
                    source, 
                    await recordsStrategy.SearchAsync(source, keyword, pagination), 
                    pagination
                );
            }

            public override async Task<ICatalogItemCollection> SearchAsync(Expression expression, Pagination pagination = null)
            {
                pagination ??= new Pagination();

                return new CollectionPage(
                    recordsStrategy, 
                    source, 
                    await recordsStrategy.SearchAsync(source, expression, pagination), 
                    pagination
                );
            }

            public override Task<IEnumerable<ICatalogItem>> GetItemsAsync() 
                => Task.FromResult(items.Features.Select(recordsStrategy.ParseFeature));


            protected override async Task<BaseCatalogItemCollectionPage<Collection>> CreatePageAsyncInternal(Collection src, Pagination p)
                => await CreateAsync(src, p, recordsStrategy);
        }
    }
}
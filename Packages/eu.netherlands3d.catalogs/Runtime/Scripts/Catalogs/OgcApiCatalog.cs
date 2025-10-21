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
        public string Id { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }
        public IDictionary<string, object> Metadata { get; } = new Dictionary<string, object>();

        private OgcApiCatalog(OgcApi.OgcApi ogcApi, OgcApiRecordsStrategy recordsStrategy)
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
            var recordsStrategy = new OgcApiRecordsStrategySelector(conformance);

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
                // TODO: Support Local Resource Catalogs - meaning we should also support collections with ItemType "feature"
                items = source.Items.Where(collection => collection.ItemType == "record").ToArray();
            }

            public override Task<ICatalogItem> GetAsync(string id)
            {
                throw new NotImplementedException();
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
            private FeatureCollection items = new();
            private readonly Func<Task<FeatureCollection>> itemsCallback;
            private readonly OgcApiRecordsStrategy recordsStrategy;
            protected override int MaxNumberOfItems => (int)items.NumberMatched;

            private CollectionPage(
                OgcApiRecordsStrategy recordsStrategy, 
                Collection source, 
                Func<Task<FeatureCollection>> itemsCallback,
                Pagination pagination = null
            ) : base(source, pagination)
            {
                this.itemsCallback = itemsCallback;
                this.recordsStrategy = recordsStrategy;
            }

            public static Task<CollectionPage> CreateAsync(
                Collection source, 
                Pagination pagination,
                OgcApiRecordsStrategy recordsStrategy
            ) {
                return Task.FromResult(new CollectionPage(
                    recordsStrategy,
                    source, 
                    async () => await source.FetchItems(pagination.Limit, pagination.Offset), 
                    pagination
                ));
            }

            public override Task<ICatalogItemCollection> SearchAsync(string keyword, Pagination pagination = null)
            {
                pagination ??= new Pagination();

                return Task.FromResult<ICatalogItemCollection>(new CollectionPage(
                    recordsStrategy, 
                    source, 
                    async () => await recordsStrategy.SearchAsync(source, keyword, pagination), 
                    pagination
                ));
            }

            public override Task<ICatalogItemCollection> SearchAsync(Expression expression, Pagination pagination = null)
            {
                pagination ??= new Pagination();

                return Task.FromResult<ICatalogItemCollection>(new CollectionPage(
                    recordsStrategy, 
                    source,
                    async () => await recordsStrategy.SearchAsync(source, expression, pagination), 
                    pagination
                ));
            }

            public override Task<ICatalogItem> GetAsync(string id)
            {
                throw new NotImplementedException();
            }

            public override async Task<IEnumerable<ICatalogItem>> GetItemsAsync()
            {
                items = await itemsCallback();
                var itemsList = new List<ICatalogItem>();
                foreach (var feature in items.Features)
                {
                    itemsList.Add(await recordsStrategy.ParseFeature(feature));
                }
                
                return itemsList;
            }

            protected override async Task<BaseCatalogItemCollectionPage<Collection>> CreatePageAsyncInternal(Collection src, Pagination p)
                => await CreateAsync(src, p, recordsStrategy);
        }
    }
}
using System;
using System.Linq;
using System.Threading.Tasks;
using Netherlands3D.Catalogs.CatalogItems;
using NUnit.Framework;

namespace Netherlands3D.Catalogs.Catalogs
{
    [TestFixture]
    public class PdokOgcApiCatalogTests
    {
        // BEWARE: These IDs are not guaranteed to be stable; these come from PDOK's demo catalog, and may change
        // at any time.
        private const string mainCollectionId = "metadata:main";
        private const string mainCollectionTitle = "pycsw Geospatial Catalogue";
        private const string firstItemInMainCollection = "2891cc29-0a79-46d1-8649-287046d621c7";
        private const string secondItemInMainCollection = "97cf6a64-9cfc-4ce6-9741-2db44fd27fca";
        private const string thirdItemInMainCollection = "ad797d3c-0fb1-4a1a-a88b-1f1691765859";
        private const string fourthItemInMainCollection = "ba8b66c9-5b11-4a15-90d6-73335029061d";
        private const string fifthItemInMainCollection = "ff9315c8-f25a-4d01-9245-5cf058314ebf";

        private PdokOgcApiCatalog catalog;

        [SetUp]
        public async Task SetUp()
        {
            catalog = await PdokOgcApiCatalog.CreateAsync();
        }

        [Test]
        public async Task BrowseAsync_DefaultPage_ReturnsAllTopLevelItems()
        {
            var page = await catalog.BrowseAsync();
            var itemsAsList = (await page.GetItemsAsync()).ToList();

            Assert.AreEqual(1, itemsAsList.Count);
            Assert.IsInstanceOf<ICatalogItem>(itemsAsList[0]);
            
            // Can be approached as a catalog item for UI purposes
            Assert.AreEqual(mainCollectionId, itemsAsList[0].Id);
            Assert.AreEqual(mainCollectionTitle, itemsAsList[0].Title);

            Assert.IsFalse(page.HasNextPage);
            Assert.IsFalse(page.HasPreviousPage);
        }

        [Test]
        public async Task BrowseAsync_GetItemsFromMainCollection()
        {
            var page = await catalog.BrowseAsync();
            var items = await page.GetItemsAsync();
            var itemsAsList = items.ToList();
            
            CollectionAssert.IsNotEmpty(itemsAsList);
            Assert.AreEqual(1, itemsAsList.Count);

            // Can be approached as a record collection for going deeper into the hierarchy
            var firstItem = itemsAsList[0] as ICatalogItemCollection;
            Assert.IsNotNull(firstItem);
            
            var mainCollectionItems = await firstItem.GetItemsAsync();
            var mainCollectionItemsAsList = mainCollectionItems.ToList();
            Assert.AreEqual(5, mainCollectionItemsAsList.Count);
            
            Assert.IsFalse(firstItem.HasNextPage);
            Assert.IsTrue(firstItem.IsFirstPage);
            Assert.IsFalse(firstItem.HasPreviousPage);
            Assert.IsTrue(firstItem.IsLastPage);
        }

        [Test]
        public async Task BrowseAsync_GetCollectionAsFolder()
        {
            var collectionListing = await catalog.BrowseAsync();
            var collectionListingItems = await collectionListing.GetItemsAsync();
            var collectionListingItemsAsList = collectionListingItems.ToList();
            
            CollectionAssert.IsNotEmpty(collectionListingItemsAsList);
            Assert.AreEqual(1, collectionListingItemsAsList.Count);

            // Can be approached as a record collection for going deeper into the hierarchy
            var mainCollection = collectionListingItemsAsList[0] as FolderItem;
            Assert.IsNotNull(mainCollection);
            Assert.IsInstanceOf<FolderItem>(mainCollection);
            Assert.IsInstanceOf<ICatalogItemCollection>(mainCollection);
            
            Assert.AreEqual(mainCollectionId, mainCollection.Id);
            Assert.AreEqual(mainCollectionTitle, mainCollection.Title);
        }

        [Test]
        public async Task BrowseAsync_GetInformationFromRecord()
        {
            var collectionListing = await catalog.BrowseAsync();
            var collectionListingItems = await collectionListing.GetItemsAsync();
            var collectionListingItemsAsList = collectionListingItems.ToList();
            
            CollectionAssert.IsNotEmpty(collectionListingItemsAsList);
            Assert.AreEqual(1, collectionListingItemsAsList.Count);

            // Can be approached as a record collection for going deeper into the hierarchy
            var mainCollection = collectionListingItemsAsList[0] as ICatalogItemCollection;
            Assert.IsNotNull(mainCollection);
            
            var mainCollectionItems = await mainCollection.GetItemsAsync();
            var mainCollectionItemsAsList = mainCollectionItems.ToList();

            var record = mainCollectionItemsAsList[0] as RecordItem;
            Assert.IsInstanceOf<RecordItem>(record);
            Assert.IsNotNull(record);
            Assert.AreEqual(firstItemInMainCollection, record.Id);
            Assert.AreEqual("BGT WMTS", record.Title);
            Assert.AreEqual("De BGT, Basisregistratie Grootschalige Topografie, wordt de gedetailleerde grootschalige basiskaart (digitale kaart) van heel Nederland, waarin op een eenduidige manier de ligging van alle fysieke objecten zoals gebouwen, wegen, water, spoorlijnen en (landbouw)terreinen is geregistreerd.\n\nVoorstellen voor verbetering van de BGT kunt u bekijken met de BGT Terugmeldingen dataset:\nhttps://www.nationaalgeoregister.nl/geonetwork/srv/dut/catalog.search#/metadata/ce5f0923-b697-42ea-8744-4fa4a77ec02e", record.Description);
            Assert.AreEqual("https://service.pdok.nl/lv/bgt/wmts/v1_0?request=GetCapabilities&service=WMTS", record.Url?.ToString());
            Assert.AreEqual("OGC:WMTS", record.Protocol);
            Assert.IsNull(record.MediaType);
        }

        [Test]
        public async Task BrowseAsync_WithLimitAndOffset_PagesCorrectly()
        {
            // Pagination set at the top level persists through the whole catalog
            var collectionListing = await catalog.BrowseAsync(new Pagination(0, 2));
            var collectionListingItems = await collectionListing.GetItemsAsync();
            var collectionListingItemsAsList = collectionListingItems.ToList();
            
            // Can be approached as a record collection for going deeper into the hierarchy
            var firstCollection = collectionListingItemsAsList[0] as ICatalogItemCollection;
            
            var firstPageItems = (await firstCollection.GetItemsAsync()).ToList();
            Assert.AreEqual(2, firstPageItems.Count);
            Assert.AreEqual(firstItemInMainCollection, firstPageItems[0].Id);
            Assert.AreEqual(secondItemInMainCollection, firstPageItems[1].Id);

            Assert.IsTrue(firstCollection.HasNextPage);
            Assert.IsTrue(firstCollection.IsFirstPage);
            Assert.IsFalse(firstCollection.HasPreviousPage);
            Assert.IsFalse(firstCollection.IsLastPage);
        
            // second page
            var secondPage = await firstCollection.GetNextPageAsync();
            var secondPageItems = (await secondPage.GetItemsAsync()).ToList();
            Assert.AreEqual(2, secondPageItems.Count);
            Assert.AreEqual(thirdItemInMainCollection, secondPageItems[0].Id);
            Assert.AreEqual(fourthItemInMainCollection, secondPageItems[1].Id);

            Assert.IsTrue(secondPage.HasNextPage);
            Assert.IsTrue(secondPage.HasPreviousPage);
        
            // third page
            var thirdPage = await secondPage.GetNextPageAsync();
            var thirdPageItems = (await thirdPage.GetItemsAsync()).ToList();
            Assert.AreEqual(1, thirdPageItems.Count);
            Assert.AreEqual(fifthItemInMainCollection, thirdPageItems[0].Id);

            Assert.IsFalse(thirdPage.HasNextPage);
            Assert.IsTrue(thirdPage.HasPreviousPage);
        
            // back to first
            var backToFirst = await secondPage.GetPreviousPageAsync();
            var backItems = (await backToFirst.GetItemsAsync()).ToList();
            Assert.AreEqual(firstItemInMainCollection, backItems[0].Id);
            Assert.AreEqual(secondItemInMainCollection, backItems[1].Id);
        }
        
        [Test]
        public async Task BrowseAsync_InvalidNextPage_ThrowsInvalidOperationException()
        {
            var page = await catalog.BrowseAsync();
            Assert.IsFalse(page.HasNextPage);

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await page.GetNextPageAsync()
            );
        }

        [Test]
        public async Task BrowseAsync_InvalidPreviousPage_ThrowsInvalidOperationException()
        {
            var page = await catalog.BrowseAsync();
            Assert.IsFalse(page.HasPreviousPage);

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await page.GetPreviousPageAsync()
            );
        }
    }
}
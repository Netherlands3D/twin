using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Netherlands3D.Catalogs.Catalogs
{
    [TestFixture]
    public class PdokOgcApiCatalogTests
    {
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
            Assert.AreEqual("metadata:main", itemsAsList[0].Id);
            Assert.AreEqual("pycsw Geospatial Catalogue", itemsAsList[0].Title);

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

        //     [Test]
        //     public async Task BrowseAsync_WithLimitAndOffset_PagesCorrectly()
        //     {
        //         // first page of size 2
        //         var firstPage = await catalog.BrowseAsync(Pagination.WithOffset(0, 2));
        //         
        //         var firstItems = (await firstPage.GetItemsAsync()).ToList();
        //         CollectionAssert.AreEqual(sampleCatalogItems.Take(2), firstItems);
        //         Assert.IsTrue(firstPage.HasNextPage);
        //         Assert.IsTrue(firstPage.IsFirstPage);
        //         Assert.IsFalse(firstPage.HasPreviousPage);
        //         Assert.IsFalse(firstPage.IsLastPage);
        //
        //         // second page
        //         var secondPage = await firstPage.GetNextPageAsync();
        //         var secondItems = (await secondPage.GetItemsAsync()).ToList();
        //         CollectionAssert.AreEqual(sampleCatalogItems.Skip(2).Take(2), secondItems);
        //         Assert.IsTrue(secondPage.HasNextPage);
        //         Assert.IsTrue(secondPage.HasPreviousPage);
        //
        //         // third page
        //         var thirdPage = await secondPage.GetNextPageAsync();
        //         var thirdItems = (await thirdPage.GetItemsAsync()).ToList();
        //         CollectionAssert.AreEqual(sampleCatalogItems.Skip(4).Take(2), thirdItems);
        //         Assert.IsFalse(thirdPage.HasNextPage);
        //         Assert.IsTrue(thirdPage.HasPreviousPage);
        //
        //         // back to first
        //         var backToFirst = await secondPage.GetPreviousPageAsync();
        //         var backItems = (await backToFirst.GetItemsAsync()).ToList();
        //         CollectionAssert.AreEqual(sampleCatalogItems.Take(2), backItems);
        //     }
        //
        //     [Test]
        //     public async Task BrowseAsync_WithLimitAndOffset_GetSecondPageDirectly()
        //     {
        //         int numberOfItemsPerPage = 2;
        //         int pageNumber = 2;
        //         
        //         var secondPage = await catalog.BrowseAsync(
        //             Pagination.WithPageNumber(pageNumber, numberOfItemsPerPage)
        //         );
        //         
        //         var secondItems = (await secondPage.GetItemsAsync()).ToList();
        //         CollectionAssert.AreEqual(sampleCatalogItems.Skip(2).Take(2), secondItems);
        //         Assert.IsTrue(secondPage.HasNextPage);
        //         Assert.IsTrue(secondPage.HasPreviousPage);
        //     }
        //
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
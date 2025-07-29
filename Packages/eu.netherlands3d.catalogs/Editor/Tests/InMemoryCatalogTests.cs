using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Netherlands3D.Catalogs;

namespace Netherlands3D.Catalogs
{
    [TestFixture]
    public class InMemoryCatalogTests
    {
        private readonly List<ICatalogItem> sampleRecords = new()
        {
            InMemoryCatalog.CreateRecord("1", "Kiwi", "Fluffy fruit"),
            InMemoryCatalog.CreateRecord("2", "Banana", "Yellow fruit"),
            InMemoryCatalog.CreateRecord("3", "Cherry", "Small fruit"),
            InMemoryCatalog.CreateRecord("4", "Date", "Sweet fruit"),

            // A folder of apple varieties, page size = 2
            InMemoryCatalog.CreateFolder(
                id: "5",
                title: "Apple",
                description: "Lots of fruit",
                records: new List<ICatalogItem>
                {
                    InMemoryCatalog.CreateRecord("6", "Jonagold", null),
                    InMemoryCatalog.CreateRecord("7", "Gala", null),
                    InMemoryCatalog.CreateRecord("8", "Pinklady", null),
                    InMemoryCatalog.CreateRecord("9", "Golden Delicious", null)
                },
                limit: 2
            ),

            // A nested catalog of orchards
            new InMemoryCatalog(
                id: "10",
                title: "Orchards",
                description: null,
                records: new List<ICatalogItem>
                {
                    InMemoryCatalog.CreateRecord("11", "Hazelwood Orchard", null),
                    InMemoryCatalog.CreateRecord("12", "Silverwood Orchard", null)
                }
            )
        };

        private InMemoryCatalog catalog;

        [SetUp]
        public void SetUp()
        {
            catalog = new InMemoryCatalog(
                id: "object-library",
                title: "Object Bibliotheek",
                description: null,
                records: sampleRecords
            );
        }

        [Test]
        public async Task BrowseAsync_DefaultPage_ReturnsAllTopLevelItems()
        {
            var page = await catalog.BrowseAsync();
            var items = (await page.GetItemsAsync()).ToList();

            // 6 top-level items: 4 records + 1 folder + 1 nested catalog
            Assert.AreEqual(6, items.Count);
            CollectionAssert.AreEqual(sampleRecords, items);

            Assert.IsFalse(page.HasNextPage);
            Assert.IsFalse(page.HasPreviousPage);
        }

        [Test]
        public async Task BrowseAsync_WithLimitAndOffset_PagesCorrectly()
        {
            // first page of size 2
            var firstPage = await catalog.BrowseAsync(limit: 2, offset: 0);
            
            var firstItems = (await firstPage.GetItemsAsync()).ToList();
            CollectionAssert.AreEqual(sampleRecords.Take(2), firstItems);
            Assert.IsTrue(firstPage.HasNextPage);
            Assert.IsFalse(firstPage.HasPreviousPage);

            // second page
            var secondPage = await firstPage.GetNextPageAsync();
            var secondItems = (await secondPage.GetItemsAsync()).ToList();
            CollectionAssert.AreEqual(sampleRecords.Skip(2).Take(2), secondItems);
            Assert.IsTrue(secondPage.HasNextPage);
            Assert.IsTrue(secondPage.HasPreviousPage);

            // third page
            var thirdPage = await secondPage.GetNextPageAsync();
            var thirdItems = (await thirdPage.GetItemsAsync()).ToList();
            CollectionAssert.AreEqual(sampleRecords.Skip(4).Take(2), thirdItems);
            Assert.IsFalse(thirdPage.HasNextPage);
            Assert.IsTrue(thirdPage.HasPreviousPage);

            // back to first
            var backToFirst = await secondPage.GetPreviousPageAsync();
            var backItems = (await backToFirst.GetItemsAsync()).ToList();
            CollectionAssert.AreEqual(sampleRecords.Take(2), backItems);
        }

        [Test]
        public async Task BrowseAsync_InvalidNextPage_ThrowsInvalidOperationException()
        {
            var page = await catalog.BrowseAsync(limit: 10);
            Assert.IsFalse(page.HasNextPage);

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await page.GetNextPageAsync()
            );
        }

        [Test]
        public async Task BrowseAsync_InvalidPreviousPage_ThrowsInvalidOperationException()
        {
            var page = await catalog.BrowseAsync(limit: 10);
            Assert.IsFalse(page.HasPreviousPage);

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await page.GetPreviousPageAsync()
            );
        }

        [Test]
        public async Task BrowseAsync_FolderItem_ContainsAndPagesChildren()
        {
            // locate the folder with Id "5"
            var page = await catalog.BrowseAsync();
            var items = (await page.GetItemsAsync()).ToList();

            IPaginatedRecordCollection folder = items
                .OfType<FolderItem>()
                .Single(f => f.Id == "5");

            // First folder page: 2 items (limit=2)
            var childPage1 = folder;
            var childItems1 = (await childPage1.GetItemsAsync()).ToList();
            var expectedFirst = new[] { "6", "7" };
            CollectionAssert.AreEqual(expectedFirst, childItems1.Select(i => i.Id));

            Assert.IsTrue(childPage1.HasNextPage);
            Assert.IsFalse(childPage1.HasPreviousPage);

            // Second folder page: next 2 items
            var childPage2 = await childPage1.GetNextPageAsync();
            var childItems2 = (await childPage2.GetItemsAsync()).ToList();
            var expectedNext = new[] { "8", "9" };
            CollectionAssert.AreEqual(expectedNext, childItems2.Select(i => i.Id));

            Assert.IsFalse(childPage2.HasNextPage);
            Assert.IsTrue(childPage2.HasPreviousPage);
        }

        [Test]
        public async Task BrowseAsync_NestedCatalog_BrowseChildrenDirectly()
        {
            // find the nested InMemoryCatalog with Id "10"
            var page = await catalog.BrowseAsync();
            var items = (await page.GetItemsAsync()).ToList();

            var nested = items
                .OfType<InMemoryCatalog>()
                .Single(c => c.Id == "10");

            // browsing that catalog yields its two orchard records
            var subPage = await nested.BrowseAsync();
            var subItems = (await subPage.GetItemsAsync()).ToList();

            Assert.AreEqual(2, subItems.Count);
            Assert.IsTrue(subItems.All(i => i is RecordItem));
            Assert.AreEqual("11", subItems[0].Id);
            Assert.AreEqual("12", subItems[1].Id);

            Assert.IsFalse(subPage.HasNextPage);
            Assert.IsFalse(subPage.HasPreviousPage);
        }
    }
}
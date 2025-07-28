using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Netherlands3D.Catalogs
{
    [TestFixture]
    public class InMemoryCatalogTests
    {
        private readonly List<Record> sampleRecords = new()
        {
            new() { Id = "1", Title = "Apple", Description = "Red fruit" },
            new() { Id = "2", Title = "Banana", Description = "Yellow fruit" },
            new() { Id = "3", Title = "Cherry", Description = "Small fruit" },
            new() { Id = "4", Title = "Date", Description = "Sweet fruit" }
        };
        private InMemoryCatalog catalog;

        [SetUp]
        public void SetUp()
        {
            catalog = new InMemoryCatalog(sampleRecords);
        }

        [Test]
        public async Task BrowseAsync_DefaultPage_ReturnsFirstNItems()
        {
            // default limit=50, so all 4 items
            var page = await catalog.BrowseAsync();
            var items = (await page.GetItemsAsync()).ToList();

            Assert.AreEqual(4, items.Count);
            CollectionAssert.AreEqual(sampleRecords, items);
            Assert.IsFalse(((IPaginatedRecordCollection)page).HasNextPage);
            Assert.IsFalse(((IPaginatedRecordCollection)page).HasPreviousPage);
        }

        [Test]
        public async Task BrowseAsync_WithLimitAndOffset_PagesCorrectly()
        {
            // page size 2
            var firstPage = await catalog.BrowseAsync(limit: 2, offset: 0) as IPaginatedRecordCollection;
            var firstItems = (await firstPage.GetItemsAsync()).ToList();
            CollectionAssert.AreEqual(sampleRecords.Take(2), firstItems);
            Assert.IsTrue(firstPage.HasNextPage);
            Assert.IsFalse(firstPage.HasPreviousPage);

            // next page
            var secondPage = await firstPage.GetNextPageAsync();
            var secondItems = (await secondPage.GetItemsAsync()).ToList();
            CollectionAssert.AreEqual(sampleRecords.Skip(2).Take(2), secondItems);
            Assert.IsFalse(secondPage.HasNextPage);
            Assert.IsTrue(secondPage.HasPreviousPage);

            // back to first
            var backToFirst = await secondPage.GetPreviousPageAsync();
            var backItems = (await backToFirst.GetItemsAsync()).ToList();
            CollectionAssert.AreEqual(sampleRecords.Take(2), backItems);
        }

        [Test]
        public async Task BrowseAsync_InvalidNextPage_Throws()
        {
            var page = await catalog.BrowseAsync(limit: 10) as IPaginatedRecordCollection;
            Assert.IsFalse(page.HasNextPage);
            Assert.Throws<InvalidOperationException>(() => page.GetNextPageAsync().Wait());
        }

        [Test]
        public async Task BrowseAsync_InvalidPreviousPage_Throws()
        {
            var page = await catalog.BrowseAsync(limit: 10) as IPaginatedRecordCollection;
            Assert.IsFalse(page.HasPreviousPage);
            Assert.Throws<InvalidOperationException>(() => page.GetPreviousPageAsync().Wait());
        }
    }
}
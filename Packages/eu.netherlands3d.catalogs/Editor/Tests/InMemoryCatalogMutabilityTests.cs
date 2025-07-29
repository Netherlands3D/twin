using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Netherlands3D.Catalogs
{
    [TestFixture]
    public class InMemoryCatalogMutabilityTests
    {
        private IWritableCatalog catalog;

        [SetUp]
        public void SetUp()
        {
            var starting = new List<RecordItem>
            {
                new() { Id = "A", Title = "One" },
                new() { Id = "B", Title = "Two" }
            };
            catalog = new InMemoryCatalog("object-library", "Object Bibliotheek", null, starting);
        }

        [Test]
        public async Task Add_Record()
        {
            var collection = await catalog.BrowseAsync();
            var before = (await collection.GetItemsAsync()).Count();

            catalog.Add(new RecordItem { Id = "C", Title = "Three" });
            var after = await (await catalog.BrowseAsync()).GetItemsAsync();

            Assert.AreEqual(before + 1, after.Count());
            Assert.IsTrue(after.Any(r => r.Id == "C" && r.Title == "Three"));
        }

        [Test]
        public async Task Remove_Record()
        {
            var collection = await catalog.BrowseAsync();
            var before = (await collection.GetItemsAsync()).Count();
            var removed = catalog.Remove("B");
            var items = await (await catalog.BrowseAsync()).GetItemsAsync();

            Assert.IsTrue(removed);
            Assert.AreEqual(before - 1, items.Count());
            Assert.IsFalse(items.Any(r => r.Id == "B"));
        }

        [Test]
        public async Task Remove_NonExistingId_ReturnsFalseAndNoChange()
        {
            var collection = await catalog.BrowseAsync();
            var before = (await collection.GetItemsAsync()).Count();
            var removed = catalog.Remove("Z");
            var items = await (await catalog.BrowseAsync()).GetItemsAsync();

            Assert.IsFalse(removed);
            Assert.AreEqual(before, items.Count());
        }

        [Test]
        public async Task Clear_RemovesAllRecords()
        {
            catalog.Clear();

            var collection = await catalog.BrowseAsync();
            var items = await collection.GetItemsAsync();

            Assert.IsEmpty(items);
        }

        [Test]
        public async Task Mutations_ReflectInPaging()
        {
            // add two more to exceed default page size=2
            catalog.Add(new RecordItem { Id = "C", Title = "Three" });
            catalog.Add(new RecordItem { Id = "D", Title = "Four" });

            // now browse with pageSize=2
            var page1 = await catalog.BrowseAsync(limit: 2, offset: 0) as IPaginatedRecordCollection;
            Assert.IsTrue(page1.HasNextPage); // we have 4 total

            var page2 = await page1.GetNextPageAsync();
            Assert.IsFalse(page2.HasNextPage);
            Assert.IsTrue(page2.HasPreviousPage);

            // clear and verify no pages
            catalog.Clear();
            var clearedPage = await catalog.BrowseAsync(limit: 2) as IPaginatedRecordCollection;
            Assert.IsFalse(clearedPage.HasNextPage);
            Assert.IsFalse(clearedPage.HasPreviousPage);
            Assert.IsEmpty(clearedPage.GetItemsAsync().Result);
        }
    }
}
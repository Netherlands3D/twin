using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Netherlands3D.Catalogs
{
    [TestFixture]
    public class InMemoryCatalogSearchTests
    {
        private readonly List<ICatalogItem> sampleRecords = new()
        {
            InMemoryCatalog.CreateRecord("1", "Kiwi", "Fluffy fruit"),
            InMemoryCatalog.CreateRecord("2", "Banana", "Yellow fruit"),
            InMemoryCatalog.CreateRecord("3", "Cherry", "Small fruit"),
            InMemoryCatalog.CreateRecord("4", "Date", "Sweet fruit"),

            // a folder named “Apple”
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
                Pagination.WithOffset(0, 2) // page size inside folder, not relevant for top-level search
            ),

            // a nested catalog “Orchards”
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
                id: "fruit-lib",
                title: "Fruit Library",
                description: "All things fruit",
                records: sampleRecords
            );
        }

        [Test]
        public async Task SearchAsync_RecordTitleMatch_ReturnsOnlyThatRecord()
        {
            var page = await catalog.SearchAsync("Kiwi");
            var items = (await page.GetItemsAsync()).ToList();

            Assert.AreEqual(1, items.Count, "Should find exactly one matching record");
            Assert.IsInstanceOf<RecordItem>(items[0], "Result should be a RecordItem");
            Assert.AreEqual("1", items[0].Id);
            Assert.AreEqual("Kiwi", items[0].Title);

            Assert.IsFalse(page.HasNextPage, "Single result → no next page");
            Assert.IsFalse(page.HasPreviousPage, "Single result → no previous page");
        }

        [Test]
        public async Task SearchAsync_FolderTitleMatch_ReturnsFolder()
        {
            var page = await catalog.SearchAsync("Apple");
            var items = (await page.GetItemsAsync()).ToList();

            Assert.AreEqual(1, items.Count);
            Assert.IsInstanceOf<FolderItem>(items[0]);
            Assert.AreEqual("5", items[0].Id);
            Assert.AreEqual("Apple", items[0].Title);
        }

        [Test]
        public async Task SearchAsync_NestedCatalogTitleMatch_ReturnsThatCatalog()
        {
            var page = await catalog.SearchAsync("Orchards");
            var items = (await page.GetItemsAsync()).ToList();

            Assert.AreEqual(1, items.Count);
            Assert.IsInstanceOf<InMemoryCatalog>(items[0]);
            Assert.AreEqual("10", items[0].Id);
            Assert.AreEqual("Orchards", items[0].Title);
        }

        [Test]
        public async Task SearchAsync_SubstringCaseInsensitive_WorksAcrossRecords()
        {
            // substring "a" appears in Banana, Date, Apple, Orchards (but not Kiwi or Cherry)
            var page = await catalog.SearchAsync("a");
            var items = (await page.GetItemsAsync()).ToList();

            var expectedIds = new[] { "2", "4", "5", "10" };
            CollectionAssert.AreEquivalent(expectedIds, items.Select(i => i.Id));
        }

        [Test]
        public async Task SearchAsync_Pagination_WorksCorrectly()
        {
            // search "a", limit 2 → first 2 matching items
            var page1 = await catalog.SearchAsync("a", Pagination.WithOffset(0, 2));
            var ids1 = (await page1.GetItemsAsync()).Select(i => i.Id).ToList();
            Assert.AreEqual(2, ids1.Count);
            Assert.IsTrue(page1.HasNextPage);
            Assert.IsFalse(page1.HasPreviousPage);

            // next page → remaining two
            var page2 = await page1.GetNextPageAsync();
            var ids2 = (await page2.GetItemsAsync()).Select(i => i.Id).ToList();
            Assert.AreEqual(2, ids2.Count);
            Assert.IsFalse(page2.HasNextPage);
            Assert.IsTrue(page2.HasPreviousPage);

            // combined results = all four expected
            var allIds = ids1.Concat(ids2).ToList();
            CollectionAssert.AreEquivalent(new[] { "2", "4", "5", "10" }, allIds);
        }

        [Test]
        public async Task SearchAsync_NoMatches_ReturnsEmpty()
        {
            var page = await catalog.SearchAsync("pineapple");
            var items = await page.GetItemsAsync();
            Assert.IsEmpty(items);
            Assert.IsFalse(page.HasNextPage);
            Assert.IsFalse(page.HasPreviousPage);
        }

        [Test]
        public async Task SearchAsync_DoesNotDescendIntoFoldersOrNestedCatalogs()
        {
            // “Jonagold” lives inside the Apple folder, not top-level
            var page = await catalog.SearchAsync("Jonagold");
            var items = await page.GetItemsAsync();
            Assert.IsEmpty(items, "Search only applies to top-level Titles");
        }
    }
}
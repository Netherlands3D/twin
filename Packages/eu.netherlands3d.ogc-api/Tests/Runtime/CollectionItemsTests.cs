using System.Threading.Tasks;
using GeoJSON.Net.Feature;
using Netherlands3D.OgcApi.Pagination;
using Netherlands3D.OgcApi.Tests.Fixtures;
using NUnit.Framework;

namespace Netherlands3D.OgcApi.Tests
{
    [TestFixtureSource(nameof(Cases))]
    public class CollectionItemsTests
    {
        private static readonly OgcApiFixture[] Cases =
        {
            new Pdok(),
            new PyGeoAPI(),
            new PyCswGis(),
            new DigilabDemoAPI()
        };

        private OgcApi ogcApi;
        private readonly OgcApiFixture fixture;

        public CollectionItemsTests(OgcApiFixture fixture)
        {
            this.fixture = fixture;
        }

        [SetUp]
        public void Setup()
        {
            ogcApi = new OgcApi(fixture.BaseUrl);
        }

        [Test]
        public async Task CanFetchCollectionItems()
        {
            var id = fixture.Catalogues[0].Id;
            Collection collection = (await ogcApi.Collections()).FindById(id);

            Assert.IsNotNull(collection, $"Collection {id} was null");
            var items = await collection.Fetch();

            Assert.IsNotNull(items, $"Items for collection {id} was null");
            Assert.IsNotNull(items.Value, $"Feature collection in results for collection {id} was null");

            Assert.IsTrue(items.Value.Features.Count > 0, "No features were found in the collection");
        }

        [Test]
        public async Task CanLimitFetchingCollectionItemsToTwo()
        {
            var id = fixture.Catalogues[0].Id;
            Collection collection = (await ogcApi.Collections()).FindById(id);

            Assert.IsNotNull(collection, $"Collection {id} was null");
            var items = await collection.Fetch(2);

            Assert.IsInstanceOf<Results<FeatureCollection>>(items, $"Items for collection {id} was null");
            Assert.IsNotNull(items.Value, $"Feature collection in results for collection {id} was null");

            Assert.IsTrue(items.Value.Features.Count == 2, $"Expected 2 results, but got {items.Value.Features.Count}");
            Assert.IsTrue(items.NumberReturned == 2, $"Expected 2 returned results, but got {items.NumberReturned}");
            Assert.IsTrue(items.First(), "Expected this to be the first page in the results");
            Assert.IsFalse(items.Last(), "Expected this not to be the last page in the results");
        }

        [Test]
        public async Task CanPaginateToTheNextResultSet()
        {
            var id = fixture.Catalogues[0].Id;
            Collection collection = (await ogcApi.Collections()).FindById(id);

            Assert.IsNotNull(collection, $"Collection {id} was null");
            var items = await collection.Fetch(limit: 2);

            Assert.IsInstanceOf<Results<FeatureCollection>>(items, $"Items for collection {id} was null");

            var nextResults = await items.Next();
            Assert.IsInstanceOf<Results<FeatureCollection>>(nextResults, "Next results returned wrong type of object");
            Assert.IsTrue(nextResults.NumberReturned <= 2,
                $"Expected at most 2 returned results, but got {nextResults.NumberReturned}");
            Assert.IsFalse(nextResults.First(), "Expected this not to be the first page in the results");
        }

        [Test]
        public async Task CanPaginateToThePreviousResultSet()
        {
            var id = fixture.Catalogues[0].Id;
            Collection collection = (await ogcApi.Collections()).FindById(id);

            Assert.IsNotNull(collection, $"Collection {id} was null");
            var items = await collection.Fetch(limit: 2, offset: 2);

            Assert.IsInstanceOf<Results<FeatureCollection>>(items, $"Items for collection {id} was null");
            Assert.IsFalse(
                items.First(),
                "Expected this not to be the first page in the results because we started at offset 2"
            );

            var previous = await items.Previous();
            Assert.IsInstanceOf<Results<FeatureCollection>>(previous, "Previous results returned wrong type of object");
            // this assertion fails for the Digilab demo environment, see:
            // https://github.com/Geonovum/DTaaS-Testbed2/discussions/13#discussioncomment-13850505
            Assert.IsTrue(
                previous.NumberReturned == 2, 
                $"Expected 2 returned results, but got {previous.NumberReturned}"
            );
            Assert.IsTrue(previous.First(), "Expected this to be the first page in the results");
            Assert.IsFalse(previous.Last(), "Expected this not to be the last page in the results");
        }
    }
}
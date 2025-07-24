using System.Threading.Tasks;
using GeoJSON.Net.Feature;
using Netherlands3D.OgcApi.Pagination;
using Netherlands3D.OgcApi.Tests.Fixtures;
using NUnit.Framework;

namespace Netherlands3D.OgcApi.Tests
{
    [TestFixtureSource(nameof(Cases))]
    public class CollectionItemTests
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

        public CollectionItemTests(OgcApiFixture fixture)
        {
            this.fixture = fixture;
        }

        [SetUp]
        public void Setup()
        {
            ogcApi = new OgcApi(fixture.BaseUrl);
        }

        [Test]
        public async Task CanFetchItemFromCollectionListing()
        {
            var id = fixture.Catalogues[0].Id;
            Collection collection = (await ogcApi.Collections()).FindById(id);

            Assert.IsNotNull(collection, $"Collection {id} was null");
            var items = await collection.FetchItems();

            Assert.IsNotNull(items, $"Items for collection {id} was null");
            Assert.IsNotNull(items.Value, $"Feature collection in results for collection {id} was null");
            
            var item = items.Value.Features[0];
            
            Assert.IsInstanceOf<Feature>(item);
            CollectionAssert.Contains(item.Properties.Keys, "title");
            CollectionAssert.Contains(item.Properties.Keys, "type");
        }

        [Test]
        public async Task CanFetchItemFromCollectionById()
        {
            var id = fixture.Catalogues[0].Id;
            var recordId = fixture.Catalogues[0].ExampleRecordId;
            Collection collection = (await ogcApi.Collections()).FindById(id);
        
            Assert.IsNotNull(collection, $"Collection {id} was null");
            var item = await collection.FetchItemById(recordId);
        
            Assert.IsInstanceOf<Feature>(item);
            CollectionAssert.Contains(item.Properties.Keys, "title");
            CollectionAssert.Contains(item.Properties.Keys, "type");
        }
    }
}
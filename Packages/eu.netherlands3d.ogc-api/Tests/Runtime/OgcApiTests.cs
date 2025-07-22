using System.Threading.Tasks;
using Netherlands3D.OgcApi.Features;
using Netherlands3D.OgcApi.Tests.Fixtures;
using NUnit.Framework;

namespace Netherlands3D.OgcApi.Tests
{
    [TestFixtureSource(nameof(Cases))]
    public class OgcApiTests
    {
        static readonly OgcApiFixture[] Cases = {
            new PyGeoAPI(),
            new DigilabDemoAPI()
        };
        
        private OgcApi ogcApi;
        private readonly OgcApiFixture fixture;

        public OgcApiTests(OgcApiFixture fixture)
        {
            this.fixture = fixture;
        }
        
        [SetUp]
        public void Setup()
        {
            ogcApi = new OgcApi(fixture.BaseUrl);
        }

        [Test]
        public async Task CanFetchTitle()
        {
            string title = await ogcApi.Title();
            
            Assert.AreEqual(fixture.Title, title);
        }
        
        [Test]
        public async Task CanFetchDescription()
        {
            string description = await ogcApi.Description();
            
            Assert.AreEqual(fixture.Description, description);
        }
        
        [Test]
        public async Task CanFetchLandingPage()
        {
            LandingPage page = await ogcApi.LandingPage();
            
            Assert.IsNotNull(page, "LandingPage was null");
            Assert.IsNotNull(page.Links, "LandingPage.Links was null");
            Assert.IsTrue(page.Links.Length > 0, "No links found on LandingPage");
        }

        [Test]
        public async Task CanFetchConformanceDeclaration()
        {
            ConformanceDeclaration root = await ogcApi.Conformance();

            Assert.IsNotNull(root, "Conformance was null");
            Assert.IsTrue(root.ConformsTo.Length > 0, "No conformance returned");
        }

        [Test]
        public async Task CanFetchCollections()
        {
            Collections collections = await ogcApi.Collections();

            Assert.IsNotNull(collections, "Collections was null");
            Assert.IsNotNull(collections.Items, "Collections.Items was null");
            Assert.IsTrue(collections.Items.Length > 0, "No collections returned");
        }

        [Test]
        public async Task CanFetchCatalogues()
        {
            Collections collections = await ogcApi.Catalogues();

            Assert.IsNotNull(collections, "Catalogues was null");
            Assert.IsNotNull(collections.Items, "Catalogues.Items was null");
            Assert.IsTrue(
                collections.Items.Length >= fixture.Catalogues.Length, 
                $"At least {collections.Items.Length} catalogues were expected, but found {fixture.Catalogues.Length}"
            );

            for (int index = 0; index < fixture.Catalogues.Length; index++)
            {
                var expected = fixture.Catalogues[index];
                var catalogue = collections.Items[index];

                Assert.AreEqual(expected.Id, catalogue.Id, $"Catalogue {index} has an unexpected id");
                Assert.AreEqual(expected.Title, catalogue.Title, $"Catalogue {index} has an unexpected id");
            }
        }
    }
}

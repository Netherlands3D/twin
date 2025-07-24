using System.Threading.Tasks;
using Netherlands3D.OgcApi.Tests.Fixtures;
using NUnit.Framework;

namespace Netherlands3D.OgcApi.Tests
{
    [TestFixtureSource(nameof(Cases))]
    public class LandingPageTests
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

        public LandingPageTests(OgcApiFixture fixture)
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
            ConformanceDeclaration conformance = await ogcApi.Conformance();

            Assert.IsNotNull(conformance, "Conformance was null");
            Assert.IsTrue(conformance.ConformsTo.Length > 0, "No conformance returned");
            
            Assert.Contains("http://www.opengis.net/spec/ogcapi-records-1/1.0/conf/core", conformance.ConformsTo);
            Assert.Contains("http://www.opengis.net/spec/ogcapi-records-1/1.0/conf/json", conformance.ConformsTo);
        }
    }
}
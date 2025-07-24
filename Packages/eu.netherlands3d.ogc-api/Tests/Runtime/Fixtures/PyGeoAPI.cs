namespace Netherlands3D.OgcApi.Tests.Fixtures
{
    public record PyGeoAPI : OgcApiFixture
    {
        public PyGeoAPI()
        {
            BaseUrl = "https://demo.pygeoapi.io/master";
            Title = "pygeoapi Demo instance - running latest GitHub version";
            Description = "pygeoapi provides an API to geospatial data";
            Catalogues = new[]
            {
                new OgcApiCatalogueFixture
                {
                    Id = "dutch-metadata",
                    Url = "https://demo.pygeoapi.io/master/collections/dutch-metadata",
                    Title = "Sample metadata records from Dutch Nationaal georegister",
                    ExampleRecordId = "35149dfb-31d3-431c-a8bc-12a4034dac48"
                }
            };
        }
    }
}
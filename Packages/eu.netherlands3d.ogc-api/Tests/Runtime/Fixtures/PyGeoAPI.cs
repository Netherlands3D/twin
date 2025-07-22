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
                    Url = "https://demo.pygeoapi.io/master/collections/dutch-metadata?f=json",
                    Title = "Sample metadata records from Dutch Nationaal georegister"
                }
            };
        }
    }
}
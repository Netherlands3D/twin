namespace Netherlands3D.OgcApi.Tests.Fixtures
{
    public record Pdok : OgcApiFixture
    {
        public Pdok()
        {
            BaseUrl = "https://api.pdok.nl/catalogus/v1-demo/";
            Title = "pycsw Geospatial Catalogue";
            Description = "pycsw is an OARec and OGC CSW server implementation written in Python";
            Catalogues = new[]
            {
                new OgcApiCatalogueFixture
                {
                    Id = "metadata:main",
                    Url = "https://api.pdok.nl/catalogus/v1-demo/collections/metadata:main",
                    Title = "pycsw Geospatial Catalogue",
                    ExampleRecordId = "2891cc29-0a79-46d1-8649-287046d621c7"
                }
            };
        }
    }
}
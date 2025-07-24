namespace Netherlands3D.OgcApi.Tests.Fixtures
{
    public record PyCswGis : OgcApiFixture
    {
        public PyCswGis()
        {
            BaseUrl = "https://demo.pycsw.org/gisdata";
            Title = "pycsw Geospatial Catalogue gisdata demo";
            Description = "pycsw is an OARec and OGC CSW server implementation written in Python";
            Catalogues = new[]
            {
                new OgcApiCatalogueFixture
                {
                    Id = "metadata:main",
                    Url = "https://demo.pycsw.org/gisdata/collections/metadata:main",
                    Title = "pycsw Geospatial Catalogue gisdata demo",
                    ExampleRecordId = "urn:uuid:dc9b6d52-932a-11ea-ad6f-823cf448c401"
                }
            };
        }
    }
}
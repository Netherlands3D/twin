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
                    Url = "https://demo.pycsw.org/gisdata/collections/metadata:main?f=json",
                    Title = "pycsw Geospatial Catalogue gisdata demo"
                }
            };
        }
    }
}
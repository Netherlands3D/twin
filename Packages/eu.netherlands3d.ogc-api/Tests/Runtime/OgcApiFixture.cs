namespace Netherlands3D.OgcApi.Tests
{
    public record OgcApiCatalogueFixture
    {
        public string Id;
        public string Url;
        public string Title;
    }

    public record OgcApiFixture
    {
        public string BaseUrl;
        public string Title;
        public string Description;
        public OgcApiCatalogueFixture[] Catalogues;
    }
}
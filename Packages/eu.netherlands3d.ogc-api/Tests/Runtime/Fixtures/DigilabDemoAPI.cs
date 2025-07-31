namespace Netherlands3D.OgcApi.Tests.Fixtures
{
    public record DigilabDemoAPI : OgcApiFixture
    {
        public DigilabDemoAPI()
        {
            BaseUrl = "https://digilab.geocat.live/ogcapi-records";
            Title = "Digilab Demo Catalogus";
            Description = null;
            Catalogues = new[]
            {
                new OgcApiCatalogueFixture
                {
                    Id = "03a71d9b-78d5-4014-940b-287ae4c99fc7",
                    Url = "https://digilab.geocat.live/ogcapi-records/collections/03a71d9b-78d5-4014-940b-287ae4c99fc7?f=application%2Fjson",
                    Title = "Digilab Demo Catalogus",
                    ExampleRecordId = "fe48e569-f8d5-4c23-945e-78ffcad63c12"
                },
                new OgcApiCatalogueFixture
                {
                    Id = "2211dada-a830-47f0-9762-5e5c370d315a",
                    Url = "https://digilab.geocat.live/ogcapi-records/collections/2211dada-a830-47f0-9762-5e5c370d315a?f=application%2Fjson",
                    Title = "Clearly Rotterdam 3D"
                }
            };
        }
    }
}
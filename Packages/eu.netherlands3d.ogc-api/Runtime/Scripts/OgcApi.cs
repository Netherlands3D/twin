using System;
using System.Linq;
using System.Threading.Tasks;
using KindMen.Uxios.Api;

namespace Netherlands3D.OgcApi
{
    public class OgcApi
    {
        private readonly Resource<LandingPage> landingPageResource;
        private readonly Resource<Collections> collectionsResource;
        private readonly Resource<ConformanceDeclaration> conformanceResource;

        public OgcApi(string baseUrl)
        {
            landingPageResource = new Resource<LandingPage>(new Uri($"{baseUrl}"));
            collectionsResource = new Resource<Collections>(new Uri($"{baseUrl}/collections"));
            conformanceResource = new Resource<ConformanceDeclaration>(new Uri($"{baseUrl}/conformance"));
        }

        public async Task<string> Id() => (await LandingPage()).Id;
        public async Task<string> Title() => (await LandingPage()).Title;
        public async Task<string> Description() => (await LandingPage()).Description;
        public async Task<string> Attribution() => (await LandingPage()).Attribution;

        public async Task<LandingPage> LandingPage()
        {
            return await landingPageResource.Value;
        }

        public async Task<Collections> Collections()
        {
            return await collectionsResource.Value;
        }

        public async Task<ConformanceDeclaration> Conformance()
        {
            return await conformanceResource.Value;
        }

        public async Task<Collections> Catalogues()
        {
            var collections = await Collections();

            return new Collections
            {
                // using Linq instead of foreach to avoid allocating a List, or having to resize the array during loop
                Items = collections.Items
                    .Where(collection => collection.ItemType is "catalog" or "record")
                    .ToArray(),
                Links = collections.Links,
            };
        }
    }
}

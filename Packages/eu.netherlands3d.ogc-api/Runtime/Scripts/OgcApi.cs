using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KindMen.Uxios.Api;
using Netherlands3D.OgcApi.Features;

namespace Netherlands3D.OgcApi
{
    public class OgcApi
    {
        private string BaseUrl { get; }

        public OgcApi(string baseUrl)
        {
            BaseUrl = baseUrl;
        }

        public async Task<string> Title() => (await LandingPage()).Title;
        public async Task<string> Description() => (await LandingPage()).Description;
        public async Task<string> Attribution() => (await LandingPage()).Attribution;

        public async Task<LandingPage> LandingPage()
        {
            return await new Resource<LandingPage>(new Uri($"{BaseUrl}"))
                .Value;
        }

        public async Task<Collections> Collections()
        {
            return await new Resource<Collections>(new Uri($"{BaseUrl}/collections"))
                .Value;
        }

        public async Task<ConformanceDeclaration> Conformance()
        {
            return await new Resource<ConformanceDeclaration>(new Uri($"{BaseUrl}/conformance"))
                .Value;
        }

        public async Task<Collections> Catalogues()
        {
            var collections = await Collections();

            return new Collections
            {
                Items = collections.Items
                    .Where(collection => collection.ItemType is "catalog" or "record")
                    .ToArray(),
                Links = collections.Links,
            };
        }
    }
}

using System;
using KindMen.Uxios;
using Netherlands3D.Functionalities.Wms;
using Netherlands3D.OgcWebServices.Shared;
using UnityEngine;

namespace Netherlands3D.Functionalities.Wcs
{
    public class GetCoverageRequest : BaseRequest
    {
        private const string DefaultFallbackVersion = "1.0.0";
        public GetCoverageRequest(Uri sourceUrl, string xml) : base(sourceUrl)
        {
        }

        internal MapFilters CreateMapFromCapabilitiesUrl(Uri url, int width, int height)
        {
            var parameters = QueryString.Decode(url.Query);

            var version = parameters.Single("version");
            if (string.IsNullOrEmpty(version))
            {
                version = DefaultFallbackVersion;
                Debug.LogWarning("WCS version could not be determined, defaulting to " + DefaultFallbackVersion);
            }

            var wcsParam = new MapFilters
            {
                name = parameters.Single("coverageid") ?? parameters.Single("coverage"),
                spatialReferenceType = "CRS",
                spatialReference = defaultCoordinateSystemReference,
                version = version,
                style = null,
                width = width,
                height = height,
                transparent = false
            };

            var crs = parameters.Single("CRS");
            wcsParam.spatialReference = !string.IsNullOrEmpty(crs)
                ? crs
                : defaultCoordinateSystemReference;

            return wcsParam;
        }
    }
}
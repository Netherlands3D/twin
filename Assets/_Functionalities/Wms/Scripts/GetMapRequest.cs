using System;
using KindMen.Uxios;
using Netherlands3D.OgcWebServices.Shared;
using UnityEngine;

namespace Netherlands3D.Functionalities.Wms
{
    public class GetMapRequest : BaseRequest
    {
        private const string defaultFallbackVersion = "1.3.0"; //todo: make shared between wms classes?
        
        public GetMapRequest(Uri sourceUrl, string xml) : base(sourceUrl)
        {
        }

        internal MapFilters CreateMapFromCapabilitiesUrl(Uri url, int width, int height, bool transparent)
        {
            var parameters = QueryString.Decode(url.Query);
            var version = parameters.Single("version");
            if (string.IsNullOrEmpty(version))
            {
                version = defaultFallbackVersion;
                Debug.LogWarning("WMS version could not be determined, defaulting to " + defaultFallbackVersion);
            }

            var wmsParam = new MapFilters
            {
                name = parameters.Single("layers"),
                spatialReferenceType = MapFilters.SpatialReferenceTypeFromVersion(new Version(version)),
                spatialReference = defaultCoordinateSystemReference,
                style = parameters.Single("styles"),
                version = version,
                width = width,
                height = height,
                transparent = transparent
            };

            var crs = parameters.Single(wmsParam.spatialReferenceType);
            wmsParam.spatialReference = !string.IsNullOrEmpty(crs) ? crs : defaultCoordinateSystemReference;

            return wmsParam;
        }
    }
}
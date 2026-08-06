using System;
using KindMen.Uxios;
using Netherlands3D.Web;

namespace Netherlands3D.Functionalities.Wms
{
    public struct MapFilters
    {
        public string name;
        public string version;
        public string spatialReferenceType;
        public string spatialReference;
        public string style;
        public int width;
        public int height;
        public bool transparent;

        public static MapFilters FromUrl(Uri url)
        {
            var queryParameters = QueryString.Decode(url.Query);
            if (queryParameters.Single("request")?.ToLower() != "getmap")
            {
                throw new NotSupportedException("Creating a Map from URL is reserved for requests of type GetMap");
            }

            var version = queryParameters.Single("version");
            var spatialReferenceType = SpatialReferenceTypeFromVersion(new Version(version));
            int.TryParse(queryParameters.Single("width"), out var parsedWidth);
            int.TryParse(queryParameters.Single("height"), out var parsedHeight);
            bool.TryParse(queryParameters.Single("transparent"), out var parsedTransparent);

            return new MapFilters
            {
                name = queryParameters.Single("layers"),
                version = version,
                spatialReferenceType = spatialReferenceType,
                spatialReference = queryParameters.Single(spatialReferenceType),
                style = queryParameters.Single("style"),
                width = parsedWidth,
                height = parsedHeight,
                transparent = parsedTransparent,
            };
        }
        
        public static MapFilters FromUrlWCS(Uri url)
        {
            var queryParameters = QueryString.Decode(url.Query);

            if (queryParameters.Single("request")?.ToLower() != "getcoverage")
            {
                throw new NotSupportedException("Creating a Map from URL is reserved for requests of type GetCoverage");
            }

            var version = queryParameters.Single("version");

            int.TryParse(queryParameters.Single("WIDTH"), out var parsedWidth);
            int.TryParse(queryParameters.Single("HEIGHT"), out var parsedHeight);

            return new MapFilters
            {
                name = queryParameters.Single("coverage"),
                version = version,
                spatialReferenceType = "CRS",
                spatialReference = queryParameters.Single("CRS"),
                width = parsedWidth,
                height = parsedHeight,
            };
        }
        
        public static string SpatialReferenceTypeFromVersion(Version version)
        {
            return version.CompareTo(new Version("1.3.0")) >= 0 ? "CRS" : "SRS";
        }
        
        public Uri ToUrlBasedOn(Uri otherUrl)
        {
            // Start by removing any query parameters we want to inject
            var uriBuilder = new UriBuilder(otherUrl.AbsoluteUri);
            
            // Grab query parameters from previous url for re-use
            var queryParameters = QueryString.Decode(otherUrl.Query);

            // Set the required query parameters for the GetMap request
            uriBuilder.SetQueryParameter("service", "WMS");
            uriBuilder.SetQueryParameter("version", version);
            uriBuilder.SetQueryParameter("request", "GetMap");

            uriBuilder.SetQueryParameter("layers", name);
            uriBuilder.SetQueryParameter("styles", style);
            uriBuilder.SetQueryParameter(spatialReferenceType, spatialReference);
            uriBuilder.SetQueryParameter("bbox", "{0}"); // Bbox value is injected by WmsTileDataLayer
            uriBuilder.SetQueryParameter("width", width.ToString());
            uriBuilder.SetQueryParameter("height", height.ToString());
            
            string format = queryParameters.Single("format");
            if (format is not "image/png" and not "image/jpeg")
            {
                format = "image/png";
            }
            uriBuilder.SetQueryParameter("format", format);
            
            string transparentField = queryParameters.Single("transparent");
            if (string.IsNullOrEmpty(transparentField))
            {
                uriBuilder.SetQueryParameter("transparent", transparent ? "true" : "false");
            }

            return uriBuilder.Uri;
        }
        
        
        //TEMP FOR WCS
        public Uri ToUrlBasedOnWCS(Uri otherUrl)
        {
            // Start by removing any query parameters we want to inject
            var uriBuilder = new UriBuilder(otherUrl.AbsoluteUri);

            // Grab query parameters from previous url for re-use
            var queryParameters = QueryString.Decode(otherUrl.Query);

            // Set the required query parameters for the GetCoverage request
            uriBuilder.SetQueryParameter("service", "WCS");
            uriBuilder.SetQueryParameter("version", version);
            uriBuilder.SetQueryParameter("request", "GetCoverage");

            uriBuilder.SetQueryParameter("coverage", name);
            uriBuilder.SetQueryParameter("CRS", spatialReference);
            uriBuilder.SetQueryParameter("BBOX", "{0}"); // Bbox value is injected by WcsTileDataLayer
            uriBuilder.SetQueryParameter("WIDTH", width.ToString());
            uriBuilder.SetQueryParameter("HEIGHT", height.ToString());

            string format = queryParameters.Single("FORMAT");
            if (format is not "NetCDF4" and not "GeoTIFF")
            {
                format = "NetCDF4";
            }
            uriBuilder.SetQueryParameter("FORMAT", format);

            string time = queryParameters.Single("time");
            if (string.IsNullOrEmpty(time))
            {
                uriBuilder.SetQueryParameter("time", "current");
            }

            return uriBuilder.Uri;
        }
    }
}
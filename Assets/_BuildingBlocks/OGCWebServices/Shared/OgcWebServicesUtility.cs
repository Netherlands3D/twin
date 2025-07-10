using System;
using KindMen.Uxios;

namespace Netherlands3D.OgcWebServices.Shared
{
    public static class OgcWebServicesUtility
    {
        public static string CreateGetCapabilitiesURL(string url, ServiceType serviceType)
        {
            var uri = new Uri(url);
            var baseUrl = uri.GetLeftPart(UriPartial.Path);
            return $"{baseUrl}?request=GetCapabilities&service={serviceType.ToString().ToUpper()}";
        }

        public static bool IsValidUrl(Uri url, RequestType requestType)
        {
            return url.Query.Contains($"request={requestType}", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsValidUrl(Uri url, ServiceType serviceType, RequestType requestType)
        {
            return url.Query.Contains($"service={serviceType}", StringComparison.OrdinalIgnoreCase) &&
                   url.Query.Contains($"request={requestType}", StringComparison.OrdinalIgnoreCase);
        }

        // some of the ows urls we support do return the GetCapabilities, but do not have this specified in the url query.
        public static bool IsSupportedGetCapabilitiesUrl(Uri url, string bodyContents, ServiceType serviceType)
        {
            if (IsValidUrl(url, serviceType, RequestType.GetCapabilities))
            {
                return true;
            }

            var serviceTypeString = serviceType.ToString().ToUpper();

            // light weight -and rather ugly- check if this is a capabilities file without parsing the XML
            // Body should contain ("<WMS_Capabilities") || contents.Contains("<wms:WMS_Capabilities") for wms
            // Body should contain ("<WFS_Capabilities") || contents.Contains("<wfs:WFS_Capabilities") for wfs
            return bodyContents.Contains($"<{serviceTypeString}_Capabilities") || bodyContents.Contains($"<{serviceTypeString.ToLower()}:{serviceTypeString}_Capabilities");
        }

        public static string GetParameterFromURL(string url, string parameter)
        {
            int queryParametersStart = url.IndexOf('?');
            string query = string.Empty;
            if (queryParametersStart >= 0 && queryParametersStart != url.Length - 1)
            {
                query = url.Substring(queryParametersStart + 1);
            }

            var queryParameters = QueryString.Decode(query);
            return queryParameters.Single(parameter);
        }
    }
}
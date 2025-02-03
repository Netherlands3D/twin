using System;

namespace Netherlands3D.Functionalities.OgcWebServices.Shared
{
    public static class OgcCWebServicesUtility
    {
        public static string CreateGetCapabilitiesURL(string url, string serviceType)
        {
            var uri = new Uri(url);
            var baseUrl = uri.GetLeftPart(UriPartial.Path);
            return $"{baseUrl}?request=GetCapabilities&service={serviceType}";
        }

        public static bool IsValidURL(string url, ServiceType serviceType)
        {
            return url.Contains($"service={serviceType.ToString()}", StringComparison.OrdinalIgnoreCase);
        }
        
        public static bool IsSupportedUrl(Uri url, ServiceType serviceType, RequestType requestType)
        {
            return url.Query.Contains($"service={serviceType.ToString()}") && url.Query.Contains($"request={requestType.ToString()}", StringComparison.OrdinalIgnoreCase);
        }
    }
}

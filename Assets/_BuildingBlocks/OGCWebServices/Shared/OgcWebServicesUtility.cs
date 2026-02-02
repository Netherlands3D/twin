using System;
using System.Collections.Generic;
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
        
        /// <summary>
        /// Makes all query parameters' keys lowercase. Values cannot be made lowercase, as the capitalisation of the
        /// values _does_ matter!
        ///
        /// Even though the HTTP specification states that keys in query parameters are case-sensitive and you normally
        /// do not want to change the case of these, the OWS/WMS/WFS specification states that query parameter keys
        /// should be treated case-insensitive.
        ///
        /// This means that any form of capitalisation could be received from the end-user, making it hard to identify
        /// or replace values by their keys.
        ///
        /// This method will make all query parameter keys lowercase, ensuring a predictable case for all further
        /// operations.
        ///
        /// Should a key exist multiple times in different capitalisations - they are merged as one according to the
        /// HTTP spec's rules. Thus "SERVICE=wms&service=wfs" becomes "service=wms&service=wfs".
        /// </summary>
        /// <param name="sourceUrl">The URL to normalize</param>
        /// <returns>A Uri object with the normalized URL.</returns>
        public static Uri NormalizeUrl(string sourceUrl)
        {
            var urlBuilder = new UriBuilder(sourceUrl);
            
            // Decode the query parameters, ensuring that characters are properly decoded as well.
            var queryParameters = QueryString.Decode(urlBuilder.Query);
            
            // Build a hashset of all keys that are not lowercase - this will prevent exceptions because you cannot 
            // modify a dictionary while iterating it.
            HashSet<string> parametersToBeReplaced = new HashSet<string>();
            foreach (var queryParameter in queryParameters)
            {
                if (queryParameter.Key == queryParameter.Key.ToLower()) continue;
                parametersToBeReplaced.Add(queryParameter.Key);
            }

            // Iterate through all non-lowercase keys and replace them with a lowercase key.
            foreach (var queryParameterKey in parametersToBeReplaced)
            {
                var value = queryParameters[queryParameterKey];
                queryParameters.Remove(queryParameterKey);
                
                var lowercaseKey = queryParameterKey.ToLower();

                // if the lowercase version doesn't exist yet - add it, or ...
                if (!queryParameters.TryGetValue(lowercaseKey, out var existingValue))
                {
                    queryParameters.Add(lowercaseKey, value);
                    continue;
                }

                // ... when it does exist, merge the new one into it
                foreach (var part in value.Values)
                {
                    existingValue.Add(part);
                }
            }
            
            // Encode it again and inject it into the given url.
            urlBuilder.Query = QueryString.Encode(queryParameters);

            return urlBuilder.Uri;
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

        public static string GetVersionFromUrl(Uri url)
        {
            var urlLower = url.ToString().ToLower();
            var versionQueryKey = "version=";
            if (urlLower.Contains(versionQueryKey))
                return urlLower.Split(versionQueryKey)[1].Split("&")[0];
            return null;
        }
    }
}
using System;
using KindMen.Uxios;
using KindMen.Uxios.Http;

namespace Netherlands3D.Web
{
    public static class UriExtensionMethods
    {
        [Obsolete("Use Uxios' QueryString and QueryParameters.Add directly instead")]
        public static void AddQueryParameter(this UriBuilder uriBuilder, string key, string value)
        {
            var queryParameters = new QueryParameters(uriBuilder.Query);
            queryParameters.Add(key, value);

            uriBuilder.Query = QueryString.Encode(queryParameters);
        }

        /// <summary>
        /// Set a query parameter, if it already exists it will be replaced and casing will be reused.
        /// This can be useful for services that are case sensitive.
        /// </summary>
        [Obsolete("Use Uxios' QueryString and QueryParameters.Set directly instead")]
        public static void SetQueryParameter(this UriBuilder uriBuilder, string key, string value)
        {
            var queryParameters = QueryString.Decode(uriBuilder.Query);

            // If a pre-existing value exists and it is all-caps, assume that the new value 
            // should be uppercase as well because of quirks between map servers
            if (queryParameters.TryGetValue(key, out var queryParameter))
            {
                if (queryParameter.Single == queryParameter.Single.ToUpper())
                {
                    value = value.ToUpper();
                }
            }

            queryParameters.Set(key, value);

            uriBuilder.Query = QueryString.Encode(queryParameters);
        }

        [Obsolete("Use Uxios' QueryString and QueryParameters.Remove directly instead")]
        public static void RemoveQueryParameter(this UriBuilder uriBuilder, string key)
        {
            var queryParameters = QueryString.Decode(uriBuilder.Query);
            queryParameters.Remove(key);

            uriBuilder.Query = QueryString.Encode(queryParameters);
        }
    }
}

using System;
using System.Collections.Specialized;
using System.Linq;
using UnityEngine.Networking;

namespace Netherlands3D.Web
{
    public static class UriExtensionMethods
    {
        public static void AddQueryParameter(this UriBuilder uriBuilder, string key, string value)
        {
            var nameValueCollection = new NameValueCollection();
            QueryStringAsNameValueCollection(uriBuilder.Query, nameValueCollection);

            nameValueCollection.Add(key, value);

            uriBuilder.Query = nameValueCollection.ToQueryString();
        }

        public static void AddQueryParameter(this UriBuilder uriBuilder, UriQueryParameter parameter)
        {
            AddQueryParameter(uriBuilder, parameter.Key, parameter.Value);
        }

        /// <summary>
        /// Set a query parameter, if it already exists it will be replaced and casing will be reused.
        /// This can be useful for services that are case sensitive.
        /// </summary>
        public static void SetQueryParameter(this UriBuilder uriBuilder, string key, string value)
        {
            var query = uriBuilder.Query;

            var nameValueCollection = new NameValueCollection();
            QueryStringAsNameValueCollection(query, nameValueCollection);

            // If a pre-existing value exists and it is all-caps, assume that the new value 
            // should be uppercase as well because of quirks between map servers
            if (nameValueCollection.AllKeys.Contains(key))
            {
                var existingValue = nameValueCollection[key];
                if (existingValue == existingValue.ToUpper())
                {
                    value = value.ToUpper();
                }
            }

            nameValueCollection.Remove(key);
            nameValueCollection.Add(key, value);

            uriBuilder.Query = nameValueCollection.ToQueryString();
        }

        public static void RemoveQueryParameter(this UriBuilder uriBuilder, string key)
        {
            var query = uriBuilder.Query;

            var nameValueCollection = new NameValueCollection();
            QueryStringAsNameValueCollection(query, nameValueCollection);

            nameValueCollection.Remove(key);

            uriBuilder.Query = nameValueCollection.ToQueryString();
        }

        private static string ToQueryString(this NameValueCollection nameValueCollection)
        {
            var escapedQueryParameters = nameValueCollection.AllKeys
                .Select(key => $"{UnityWebRequest.EscapeURL(key)}={UnityWebRequest.EscapeURL(nameValueCollection[key])}");

            return string.Join("&", escapedQueryParameters);
        }

        /// <summary>
        /// Attempts to parse the query string and append the found elements as to the given NameValueCollection.
        /// </summary>
        public static void TryParseQueryString(this UriBuilder uriBuilder, NameValueCollection nameValueCollection)
        {
            QueryStringAsNameValueCollection(uriBuilder.Query, nameValueCollection);
        }

        /// <summary>
        /// Attempts to parse the query string and append the found elements as to the given NameValueCollection.
        /// </summary>
        public static void TryParseQueryString(this Uri uri, NameValueCollection nameValueCollection)
        {
            QueryStringAsNameValueCollection(uri.Query, nameValueCollection);
        }

        /// <see href="https://gist.github.com/ranqn/d966423305ce70cbc320f319d9485fa2" />
        private static void QueryStringAsNameValueCollection(string query, NameValueCollection result)
        {
            if (string.IsNullOrEmpty(query)) return;

            var decodedLength = query.Length;
            var namePos = 0;
            var first = true;

            while (namePos <= decodedLength)
            {
                int valuePos = -1, valueEnd = -1;
                for (var q = namePos; q < decodedLength; q++)
                {
                    if ((valuePos == -1) && (query[q] == '='))
                    {
                        valuePos = q + 1;
                        continue;
                    }

                    if (query[q] != '&') continue;

                    valueEnd = q;
                    break;
                }

                if (first)
                {
                    first = false;
                    if (query[namePos] == '?')
                        namePos++;
                }

                string name;
                if (valuePos == -1)
                {
                    name = null;
                    valuePos = namePos;
                }
                else
                {
                    name = UnityWebRequest.UnEscapeURL(query.Substring(namePos, valuePos - namePos - 1));
                }

                if (valueEnd < 0)
                {
                    namePos = -1;
                    valueEnd = query.Length;
                }
                else
                {
                    namePos = valueEnd + 1;
                }

                var value = UnityWebRequest.UnEscapeURL(query.Substring(valuePos, valueEnd - valuePos));

                result.Add(name, value);
                if (namePos == -1)
                    break;
            }
        }
    }
}

using System;
using System.Collections.Specialized;
using KindMen.Uxios;
using Netherlands3D.Web;

namespace Netherlands3D.Credentials.StoredAuthorization
{
    [Serializable]
    public class InferableSingleKey : StoredAuthorization
    {
        public string key = "";
        public virtual string queryKeyName { get; protected set; } = "";

        public string GetQueryString()
        {
            return $"?{queryKeyName}={key}";
        }

        public override AuthorizationType AuthorizationType => AuthorizationType.InferableSingleKey;

        public InferableSingleKey(Uri url, string key) : base(url)
        {
            this.key = key;
        }

        public override Uri GetUriWithCredentials()
        {
            if (string.IsNullOrEmpty(queryKeyName))
                return baseUri;

            var uriBuilder = new UriBuilder(baseUri);
            uriBuilder.SetQueryParameter(queryKeyName, key);
            return uriBuilder.Uri;
        }
    }
}

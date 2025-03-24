using System;
using Netherlands3D.Web;

namespace Netherlands3D.Credentials.StoredAuthorization
{
    [Serializable]
    public class QueryStringAuthorization : StoredAuthorization
    {
        public string key = "";
        public virtual string queryKeyName => "";
        public override AuthorizationType AuthorizationType => AuthorizationType.InferableSingleKey; //todo: once this enum is removed, this class can (and should) become abstract

        public QueryStringAuthorization(Uri url, string key) : base(url)
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

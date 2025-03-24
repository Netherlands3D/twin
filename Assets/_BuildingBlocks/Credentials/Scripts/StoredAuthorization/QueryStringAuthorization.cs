using System;
using Netherlands3D.Web;

namespace Netherlands3D.Credentials.StoredAuthorization
{
    [Serializable]
    public class QueryStringAuthorization : StoredAuthorization
    {
        public string QueryKeyValue { get; } = "";
        public virtual string QueryKeyName => "";
        public override AuthorizationType AuthorizationType => AuthorizationType.InferableSingleKey; //todo: once this enum is removed, this class can (and should) become abstract

        public QueryStringAuthorization(Uri url, string key ) : base(url)
        {
            QueryKeyValue = key;
        }

        public override Uri GetUriWithCredentials()
        {
            if (string.IsNullOrEmpty(QueryKeyName))
                return BaseUri;

            var uriBuilder = new UriBuilder(BaseUri);
            uriBuilder.SetQueryParameter(QueryKeyName, QueryKeyValue);
            return uriBuilder.Uri;
        }
    }
}

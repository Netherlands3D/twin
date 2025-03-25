using System;
using Netherlands3D.Web;

namespace Netherlands3D.Credentials.StoredAuthorization
{
    [Serializable]
    public abstract class QueryStringAuthorization : StoredAuthorization
    {
        public string QueryKeyValue { get; } = "";
        public virtual string QueryKeyName => "";

        protected QueryStringAuthorization(Uri url, string key ) : base(url)
        {
            QueryKeyValue = key;
        }

        public override Uri GetFullUri()
        {
            if (string.IsNullOrEmpty(QueryKeyName))
                throw new Exception("The Query name should be overriden to provide a value in the inherited class, it is still set to an empty string");
            
            var uriBuilder = new UriBuilder(inputUri);
            uriBuilder.SetQueryParameter(QueryKeyName, QueryKeyValue);
            return uriBuilder.Uri;
        }
    }
}

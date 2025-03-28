using System;
using KindMen.Uxios;
using KindMen.Uxios.Http;
using Netherlands3D.Web;

namespace Netherlands3D.Credentials.StoredAuthorization
{
    [Serializable]
    public abstract class QueryStringAuthorization : StoredAuthorization
    {
        public string QueryKeyValue { get; } = "";
        public abstract string QueryKeyName { get; }

        protected QueryStringAuthorization(Uri url, string key ) : base(url)
        {
            QueryKeyValue = key;
        }

        public override Config GetConfig()
        {
            return new Config()
            {
                Params = new QueryParameters(){ {QueryKeyName, QueryKeyValue} }
            };
        }
        
        public Uri GetFullUri(Uri uri) //you have to provide a Uri because it can contain other query parameters that we don't want to touch
        {
            if (string.IsNullOrEmpty(QueryKeyName))
                throw new Exception("The Query name should be overriden to provide a value in the inherited class, it is still set to an empty string");
            
            var uriBuilder = new UriBuilder(uri);
            uriBuilder.SetQueryParameter(QueryKeyName, QueryKeyValue);
            return uriBuilder.Uri;
        }
    }
}

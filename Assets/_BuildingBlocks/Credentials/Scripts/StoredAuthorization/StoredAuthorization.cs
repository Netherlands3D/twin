using System;

namespace Netherlands3D.Credentials.StoredAuthorization
{
    [Serializable]
    public abstract class StoredAuthorization
    {
        public Uri baseUri;
        public abstract Uri GetUriWithCredentials();

        protected StoredAuthorization(Uri uri)
        {
            baseUri = new Uri(uri.GetLeftPart(UriPartial.Path));
        }

        public abstract AuthorizationType AuthorizationType { get; }
    }
}
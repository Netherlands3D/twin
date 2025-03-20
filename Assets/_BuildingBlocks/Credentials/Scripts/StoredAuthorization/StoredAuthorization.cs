using System;

namespace Netherlands3D.Credentials.StoredAuthorization
{
    [Serializable]
    public abstract class StoredAuthorization
    {
        public Uri baseUri;
        protected Uri inputUri; //the initial endpoint
        public virtual string headerName { get; protected set; } = "Authorization";
        public abstract Uri GetUriWithCredentials();

        protected StoredAuthorization(Uri uri)
        {
            inputUri = uri;
            baseUri = new Uri(uri.GetLeftPart(UriPartial.Path));
        }

        public void SetInputUri(Uri uri)
        {
            inputUri = uri;
        }

        public abstract AuthorizationType AuthorizationType { get; }
    }
}
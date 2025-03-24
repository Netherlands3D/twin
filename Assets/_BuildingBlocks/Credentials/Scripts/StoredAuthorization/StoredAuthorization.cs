using System;

namespace Netherlands3D.Credentials.StoredAuthorization
{
    [Serializable]
    public abstract class StoredAuthorization
    {
        public Uri BaseUri { get; }
        protected Uri inputUri; //the initial endpoint

        protected StoredAuthorization(Uri uri)
        {
            inputUri = uri;
            BaseUri = new Uri(uri.GetLeftPart(UriPartial.Path));
        }

        public void SetInputUri(Uri uri)
        {
            inputUri = uri;
        }
    }
}
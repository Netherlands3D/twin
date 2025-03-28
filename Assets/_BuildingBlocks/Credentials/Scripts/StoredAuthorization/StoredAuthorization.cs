using System;
using KindMen.Uxios;

namespace Netherlands3D.Credentials.StoredAuthorization
{
    [Serializable]
    public abstract class StoredAuthorization
    {
        public Uri BaseUri { get; }
        public Uri InputUri { get; } //the uri received from the user

        protected StoredAuthorization(Uri uri)
        {
            InputUri = uri;
            BaseUri = new Uri(uri.GetLeftPart(UriPartial.Path));
        }

        public abstract Config GetConfig();
    }
}
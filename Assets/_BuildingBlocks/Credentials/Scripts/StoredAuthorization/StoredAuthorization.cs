using System;
using KindMen.Uxios;

namespace Netherlands3D.Credentials.StoredAuthorization
{
    [Serializable]
    public abstract class StoredAuthorization
    {
        public Uri BaseUri { get; }

        protected StoredAuthorization(Uri uri)
        {
            BaseUri = new Uri(uri.GetLeftPart(UriPartial.Path));
        }

        public abstract Config GetConfig();
    }
}
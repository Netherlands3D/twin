using System;
using KindMen.Uxios;

namespace Netherlands3D.Credentials.StoredAuthorization
{
    [Serializable]
    public abstract class StoredAuthorization
    {
        public Uri Domain { get; }

        protected StoredAuthorization(Uri uri)
        {
            Domain = new Uri(uri.GetLeftPart(UriPartial.Path));
        }

        public virtual Uri SanitizeUrl(Uri uri)
        {
            return uri;
        }

        public abstract Config AddToConfig(Config config);
    }
}
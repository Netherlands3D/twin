
using System;

namespace Netherlands3D.Credentials.StoredAuthorization
{
    public class Public : StoredAuthorization
    {
        public override AuthorizationType AuthorizationType => AuthorizationType.Public;

        public Public(Uri url) : base(url)
        {
        }

        public override Uri GetUriWithCredentials()
        {
            return baseUri;
        }
    }
}

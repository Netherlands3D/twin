
using System;

namespace Netherlands3D.Credentials.StoredAuthorization
{
    public class Public : StoredAuthorization
    {        
        public override AuthorizationType AuthorizationType => AuthorizationType.Public;
        private Uri publicUri;

        public Public(Uri url) : base(url)
        {
            publicUri = url;
        }

        public override Uri GetUriWithCredentials()
        {
            return publicUri;
        }
    }
}

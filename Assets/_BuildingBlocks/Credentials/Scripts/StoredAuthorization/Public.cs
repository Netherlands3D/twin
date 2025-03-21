
using System;

namespace Netherlands3D.Credentials.StoredAuthorization
{
    [Serializable]
    public class Public : StoredAuthorization
    {        
        public override AuthorizationType AuthorizationType => AuthorizationType.Public;
       

        public Public(Uri url) : base(url)
        {
            
        }

        public override Uri GetUriWithCredentials()
        {
            return inputUri;
        }
    }
}

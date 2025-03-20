using System;

namespace Netherlands3D.Credentials.StoredAuthorization
{
    //this type is returned when all supported authorization types fail (403 error). This means either the provided credentials are invalid, or we do not support the authorization type of this url. 
    //todo: try to determine the difference between wrong credentials and unsupported authorization types
    [Serializable]
    public class FailedOrUnsupported : StoredAuthorization
    {
        public override AuthorizationType AuthorizationType => AuthorizationType.FailedOrUnsupported;

        public FailedOrUnsupported(Uri uri) : base(uri)
        {
        }
        
        public override Uri GetUriWithCredentials()
        {
            return baseUri;
        }
    }
}
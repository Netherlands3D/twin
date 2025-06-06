using System;
using KindMen.Uxios;

namespace Netherlands3D.Credentials.StoredAuthorization
{
    //this type is returned when all supported authorization types fail (401 or 403 error). This means either the provided credentials are invalid, or we do not support the authorization type of this url. 
    [Serializable]
    public class FailedOrUnsupported : StoredAuthorization
    {
        public FailedOrUnsupported(Uri uri) : base(uri)
        {
        }

        public override Config AddToConfig(Config config)
        {
            return config;
        }
    }
}
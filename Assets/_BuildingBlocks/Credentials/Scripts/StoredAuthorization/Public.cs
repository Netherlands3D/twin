
using System;
using KindMen.Uxios;

namespace Netherlands3D.Credentials.StoredAuthorization
{
    [Serializable]
    public class Public : StoredAuthorization
    {        
        public Public(Uri url) : base(url)
        {
        }

        public override Config AddToConfig(Config config)
        {
            return config;
        }
    }
}


using System;

namespace Netherlands3D.Credentials.StoredAuthorization
{
    [Serializable]
    public class Public : StoredAuthorization
    {        
        public Public(Uri url) : base(url)
        {
        }
    }
}

using System;

namespace Netherlands3D.Credentials.StoredAuthorization
{
    [Serializable]
    public class Key : QueryStringAuthorization
    {
        public override string QueryKeyName => "key";
        
        public Key(Uri url, string key) : base(url, key)
        {
        }
    }
}
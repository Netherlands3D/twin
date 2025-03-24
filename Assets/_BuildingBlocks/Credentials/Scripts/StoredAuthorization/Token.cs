using System;

namespace Netherlands3D.Credentials.StoredAuthorization
{
    [Serializable]
    public class Token : QueryStringAuthorization
    {
        public override string QueryKeyName => "token";

        public Token(Uri url, string key) : base(url, key)
        {
        }
    }
}
using System;

namespace Netherlands3D.Credentials.StoredAuthorization
{
    [Serializable]
    public class Token : QueryStringAuthorization
    {
        public override AuthorizationType AuthorizationType => AuthorizationType.Token;
        public override string queryKeyName => "token";

        public Token(Uri url, string key) : base(url, key)
        {
        }
    }
}
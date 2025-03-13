using System;

namespace Netherlands3D.Credentials.StoredAuthorization
{
    [Serializable]
    public class Token : InferableSingleKey
    {
        public override AuthorizationType AuthorizationType => AuthorizationType.Token;
        public override string queryKeyName { get; protected set; } = "token";

        public Token(Uri url, string key) : base(url, key)
        {
        }
    }
}
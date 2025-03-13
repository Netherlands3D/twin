using System;

namespace Netherlands3D.Credentials.StoredAuthorization
{
    [Serializable]
    public class Key : InferableSingleKey
    {
        public override AuthorizationType AuthorizationType => AuthorizationType.Key;
        public override string queryKeyName { get; protected set; } = "key";
        
        public Key(Uri url, string key) : base(url, key)
        {
        }
    }
}
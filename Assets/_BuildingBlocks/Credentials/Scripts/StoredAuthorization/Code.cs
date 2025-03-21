using System;

namespace Netherlands3D.Credentials.StoredAuthorization
{
    [Serializable]
    public class Code : QueryStringAuthorization
    {
        public override AuthorizationType AuthorizationType => AuthorizationType.Code;
        public override string queryKeyName { get; protected set; } = "code";

        public Code(Uri url, string key) : base(url, key)
        {
        }
    }
}
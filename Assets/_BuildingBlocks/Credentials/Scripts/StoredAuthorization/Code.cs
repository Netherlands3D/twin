using System;

namespace Netherlands3D.Credentials.StoredAuthorization
{
    [Serializable]
    public class Code : QueryStringAuthorization
    { 
        public override string QueryKeyName => "code";

        public Code(Uri url, string queryKeyValue) : base(url, queryKeyValue)
        {
        }
    }
}
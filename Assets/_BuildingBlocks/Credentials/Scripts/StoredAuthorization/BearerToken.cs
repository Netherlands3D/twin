using System;
using UnityEngine.Networking;

namespace Netherlands3D.Credentials.StoredAuthorization
{
    [Serializable]
    public class BearerToken : HeaderBasedAuthorization
    {
        protected override string headerPrefix => "Bearer ";
        
        public BearerToken(Uri url, string key) : base(url, key)
        {
        }

        public override (string, string) GetHeaderKeyAndValue()
        {
            return (headerName, headerPrefix + key);
        }
    }
}
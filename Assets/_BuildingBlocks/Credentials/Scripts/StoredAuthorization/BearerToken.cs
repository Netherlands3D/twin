using System;
using UnityEngine.Networking;

namespace Netherlands3D.Credentials.StoredAuthorization
{
    [Serializable]
    public class BearerToken : HeaderBasedAuthorization
    {
        private string headerPrefix = "Bearer ";
        public override AuthorizationType AuthorizationType => AuthorizationType.BearerToken;
        
        public BearerToken(Uri url, string key) : base(url, key)
        {
        }

        public UnityWebRequest GetWebRequestWithHeader()
        {
            var uwr = new UnityWebRequest();
            uwr.SetRequestHeader(headerName, headerPrefix + key);
            return uwr;
        }
    }
}

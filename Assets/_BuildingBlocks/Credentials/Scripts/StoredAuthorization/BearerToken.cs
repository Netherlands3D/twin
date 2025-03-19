using System;
using UnityEngine.Networking;

namespace Netherlands3D.Credentials.StoredAuthorization
{
    [Serializable]
    public class BearerToken : InferableSingleKey
    {
        public override AuthorizationType AuthorizationType => AuthorizationType.BearerToken;
        
        public BearerToken(Uri url, string key) : base(url, key)
        {
        }

        public UnityWebRequest GetWebRequestWithHeader()
        {
            var uwr = new UnityWebRequest();
            uwr.SetRequestHeader("Authorization", "Bearer " + key);
            return uwr;
        }
        
        public override Uri GetUriWithCredentials()
        {
            return baseUri;
        }
    }
}

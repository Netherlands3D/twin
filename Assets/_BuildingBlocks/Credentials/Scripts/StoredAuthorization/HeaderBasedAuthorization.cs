using System;

namespace Netherlands3D.Credentials.StoredAuthorization
{
    [Serializable]
    public class HeaderBasedAuthorization : StoredAuthorization
    {
        public string key = "";        

        public override AuthorizationType AuthorizationType => AuthorizationType.InferableSingleKey;

        public HeaderBasedAuthorization(Uri url, string key) : base(url)
        {
            this.key = key;
        }

        public override Uri GetUriWithCredentials()
        {
            return baseUri;
        }
    }
}

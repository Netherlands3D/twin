using System;

namespace Netherlands3D.Credentials.StoredAuthorization
{
    [Serializable]
    public abstract class HeaderBasedAuthorization : StoredAuthorization
    {
        protected string key = "";
        protected virtual string headerPrefix => "";
        protected virtual string headerName => "Authorization";

        public override AuthorizationType AuthorizationType => AuthorizationType.InferableSingleKey;

        public HeaderBasedAuthorization(Uri url, string key) : base(url)
        {
            this.key = key;
        }

        public override Uri GetUriWithCredentials()
        {
            return baseUri;
        }

        public abstract (string, string) GetHeaderKeyAndValue();
    }
}

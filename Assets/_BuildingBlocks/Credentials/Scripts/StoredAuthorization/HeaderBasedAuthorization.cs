using System;

namespace Netherlands3D.Credentials.StoredAuthorization
{
    [Serializable]
    public abstract class HeaderBasedAuthorization : StoredAuthorization
    {
        protected string key = "";
        protected virtual string headerPrefix => "";
        protected virtual string headerName => "Authorization";
        
        protected HeaderBasedAuthorization(Uri url, string key) : base(url)
        {
            this.key = key;
        }

        public abstract (string, string) GetHeaderKeyAndValue();
    }
}

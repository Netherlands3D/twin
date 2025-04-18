using System;
using KindMen.Uxios;
using KindMen.Uxios.Http;

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
        
        public override Config AddToConfig(Config config)
        {
            var newConfig = Config.BasedOn(config);
            var (headerKey, headerValue) = GetHeaderKeyAndValue();
            newConfig.AddHeader(headerKey, headerValue);
            return newConfig;
        }
    }
}

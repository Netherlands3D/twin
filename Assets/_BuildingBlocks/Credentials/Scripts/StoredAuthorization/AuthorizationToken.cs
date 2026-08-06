using System;
using KindMen.Uxios;
using UnityEngine;

namespace Netherlands3D.Credentials.StoredAuthorization
{
    [Serializable]
    public class AuthorizationToken : HeaderBasedAuthorization
    {
        public AuthorizationToken(Uri url, string key) : base(url, key)
        {
        }

        public override (string, string) GetHeaderKeyAndValue()
        {
            return (headerName, key);
        }
        
        public override Config AddToConfig(Config config)
        {
            var newConfig = Config.BasedOn(config);
            var (headerKey, headerValue) = GetHeaderKeyAndValue();

            Debug.Log($"AUTH HEADER: {headerKey} = {headerValue}");

            newConfig.AddHeader(headerKey, headerValue);
            return newConfig;
        }
    }
}
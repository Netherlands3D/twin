using System;
using KindMen.Uxios;
using UnityEngine;

namespace Netherlands3D.Credentials.StoredAuthorization
{
    [Serializable]
    public class AuthorizationToken : HeaderBasedAuthorization
    {
        protected override string headerPrefix => "";

        public AuthorizationToken(Uri url, string key) : base(url, key)
        {
        }

        public override (string, string) GetHeaderKeyAndValue()
        {
            return (headerName, key);
        }
    }
}
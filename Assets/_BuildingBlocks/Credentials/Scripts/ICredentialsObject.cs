using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Credentials
{
    public interface ICredentialsObject
    {
        public string URL { get; set; } //The base URL to check the credentials for

        //Header based credentials
        public void SetCredentials(string username, string password);
        public void SetBearerToken(string token);

        //Url Query parameters
        public void SetKey(string key);
        public void SetToken(string token);
        public void SetCode(string code);

        public void ClearCredentials();
    }
}

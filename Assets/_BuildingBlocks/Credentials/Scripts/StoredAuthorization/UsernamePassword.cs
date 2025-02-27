using System;
using UnityEngine.Networking;

namespace Netherlands3D.Credentials.StoredAuthorization
{
    [Serializable]
    public class UsernamePassword : StoredAuthorization
    {
        public string username;
        public string password;

        public UsernamePassword(Uri url, string username, string password) : base(url)
        {
            this.username = username;
            this.password = password;
        }
        
        public override AuthorizationType AuthorizationType => AuthorizationType.UsernamePassword;
        
        public UnityWebRequest GetWebRequestWithHeader()
        {
            var uwr = new UnityWebRequest();
            uwr.SetRequestHeader("Authorization", "Basic " + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(username + ":" + password)));
            return uwr;
        }
        
        public override Uri GetUriWithCredentials()
        {
            return baseUri;
        }
    }
}

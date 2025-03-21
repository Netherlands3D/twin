using System;
using UnityEngine.Networking;

namespace Netherlands3D.Credentials.StoredAuthorization
{
    [Serializable]
    public class UsernamePassword : HeaderBasedAuthorization
    {
        public string username;
        public string password => key;
        public override string headerPrefix { get; protected set; } = "Basic ";
        public override AuthorizationType AuthorizationType => AuthorizationType.UsernamePassword;

        public UsernamePassword(Uri url, string username, string password) : base(url, password)
        {
            this.username = username;
        }
        
        
        public UnityWebRequest GetWebRequestWithHeader()
        {
            var uwr = new UnityWebRequest();
            uwr.SetRequestHeader(headerName, headerPrefix + GetUserNamePassWordQuery());
            return uwr;
        }

        public string GetUserNamePassWordQuery()
        {
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(username + ":" + password));
        }
    }
}

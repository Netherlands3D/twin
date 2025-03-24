using System;
using UnityEngine.Networking;

namespace Netherlands3D.Credentials.StoredAuthorization
{
    [Serializable]
    public class UsernamePassword : HeaderBasedAuthorization
    {
        protected string username;
        protected string password => key;
        protected override string headerPrefix => "Basic ";
        
        public UsernamePassword(Uri url, string username, string password) : base(url, password)
        {
            this.username = username;
        }
        
        public override (string, string) GetHeaderKeyAndValue()
        {
            var usernamePasswordEncoding = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(username + ":" + password));
            return (headerName, headerPrefix + usernamePasswordEncoding);
        }
    }
}

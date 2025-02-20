using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Credentials
{
    public interface ICredentialHandler
    {
        public bool StatusEnabled { get; }
        public AuthorizationType AuthorizationType { get; }
        public string UserName { get; set; }
        public string PasswordOrKeyOrTokenOrCode { get; set; }
        UnityEvent<bool> CredentialsAccepted { get; }
        bool HasValidCredentials { get; }
        public void ApplyCredentials();
        public void SetAuthorizationInputType(AuthorizationType type);
    }
}

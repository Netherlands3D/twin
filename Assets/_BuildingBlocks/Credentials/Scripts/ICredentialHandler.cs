using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Netherlands3D.Credentials
{
    public interface ICredentialHandler
    {
        public Uri BaseUri { get; set; } //The base URL to check the credentials for
        // public UnityEvent<Uri> OnURLChanged { get; }
        public string UserName { get; set; }
        public string PasswordOrKeyOrTokenOrCode { get; set; }
        public UnityEvent<StoredAuthorization.StoredAuthorization> OnAuthorizationHandled { get; set; } //called when the keyVault completes its authorisation process. In case the returned authorization object is of type FailedOrUnsupported, the authorization failed.
        public StoredAuthorization.StoredAuthorization Authorization { get; set; }
        public void ApplyCredentials();
        public void ClearCredentials();

        // public bool StatusEnabled { get; }
        // public string UserName { get; set; }
        // public string PasswordOrKeyOrTokenOrCode { get; set; }
        // UnityEvent<bool> CredentialsAccepted { get; }
        // bool HasValidCredentials { get; }
        // public void SetAuthorizationInputType(AuthorizationType type);
        // public void UpdateUrl(string url); 
        
        // public UnityEvent<UnityWebRequest> OnServerResponseReceived { get; }


        
        // //Header based credentials
        // public void SetCredentials(string username, string password);
        // public void SetBearerToken(string token);
        //
        // //Url Query parameters
        // public void SetKey(string key);
        // public void SetToken(string token);
        // public void SetCode(string code);

    }
}

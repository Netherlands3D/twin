using System;
using UnityEngine.Events;

namespace Netherlands3D.Credentials
{
    public interface ICredentialHandler
    {
        public Uri Uri { get; set; } //The URL to check the credentials for
        public string UserName { get; set; }
        public string PasswordOrKeyOrTokenOrCode { get; set; }
        public UnityEvent<Uri, StoredAuthorization.StoredAuthorization> OnAuthorizationHandled { get; set; } //called when the keyVault completes its authorisation process. In case the returned authorization object is of type FailedOrUnsupported, the authorization failed.
        public StoredAuthorization.StoredAuthorization Authorization { get; set; }
        public void ApplyCredentials();
        public void ClearCredentials();

    }
}

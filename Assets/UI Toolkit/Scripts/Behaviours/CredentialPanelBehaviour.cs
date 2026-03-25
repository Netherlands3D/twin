using System;
using Netherlands3D.Credentials;
using Netherlands3D.Credentials.StoredAuthorization;
using Netherlands3D.UI.Panels;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Behaviours
{
    [RequireComponent(typeof(UIDocument), typeof(CredentialHandler))]
    public class CredentialPanelBehaviour : MonoBehaviour
    {
        private UIDocument appDocument;
        public UnityEvent<Uri, StoredAuthorization> OnAuthorizationHandled;
        public CredentialHandler CredentialHandler => credentialHandler;
        
    
        private VisualElement root;
        private VisualElement Root => root ??= appDocument?.rootVisualElement;
        
        //there should always be only one available from the root
        private CredentialPanel panel;
        private CredentialPanel Panel => panel ??= Root?.Q<CredentialPanel>();
        
        private CredentialHandler credentialHandler;
        
        private void Awake()
        {
            appDocument = GetComponent<UIDocument>();
            credentialHandler = GetComponent<CredentialHandler>();
        }

        private void Start()
        {
            credentialHandler.OnAuthorizationHandled.AddListener(ProcessAuthorization);
            Panel.OnConfirm += ApplyCredentials;
        }

        private void OnDestroy()
        {
            credentialHandler.OnAuthorizationHandled.RemoveListener(ProcessAuthorization);
            Panel.OnConfirm -= ApplyCredentials;
        }

        private void ApplyCredentials()
        {
            credentialHandler.UserName = ""; //userNameInputField.text;
            credentialHandler.PasswordOrKeyOrTokenOrCode = Panel.KeyField.value;
            credentialHandler.ApplyCredentials();
        }
        
        private void ProcessAuthorization(Uri uri, StoredAuthorization auth)
        {
            if (auth is FailedOrUnsupported)
            {
                //3b. if no: set UI so user inputs credentials and go to step 2
                Panel.Show();
                return;
            }

            //3a. if yes: pass this to the Layer service
            Panel.Hide();
            OnAuthorizationHandled?.Invoke(uri, auth);
        }
    }
}

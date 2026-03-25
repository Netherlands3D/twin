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
        public UnityEvent<Uri, StoredAuthorization> OnAuthorizationHandled;
        public CredentialHandler CredentialHandler => credentialHandler;
        
        private UIDocument appDocument;
    
        private VisualElement root;
        private VisualElement Root => root ??= appDocument?.rootVisualElement;
        
        //there should always be only one available from the root
        private CredentialPanel panel;
        private CredentialPanel Panel => panel ??= Root?.Q<CredentialPanel>();
        
        private CredentialHandler credentialHandler;
        
        
        
        private void Awake()
        {
            credentialHandler = GetComponent<CredentialHandler>();
        }

        private void OnEnable()
        {
            credentialHandler.OnAuthorizationHandled.AddListener(ProcessAuthorization);
        }

        private void OnDisable()
        {
            credentialHandler.OnAuthorizationHandled.RemoveListener(ProcessAuthorization);
        }
        
        private void ProcessAuthorization(Uri uri, StoredAuthorization auth)
        {
            if (auth is FailedOrUnsupported)
            {
                //3b. if no: set UI so user inputs credentials and go to step 2
                //TODO show credentials panel
                return;
            }

            //3a. if yes: pass this to the Layer service
            //TODO close credentials panel
            OnAuthorizationHandled?.Invoke(uri, auth);
        }
    }
}

using System;
using Netherlands3D.Credentials;
using Netherlands3D.Credentials.StoredAuthorization;
using Netherlands3D.UI.Panels;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Behaviours
{
    [RequireComponent(typeof(UIDocument))]
    public class CredentialPanelBehaviour : MonoBehaviour
    {
        private UIDocument appDocument;
        public UnityEvent<Uri, StoredAuthorization> OnAuthorizationHandled;
        public StoredAuthorization Authorization { get; private set; }
        public Uri Uri { get; set; }

        public string UserName { get; set; }
        public string PasswordOrKeyOrTokenOrCode { get; set; }
        
        private VisualElement root;
        private VisualElement Root => root ??= appDocument?.rootVisualElement;
        
        //there should always be only one available from the root
        private CredentialPanel panel;
        private CredentialPanel Panel => panel ??= Root?.Q<CredentialPanel>();
        
        [Tooltip("KeyVault Scriptable Object")] [SerializeField]
        private KeyVault keyVault;
        
        
        private void Awake()
        {
            appDocument = GetComponent<UIDocument>();
            keyVault.OnAuthorizationTypeDetermined.AddListener(DeterminedAuthorizationType);
        }

        private void Start()
        {
            Panel.OnConfirm += ApplyCredentials;
        }

        private void OnDestroy()
        {
            keyVault.OnAuthorizationTypeDetermined.RemoveListener(DeterminedAuthorizationType);
            Panel.OnConfirm -= ApplyCredentials;
        }

        public void ApplyCredentials()
        {
            UserName = panel.UserNameField.value;
            PasswordOrKeyOrTokenOrCode = Panel.CodeField.value;
            keyVault.Authorize(Uri, UserName, PasswordOrKeyOrTokenOrCode);
        }

        //called in the inspector on end edit of url input field
        public void SetUri(string url)
        {
            if (!string.IsNullOrEmpty(url))
                Uri = new Uri(url);
        }

        public void ClearCredentials()
        {
            UserName = "";
            PasswordOrKeyOrTokenOrCode = "";
        }

        private void DeterminedAuthorizationType(StoredAuthorization auth)
        {
            if (Uri == null ||
                auth.Domain !=
                new Uri(Uri.GetLeftPart(UriPartial.Path)) || //ensure the returned authorization is relevant to us
                auth == Authorization) //ensure the new auth is not the same at the one we already have. If it is, we don't need a reload
            {
                return;
            }
            
            Authorization = auth;
            
            if (auth is FailedOrUnsupported)
            {
                Panel.Show();
                return;
            }

            Panel.Hide();
            OnAuthorizationHandled.Invoke(auth.SanitizeUrl(Uri), auth);
        }
    }
}

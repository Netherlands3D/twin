using System;
using System.Collections.Generic;
using Netherlands3D.Credentials;
using Netherlands3D.Credentials.StoredAuthorization;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.Credentials.Properties
{
    [PropertySection(typeof(CredentialsRequiredPropertyData))]
    public class CredentialsValidationPropertySection : MonoBehaviour, ICredentialsPropertySection, IVisualizationWithPropertyData
    {
        private ICredentialHandler handler;

        [SerializeField] private GameObject statusPanel;
        [SerializeField] private GameObject validCredentialsPanel;
        [SerializeField] private GameObject invalidCredentialsPanel;

        public ICredentialHandler Handler
        {
            get => handler;
            set
            {
                handler?.OnAuthorizationHandled.RemoveListener(OnCredentialsHandled);
                handler = value;
                handler.OnAuthorizationHandled.AddListener(OnCredentialsHandled);
            }
        }

        private void Awake()
        {
            Handler = GetComponent<ICredentialHandler>();
        }

        private void Start()
        {
            handler?.OnAuthorizationHandled.AddListener(OnCredentialsHandled);
        }

        private void OnDestroy()
        {
            handler?.OnAuthorizationHandled.RemoveListener(OnCredentialsHandled);
        }

        private void OnCredentialsHandled(Uri uri, StoredAuthorization auth)
        {
            var accepted = auth != null && auth is not FailedOrUnsupported;
            
            validCredentialsPanel.SetActive(accepted);
            invalidCredentialsPanel.SetActive(!accepted);
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            Handler.Uri = properties.Get<LayerURLPropertyData>().Url;
            Handler.ApplyCredentials();
        }
        
        public void ResetStatusPanel(bool validCredentials)
        {
            statusPanel.SetActive(true);
            validCredentialsPanel.SetActive(validCredentials);
            invalidCredentialsPanel.SetActive(!validCredentials);
        }
    }
}
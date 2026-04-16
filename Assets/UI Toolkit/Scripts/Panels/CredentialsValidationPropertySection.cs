using System;
using System.Collections.Generic;
using Netherlands3D.Credentials;
using Netherlands3D.Credentials.StoredAuthorization;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.Credentials.Properties;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    [PropertySection(typeof(CredentialsRequiredPropertyData))]
    public partial class CredentialsValidationPropertySection : VisualElement, IVisualizationWithPropertyData
    {
        private CredentialPanel credentialPanel;
        private CredentialPanel CredentialPanel => credentialPanel ??= this.Q<CredentialPanel>();  
        
        private ICredentialHandler handler;
        
        public CredentialsValidationPropertySection()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");   
            
            CredentialPanel.SetEnabled(true);
            
            handler = new CredentialPropertyHandler();
        }

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
      

        private void OnCredentialsHandled(Uri uri, StoredAuthorization auth)
        {
            var accepted = auth != null && auth is not FailedOrUnsupported;
            
            // if (accepted)//we always want to show the status if credentials are accepted, however we might still want to display the error of the input panel if it was not accepted
            //     statusPanel.SetActive(true);
            //
            // validCredentialsPanel.SetActive(accepted);
            // invalidCredentialsPanel.SetActive(!accepted);
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            Handler.Uri = properties.Get<LayerURLPropertyData>().Url;
            Handler.ApplyCredentials();
        }
        
        // public void ResetStatusPanel(bool validCredentials)
        // {
        //     statusPanel.SetActive(true);
        //     validCredentialsPanel.SetActive(validCredentials);
        //     invalidCredentialsPanel.SetActive(!validCredentials);
        // }
    }
}
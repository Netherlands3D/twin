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
    public partial class CredentialsPropertySection : VisualElement, IVisualizationWithPropertyData
    {
        private CredentialPanel credentialPanel;
        private CredentialPanel CredentialPanel => credentialPanel ??= this.Q<CredentialPanel>();  
        
        
        public CredentialsPropertySection()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");   
            
            CredentialPanel.Show(true);
            
            credentialPanel.Handler?.OnAuthorizationHandled.RemoveListener(OnCredentialsHandled);
            credentialPanel.Handler?.OnAuthorizationUnchanged.RemoveListener(OnCredentialsHandled);
            credentialPanel.Handler = new CredentialPropertyHandler();
            credentialPanel.Handler.OnAuthorizationHandled.AddListener(OnCredentialsHandled);
            credentialPanel.Handler.OnAuthorizationUnchanged.AddListener(OnCredentialsHandled);
            
            RegisterCallback<DetachFromPanelEvent>(evt =>
            {
                CredentialPropertyHandler propertyHandler = credentialPanel.Handler as CredentialPropertyHandler;
                propertyHandler.Dispose();
            });
        }
      

        private void OnCredentialsHandled(Uri uri, StoredAuthorization auth)
        {
            var accepted = auth != null && auth is not FailedOrUnsupported;
            
            //we always want to show the status if credentials are accepted, however we might still want to display the error of the input panel if it was not accepted
            if (accepted)
                CredentialPanel.SetAcceptedState();
            else
                CredentialPanel.ResetState();
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            credentialPanel.Handler.Uri = properties.Get<LayerURLPropertyData>().Url;
            credentialPanel.Handler.ApplyCredentials();
        }
    }
}
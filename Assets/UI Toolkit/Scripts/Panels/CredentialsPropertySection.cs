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
        
        private ICredentialHandler handler;
        
        public CredentialsPropertySection()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");   
            
            CredentialPanel.Show(true);
            
            Handler = new CredentialPropertyHandler();
            CredentialPanel.handler = handler;
            
            RegisterCallback<DetachFromPanelEvent>(evt =>
            {
                CredentialPropertyHandler propertyHandler = Handler as CredentialPropertyHandler;
                propertyHandler.Destroy();
            });
        }

        public ICredentialHandler Handler
        {
            get => handler;
            set
            {
                handler?.OnAuthorizationHandled.RemoveListener(OnCredentialsHandled);
                handler?.OnAuthorizationUnchanged.RemoveListener(OnCredentialsHandled);
                handler = value;
                handler.OnAuthorizationHandled.AddListener(OnCredentialsHandled);
                handler.OnAuthorizationUnchanged.AddListener(OnCredentialsHandled);
            }
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
            Handler.Uri = properties.Get<LayerURLPropertyData>().Url;
            Handler.ApplyCredentials();
        }
    }
}
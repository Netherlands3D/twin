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
    [PropertySection(typeof(CredentialsRequiredPropertyData), PropertySectionCategory.Settings)]
    public partial class CredentialsPropertySection : VisualElement, IVisualizationWithPropertyData
    {
        private CredentialPanel credentialPanel;
        private ICredentialHandler credentialHandler;
        
        public CredentialsPropertySection()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");   
            
            credentialPanel = this.Q<CredentialPanel>();
            credentialHandler = new CredentialPropertyHandler();
            credentialHandler.OnAuthorizationHandled.AddListener(OnCredentialsHandled);
            credentialHandler.OnAuthorizationUnchanged.AddListener(OnCredentialsHandled);
            credentialPanel.OnConfirmCredentials.AddListener(ApplyCredentials);
            credentialPanel.Show(true);
            
            RegisterCallback<DetachFromPanelEvent>(evt =>
            {
                CredentialPropertyHandler propertyHandler = credentialHandler as CredentialPropertyHandler;
                propertyHandler.Dispose();
            });
        }

        private void ApplyCredentials()
        {
            credentialHandler.UserName = credentialPanel.UserNameField.value;
            credentialHandler.PasswordOrKeyOrTokenOrCode = credentialPanel.CodeField.value;
            credentialHandler.ApplyCredentials();
        }

        private void OnCredentialsHandled(Uri uri, StoredAuthorization auth)
        {
            var accepted = auth != null && auth is not FailedOrUnsupported;
            
            //we always want to show the status if credentials are accepted, however we might still want to display the error of the input panel if it was not accepted
            if (accepted)
                credentialPanel.SetAcceptedState();
            else
                credentialPanel.ResetState();
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            credentialHandler.Uri = properties.Get<LayerURLPropertyData>().Url;
            credentialHandler.ApplyCredentials();
        }
    }
}
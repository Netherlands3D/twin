using System;
using Netherlands3D.Credentials;
using Netherlands3D.Credentials.StoredAuthorization;
using Netherlands3D.Services;
using Netherlands3D.Twin;
using Netherlands3D.UI_Toolkit.Scripts.Panels;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using TextField = Netherlands3D.UI.Components.TextField;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement, InspectorPanel]
    public partial class ImportAssetPanel : BaseInspectorContentPanel
    {
        public override string Title => "Importeren";

        public const string supportedFileTypes = "obj,csv,json,geojson,glb";
        
        private Button uploadButton;
        private Button UploadButton => uploadButton ??= this.Q<Button>("FileUploadButton");

        private Button goToAssetLibraryButton;
        private Button GoToAssetLibraryButton => goToAssetLibraryButton ??= this.Q<Button>("GoToAssetLibraryButton");

        private TextField importUriField;
        private TextField ImportUriField => importUriField ??= this.Q<TextField>("ImportUriField");
        private Button importUriButton;
        private Button ImportUriButton => importUriButton ??= this.Q<Button>("ImportUriButton");

        private ErrorPanel errorPanel;
        private ErrorPanel ErrorPanel => errorPanel ??= this.Q<ErrorPanel>();
        
        public override ToolbarInspector.ToolbarStyle ToolbarStyle => ToolbarInspector.ToolbarStyle.AddLayer;

        private ICredentialHandler credentialHandler = new CredentialPropertyHandler();
        private CredentialPanel credentialPanel;

        public UnityEvent importSucceeded = new();
        public UnityEvent importFailed = new();


        public ImportAssetPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");
            
            GoToAssetLibraryButton.RegisterCallback<ClickEvent>(OnOpenAssetLibrary);
            UploadButton.RegisterCallback<ClickEvent>(OnUploadStarted);
            ImportUriButton.RegisterCallback<ClickEvent>(OnInportUriButtonClicked);
         
            ErrorPanel.Hide();
            credentialPanel = this.Q<CredentialPanel>();
            credentialPanel.SetEnabled(false);
            credentialPanel.OnConfirmCredentials.AddListener(ApplyCredentials);
            credentialHandler.OnAuthorizationHandled.AddListener(HandleCredentials);
            
            //we dont want to show the warning first but immediately start with the input of credentials instead
            credentialPanel.StartWithInput();
            
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                credentialHandler.OnAuthorizationHandled.RemoveListener(HandleCredentials);
            });
            
            ImportUriField.RegisterCallback<NavigationSubmitEvent>(OnSubmit, TrickleDown.TrickleDown);
        }
        
        private void OnSubmit(NavigationSubmitEvent evt)
        {
            OnImport(importUriField.value);
        }
        
        private void ApplyCredentials()
        {
            credentialHandler.UserName = credentialPanel.UserNameField.value;
            credentialHandler.PasswordOrKeyOrTokenOrCode = credentialPanel.CodeField.value;
            credentialHandler.ApplyCredentials();
        }

        private void HandleCredentials(Uri uri, StoredAuthorization auth)
        {
            var accepted = auth != null && auth is not FailedOrUnsupported;
            //we always want to show the status if credentials are accepted, however we might still want to display the error of the input panel if it was not accepted
            if (accepted)
            {
                credentialPanel.SetAcceptedState();
                credentialPanel.Show(false);
                AddLayerFromUrl(uri, auth);
            }
            else
            {
                credentialPanel.Show(true, credentialHandler.PasswordOrKeyOrTokenOrCode);
                if(!string.IsNullOrEmpty(credentialHandler.PasswordOrKeyOrTokenOrCode))
                    credentialPanel.ShowError(true);
            }
        }

        private void OnOpenAssetLibrary(ClickEvent evt) => ServiceLocator.GetService<ToolService>().GetTool(ToolType.AssetLibrary).Open();
        private void OnUploadStarted(ClickEvent evt)
        { 
            ServiceLocator.GetService<FileOpen>().OpenFile(supportedFileTypes);
        }

        private void OnInportUriButtonClicked(ClickEvent evt)
        {
            OnImport(importUriField.value);
        }

        private void OnImport(string value)
        {
            //hide the credential error as we dont want to show this on inputting the uri without credentials when they are required
            credentialPanel.ShowError(false);
            try
            {
                credentialHandler.ClearCredentials();
                Uri uri = new Uri(value);

                credentialHandler.Uri = uri;
                credentialHandler.ApplyCredentials();
            }
            catch (Exception e)
            {
                // TODO: Add better error handling
                Debug.LogException(e);
                ErrorPanel.Show();
            }
        }

        private async void AddLayerFromUrl(Uri uri, StoredAuthorization auth)
        {
            try
            {
                var layers = await App.Layers.AddFromUrl(uri, auth);

                if (layers.Length == 0)
                    Debug.LogWarning("The import of the dataset succeeded, but the dataset is empty and contains no layers");

                ImportUriField.value = string.Empty;
                importSucceeded.Invoke();
                // Hide(); //todo should this be a listener on importSucceeded?
                ServiceLocator.GetService<ToolService>().GetTool(ToolType.AssetImport).Close();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                importFailed.Invoke();
            }
        }
    }
}
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
    [UxmlElement]
    public partial class ImportAssetPanel : BaseInspectorContentPanel
    {
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

        public Action OpenAssetLibrary { get; set; }
        public EventCallback<ClickEvent> FileImportFromUrlStarted { get; set; }

        public EventCallback<ClickEvent> FileUploadStarted { get; set; }

        // public Action<Uri> UriImportStarted { get; set; }
        public Action UriImportFailed { get; set; }

        public override ToolbarInspector.ToolbarStyle ToolbarStyle => ToolbarInspector.ToolbarStyle.AddLayer;

        private ICredentialHandler credentialHandler;
        private CredentialPanel credentialPanel;
        private CredentialPanel CredentialPanel => credentialPanel ??= this.Q<CredentialPanel>();

        public UnityEvent importSucceeded = new();
        public UnityEvent importFailed = new();


        public ImportAssetPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            OnShow += () => EnableInClassList("active", true);
            OnHide += () => EnableInClassList("active", false);
            GoToAssetLibraryButton.RegisterCallback<ClickEvent>(OnOpenAssetLibrary);
            UploadButton.RegisterCallback<ClickEvent>(OnUploadStarted);
            ImportUriButton.RegisterCallback<ClickEvent>(OnInportUriButtonClicked);
            UriImportFailed += ErrorPanel.Show;
            
            CredentialPanel.SetEnabled(false);
            errorPanel.Hide();
        }

        public void SetCredentialHandler(ICredentialHandler handler)
        {
            credentialHandler = handler;
            CredentialPanel.handler = handler;

            credentialHandler.OnAuthorizationHandled.AddListener(HandleCredentials);
        }

        private void HandleCredentials(Uri uri, StoredAuthorization auth)
        {
            if (auth is FailedOrUnsupported)
            {
                credentialPanel.SetEnabled(true);
                return;
            }

            credentialPanel.SetEnabled(false);
            AddLayerFromUrl(uri, auth);
        }

        ~ImportAssetPanel()
        {
            UriImportFailed -= ErrorPanel.Show;
            credentialHandler.OnAuthorizationHandled.RemoveListener(HandleCredentials);
        }

        public override string GetTitle() => "Importeren";

        private void OnOpenAssetLibrary(ClickEvent evt) => OpenAssetLibrary?.Invoke();
        private void OnUploadStarted(ClickEvent evt)
        { 
            ServiceLocator.GetService<FileOpen>().OpenFile(supportedFileTypes);
        }

        private void OnInportUriButtonClicked(ClickEvent evt)
        {
            try
            {
                Uri uri = new Uri(ImportUriField.value);

                credentialHandler.Uri = uri;
                credentialHandler.ApplyCredentials();
            }
            catch (Exception e)
            {
                // TODO: Add better error handling
                Debug.LogException(e);
                UriImportFailed?.Invoke();
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
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                importFailed.Invoke();
            }
        }
    }
}
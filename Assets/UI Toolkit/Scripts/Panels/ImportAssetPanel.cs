using System;
using Netherlands3D.Credentials;
using Netherlands3D.Credentials.StoredAuthorization;
using Netherlands3D.Events;
using Netherlands3D.Services;
using Netherlands3D.Twin;
using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI_Toolkit.Scripts.Panels;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using ListView = Netherlands3D.UI.Components.ListView;
using TextField = Netherlands3D.UI.Components.TextField;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement, InspectorPanel]
    public partial class ImportAssetPanel : BaseInspectorContentPanel
    {
        public override string Title => "Importeren";

        public const string supportedFileTypes = "obj,csv,json,geojson,glb,las";
        
        private Breadcrumb breadcrumb;
        private ListViewItem uploadButton;
        private ListViewItem goToAssetLibraryButton;
        private ListViewItem selectionAreaButton;
        private TextField importUriField;
        private Button importUriButton;
        private ErrorPanel errorPanel;

        private ICredentialHandler credentialHandler = new CredentialPropertyHandler();
        private CredentialPanel credentialPanel;

        public UnityEvent importSucceeded = new();
        public UnityEvent importFailed = new();

        private VisualElement mainSection;
        private SelectionAreaPanel selectionAreaSection;
        
        public ImportAssetPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            //listView = this.Q<ListView>();
            uploadButton = this.Q<ListViewItem>("FileUploadButton");
            goToAssetLibraryButton = this.Q<ListViewItem>("GoToAssetLibraryButton");
            selectionAreaButton = this.Q<ListViewItem>("SelectionAreaButton");
            importUriField = this.Q<TextField>("ImportUriField");
            importUriButton = this.Q<Button>("ImportUriButton");
            errorPanel = this.Q<ErrorPanel>();
            breadcrumb = this.Q<Breadcrumb>();
            breadcrumb.CrumbClicked += OnCrumbClicked;
            mainSection = this.Q<VisualElement>("ImportAssetMainSection");
            selectionAreaSection = this.Q<SelectionAreaPanel>();
            
            goToAssetLibraryButton.RegisterCallback<ClickEvent>(OnOpenAssetLibrary);
            selectionAreaButton.RegisterCallback<ClickEvent>(GoToSelectionAreaButtonClicked);
            uploadButton.RegisterCallback<ClickEvent>(OnUploadStarted);
            importUriButton.RegisterCallback<ClickEvent>(OnInportUriButtonClicked);

            errorPanel.Hide();
            credentialPanel = this.Q<CredentialPanel>();
            credentialPanel.EnableInClassList(UtilityClassConstants.HIDDEN, true);
            credentialPanel.OnConfirmCredentials.AddListener(ApplyCredentials);
            credentialHandler.OnAuthorizationHandled.AddListener(HandleCredentials);
            
            //we dont want to show the warning first but immediately start with the input of credentials instead
            credentialPanel.StartWithInput();

            RegisterCallback<DetachFromPanelEvent>(_ => { credentialHandler.OnAuthorizationHandled.RemoveListener(HandleCredentials); });

            importUriField.RegisterCallback<NavigationSubmitEvent>(OnSubmit, TrickleDown.TrickleDown);

            SetSelectionAreaSectionActive(false);
        }

        private void OnCrumbClicked(int index, Breadcrumb.Crumb crumb)
        {
            switch (index)
            {
                case 1:
                    SetSelectionAreaSectionActive(true);
                    break; 
                default:
                    SetSelectionAreaSectionActive(false);
                    break;
            }
        }

        private void GoToSelectionAreaButtonClicked(ClickEvent evt)
        {
            SetSelectionAreaSectionActive(true);
        }

        private void SetSelectionAreaSectionActive(bool active)
        {
            mainSection.EnableInClassList(UtilityClassConstants.HIDDEN, active);
            selectionAreaSection.EnableInClassList(UtilityClassConstants.HIDDEN, !active);
            
            if(active)
                breadcrumb.AddCrumb("Selectiegebied", selectionAreaSection);
            else
            {
                breadcrumb.ClearCrumbs();
                breadcrumb.AddCrumb("Toevoegen", mainSection);
            }
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
                if (!string.IsNullOrEmpty(credentialHandler.PasswordOrKeyOrTokenOrCode))
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
                errorPanel.Show();
            }
        }

        private async void AddLayerFromUrl(Uri uri, StoredAuthorization auth)
        {
            try
            {
                var layers = await App.Layers.AddFromUrl(uri, auth);

                if (layers.Length == 0)
                    Debug.LogWarning("The import of the dataset succeeded, but the dataset is empty and contains no layers");

                importUriField.value = string.Empty;
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

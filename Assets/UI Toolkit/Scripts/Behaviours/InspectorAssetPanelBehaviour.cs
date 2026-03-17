using System;
using Netherlands3D.Credentials;
using Netherlands3D.Credentials.StoredAuthorization;
using Netherlands3D.Events;
using Netherlands3D.Twin;
using Netherlands3D.UI.Panels;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Behaviours
{
    [RequireComponent(typeof(UIDocument), typeof(CredentialHandler))]
    public class InspectorAssetPanelBehaviour : MonoBehaviour
    {
        [SerializeField] private TriggerEvent uploadFileEvent;

        [SerializeField]
        [Obsolete("Replaced by UriImportStarted")]
        private UnityEvent OpenLegacyFileImportContentPanel;

        private ImportAssetPanel panel;
        
        private CredentialHandler credentialHandler;
        
        public UnityEvent importSucceeded = new();
        public UnityEvent importFailed = new();

        private void Awake()
        {
            panel = GetComponent<UIDocument>()
                .rootVisualElement
                .Q<ImportAssetPanel>();
            
            credentialHandler = GetComponent<CredentialHandler>();
        }

        private void OnEnable()
        {
            panel.FileUploadStarted += OnUploadStarted;
            panel.UriImportStarted += OnUriImportStarted;

            // legacy
            panel.FileImportFromUrlStarted += OnFileImportFromUrlStarted;
          
            credentialHandler.OnAuthorizationHandled.AddListener(ProcessAuthorization);
            importSucceeded.AddListener(OnImportSucceeded);
            importFailed.AddListener(OnImportFailed);
        }

        private void OnDisable()
        {
            panel.FileUploadStarted -= OnUploadStarted;
            panel.UriImportStarted -= OnUriImportStarted;

            // legacy
            panel.FileImportFromUrlStarted -= OnFileImportFromUrlStarted;
            
            credentialHandler.OnAuthorizationHandled.RemoveListener(ProcessAuthorization);
            importSucceeded.RemoveListener(OnImportSucceeded);
            importFailed.RemoveListener(OnImportFailed);
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
            AddLayerFromUrl(uri, auth);
        }

        private async void AddLayerFromUrl(Uri uri, StoredAuthorization auth)
        {
            try
            {
                var layers = await App.Layers.AddFromUrl(uri, auth);

                if (layers.Length == 0)
                    Debug.LogWarning("The import of the dataset succeeded, but the dataset is empty and contains no layers");

                importSucceeded.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                importFailed.Invoke();
            }
        }

        private void OnUploadStarted(ClickEvent evt)
        {
            uploadFileEvent.InvokeStarted();
        }

        private void OnUriImportStarted(Uri uri)
        {
            credentialHandler.SetUri(uri.ToString());
            credentialHandler.ApplyCredentials();
        }
        
        private void OnImportSucceeded()
        {
            panel.Hide();
            Debug.Log("Import Succeeded");
        }

        private void OnImportFailed()
        {
            // optional: keep open or show feedback
            Debug.Log("Import Failed");
        }

        [Obsolete]
        private void OnFileImportFromUrlStarted(ClickEvent evt)
        {
            OpenLegacyFileImportContentPanel?.Invoke();
        }
    }
}

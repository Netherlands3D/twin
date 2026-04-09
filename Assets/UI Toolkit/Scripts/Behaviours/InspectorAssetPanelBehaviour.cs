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
    [RequireComponent(typeof(UIDocument), typeof(CredentialPanelBehaviour))]
    public class InspectorAssetPanelBehaviour : MonoBehaviour
    {
        private UIDocument appDocument;
        [SerializeField] private TriggerEvent uploadFileEvent;

        private VisualElement root;
        private VisualElement Root => root ??= appDocument?.rootVisualElement;
        
        private ImportAssetPanel panel;
        private ImportAssetPanel Panel => panel ??= Root?.Q<ImportAssetPanel>();

        private CredentialPanelBehaviour credentialBehaviour;
        
        public UnityEvent importSucceeded = new();
        public UnityEvent importFailed = new();

        private void Awake()
        {
            appDocument = GetComponent<UIDocument>();
            credentialBehaviour = GetComponent<CredentialPanelBehaviour>();
        }

        private void Start()
        {
            Panel.FileUploadStarted += OnUploadStarted;
            Panel.UriImportStarted += OnUriImportStarted;
            
            importSucceeded.AddListener(OnImportSucceeded);
            importFailed.AddListener(OnImportFailed);
            credentialBehaviour.OnAuthorizationHandled.AddListener(AddLayerFromUrl);
        }

        private void OnDestroy()
        {
            Panel.FileUploadStarted -= OnUploadStarted;
            Panel.UriImportStarted -= OnUriImportStarted;
            
            importSucceeded.RemoveListener(OnImportSucceeded);
            importFailed.RemoveListener(OnImportFailed);
            credentialBehaviour.OnAuthorizationHandled.RemoveListener(AddLayerFromUrl);
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
            credentialBehaviour.SetUri(uri.ToString());
            credentialBehaviour.ApplyCredentials();
        }
        
        private void OnImportSucceeded()
        {
            Panel.Hide();
        }

        private void OnImportFailed()
        {
           
        }
    }
}

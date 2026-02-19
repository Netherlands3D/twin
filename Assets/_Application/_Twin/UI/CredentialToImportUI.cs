using System;
using Netherlands3D.Credentials;
using Netherlands3D.Credentials.StoredAuthorization;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Tools.UI;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D
{
    //This class is the bridge between the CredentialHandler and the "import from URL" UI, since the credentialshandler also exists without this UI when directly instantiating a prefab (e.g. when loading a project)
    //flow:
    //1. User inputs url
    //2. CredentialsHandler checks if Credentials are needed/present (called by button)
    //3a. if yes: pass this to the DataTypeChain
    //3b. if no: set UI so user inputs credentials and go to step 2

    [RequireComponent(typeof(CredentialHandler))]
    public class CredentialToImportUI : MonoBehaviour
    {
        private CredentialHandler handler;
        public UnityEvent importSucceeded = new();
        public UnityEvent importFailed = new();

        [SerializeField] private GameObject credentialsUI;

        private void Awake()
        {
            handler = GetComponent<CredentialHandler>();
        }

        private void OnEnable()
        {
            handler.OnAuthorizationHandled.AddListener(ProcessAuthorization);
        }

        private void OnDisable()
        {
            handler.OnAuthorizationHandled.RemoveListener(ProcessAuthorization);
        }

        private void ProcessAuthorization(Uri uri, StoredAuthorization auth)
        {
            if (auth is FailedOrUnsupported)
            {
                //3b. if no: set UI so user inputs credentials and go to step 2
                SetCredentialsUIActive(true);
                return;
            }

            //3a. if yes: pass this to the Layer service
            SetCredentialsUIActive(false);
            AddLayerFromUrl(uri, auth);
        }

        private async void AddLayerFromUrl(Uri uri, StoredAuthorization auth)
        {
            try
            {
                var layers = await App.Layers.AddFromUrl(uri, auth);
                if (layers != null)
                {
                    if(layers.Length == 0)
                        Debug.LogWarning("The import of the dataset succeeded, but the dataset is empty and contains no layers");
                    
                    importSucceeded.Invoke();
                    return;
                }
                importFailed.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                importFailed.Invoke();
            }
        }

        private void SetCredentialsUIActive(bool enabled)
        {
            credentialsUI.SetActive(enabled);
        }
    }
}
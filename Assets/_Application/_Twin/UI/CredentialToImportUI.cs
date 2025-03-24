using System;
using Netherlands3D.Credentials;
using Netherlands3D.Credentials.StoredAuthorization;
using Netherlands3D.DataTypeAdapters;
using UnityEngine;

namespace Netherlands3D
{
    //This class is the bridge between the CredentialHandler and the "import from URL" UI, since the credentialshandler also exists without this UI when directly instantiating a prefab (e.g. when loading a project)
    //flow:
    //1. User inputs url
    //2. CredentialsHandler checks if Credentials are needed/present (called by button)
    //3a. if yes: pass this to the DataTypeChain
    //3b. if no: set UI so user inputs credentials and go to step 2

    [RequireComponent(typeof(CredentialHandler))]
    [RequireComponent(typeof(DataTypeChain))]
    public class CredentialToImportUI : MonoBehaviour
    {
        private CredentialHandler handler;
        private DataTypeChain chain;

        [SerializeField] private GameObject UrlInputUI;
        [SerializeField] private GameObject credentialsUI;

        private void Awake()
        {
            handler = GetComponent<CredentialHandler>();
            chain = GetComponent<DataTypeChain>();
        }

        private void OnEnable()
        {
            handler.OnAuthorizationHandled.AddListener(ProcessAuthorization);
        }

        private void OnDisable()
        {
            handler.OnAuthorizationHandled.RemoveListener(ProcessAuthorization);
        }

        private void ProcessAuthorization(StoredAuthorization auth)
        {
            if (auth is FailedOrUnsupported)
            {
                //3b. if no: set UI so user inputs credentials and go to step 2
                SetCredentialsUIActive(true);
                return;
            }

            //3a. if yes: pass this to the DataTypeChain
            SetCredentialsUIActive(false);
            chain.DetermineAdapter(auth);
        }

        private void SetCredentialsUIActive(bool enabled)
        {
            credentialsUI.SetActive(enabled);
            UrlInputUI.SetActive(!enabled);
        }
    }
}
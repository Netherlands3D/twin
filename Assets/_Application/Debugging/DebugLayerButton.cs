using System;
using Netherlands3D.Credentials;
using Netherlands3D.Credentials.StoredAuthorization;
using Netherlands3D.DataTypeAdapters;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class DebugLayerButton : MonoBehaviour
    {
        [SerializeField] private string url;
        private CredentialHandler handler;
        private DataTypeChain dataTypeChain;

        private void Start()
        {
            dataTypeChain = GetComponentInParent<DataTypeChain>();
            handler = GetComponentInParent<CredentialHandler>();
        }

        public void Spawn()
        {
            handler.SetUri(url);
            handler.OnAuthorizationHandled.AddListener(DetermineAdapter);
            handler.ApplyCredentials();
        }

        private void DetermineAdapter(Uri uri, StoredAuthorization auth)
        {            
            if(url != uri.ToString())
                return;
                
            dataTypeChain.DetermineAdapter(uri, auth);
            handler.OnAuthorizationHandled.RemoveListener(DetermineAdapter);
        }
    }
}
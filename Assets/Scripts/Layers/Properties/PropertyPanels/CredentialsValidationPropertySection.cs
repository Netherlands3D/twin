using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Twin.Layers.LayerTypes;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class CredentialsValidationPropertySection : MonoBehaviour
    {
        private LayerCredentialsHandler handler;

        [SerializeField] private GameObject validCredentialsPanel;
        [SerializeField] private GameObject invalidCredentialsPanel;

        public LayerCredentialsHandler Handler
        {
            get => handler;
            set
            {
                if (handler)
                    handler.CredentialsAccepted.RemoveListener(OnCredentialsAccepted);

                handler = value;

                OnCredentialsAccepted(handler.HasValidCredentials);

                handler.CredentialsAccepted.AddListener(OnCredentialsAccepted);
            }
        }

        private void OnEnable()
        {
            if (handler)
                handler.CredentialsAccepted.AddListener(OnCredentialsAccepted);
        }

        private void OnDisable()
        {
            if (handler)
                handler.CredentialsAccepted.RemoveListener(OnCredentialsAccepted);
        }

        private void OnCredentialsAccepted(bool accepted)
        {
            validCredentialsPanel.SetActive(accepted);
            invalidCredentialsPanel.SetActive(!accepted);
            if (accepted)
            {
                print("accepted");
            }
            else
                print("rejected");
        }
    }
}
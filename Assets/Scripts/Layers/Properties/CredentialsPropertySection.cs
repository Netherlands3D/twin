using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.UI.LayerInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    public class CredentialsPropertySection : MonoBehaviour
    {
        [SerializeField] private TMP_InputField userNameInputField;
        [SerializeField] private TMP_InputField passwordInputField;
        [SerializeField] private TMP_InputField keyTokenOrCodeInputField;
        [SerializeField] private Button submitCredentialsButton;

        public ILayerWithCredentials Layer { get; set; }

        public enum CredentialType
        {
            None = 0,
            KeyTokenOrCode = 1,
            UsernamePassword = 2
        }

        private CredentialType credentialType = CredentialType.None;

        public void SetCredentialType(int type)
        {
            credentialType = (CredentialType)type;

            if(credentialType == CredentialType.UsernamePassword)
            {
                userNameInputField.transform.parent.gameObject.SetActive(true);
                passwordInputField.transform.parent.gameObject.SetActive(true);
                keyTokenOrCodeInputField.transform.parent.gameObject.SetActive(false);
            }
            else
            {
                userNameInputField.transform.parent.gameObject.SetActive(false);
                passwordInputField.transform.parent.gameObject.SetActive(false);
                keyTokenOrCodeInputField.transform.parent.gameObject.SetActive(true);
            }
        }

        private void OnEnable()
        {
            submitCredentialsButton.onClick.AddListener(HandleCredentialsChange);
        }
        
        private void OnDisable()
        {
            submitCredentialsButton.onClick.RemoveListener(HandleCredentialsChange);
        }

        private void HandleCredentialsChange()
        {
            switch(credentialType)
            {
                case CredentialType.UsernamePassword:
                    Layer.SetCredentials(userNameInputField.text, passwordInputField.text);
                    break;
                case CredentialType.KeyTokenOrCode:
                    Layer.SetKey(keyTokenOrCodeInputField.text);
                    Layer.SetToken(keyTokenOrCodeInputField.text);
                    Layer.SetCode(keyTokenOrCodeInputField.text);
                    break;
            }
        }

        public void HandleServerReturnCode(int errorCode)
        {
            //Give feedback based on the error code
        }
    }
}

using Netherlands3D.Twin.Layers.LayerTypes;
using TMPro;
using UnityEngine;
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

        public CredentialType credentialType = CredentialType.UsernamePassword;

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

        public void SetCredentialType(int type)
        {
            credentialType = (CredentialType)type;
            SetCredentialType(credentialType);
        }

        public void SetCredentialType(CredentialType type)
        {
            credentialType = type;

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

        public void HandleServerReturnCode(int errorCode)
        {
            //Give feedback based on the error code
        }
    }
}

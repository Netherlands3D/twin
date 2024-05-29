using System.Collections;
using Netherlands3D.Twin.Layers.LayerTypes;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
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
            UsernamePassword = 2,
            Key = 3,
            Token = 4,
            Code = 5
        }

        public CredentialType credentialType = CredentialType.UsernamePassword;

        private string url = "";
        public string Url { get => url; set => url = value; }    

        private Coroutine findSpecificTypeCoroutine;    

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
                    if(findSpecificTypeCoroutine != null)
                        StopCoroutine(findSpecificTypeCoroutine);

                    findSpecificTypeCoroutine = StartCoroutine(TryToFindSpecificType());
                    break;
            }
        }

        private IEnumerator TryToFindSpecificType()
        {
            //Try input as token
            var www = new UnityWebRequest(url);
            www.SetRequestHeader("Authorization", "Bearer " + keyTokenOrCodeInputField.text);
            //add key as post variable
            www.method = UnityWebRequest.kHttpVerbGET;

            yield return www.SendWebRequest();
            if(www.result == UnityWebRequest.Result.Success)
            {
                Layer.SetToken(keyTokenOrCodeInputField.text);
                yield break;
            }
            
            //Try input as key. First make sure its not already in url as a query parameter
            if(!url.Contains("?"))
                url += "?key=" + keyTokenOrCodeInputField.text;
            else
                url += "&key=" + keyTokenOrCodeInputField.text;
            

            
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

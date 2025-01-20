using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Netherlands3D.Functionalities.Indicators
{
    public class RemoteTextureLoader : MonoBehaviour
    {
        public UnityEvent<Texture2D> onTextureLoaded = new();

        public void Load(Uri uri)
        {
            StartCoroutine(LoadRemoteTexture(uri.ToString()));
        }

        private IEnumerator LoadRemoteTexture(string textureUrl)
        {
            Debug.Log($"Loading remote texture from {textureUrl}");
            var webRequest = UnityWebRequestTexture.GetTexture(textureUrl, false);
            
            yield return webRequest.SendWebRequest();
            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Could not download {textureUrl}, result code was {webRequest.result}");

                yield break;
            }

            Texture2D myTexture = ((DownloadHandlerTexture)webRequest.downloadHandler).texture;
            myTexture.Compress(false);
            myTexture.wrapMode = TextureWrapMode.Clamp;
            
            Debug.Log($"Successfully loaded remote texture from {textureUrl}");

            onTextureLoaded.Invoke(myTexture);
        }
    }
}
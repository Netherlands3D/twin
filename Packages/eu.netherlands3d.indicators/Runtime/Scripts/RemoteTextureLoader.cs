using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Netherlands3D.Indicators
{
    public class RemoteTextureLoader : MonoBehaviour
    {
        public Texture2D fallbackTexture;

        public UnityEvent<Texture2D> onTextureLoaded = new();

        public void Load(Uri uri)
        {
            // We always load the fallback immediately, and then start downloading
            if (fallbackTexture) onTextureLoaded.Invoke(fallbackTexture);
            
            StartCoroutine(LoadRemoteTexture(uri.ToString()));
        }

        private IEnumerator LoadRemoteTexture(string textureUrl)
        {
            var webRequest = UnityWebRequestTexture.GetTexture(textureUrl, false);
            
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Could not download {textureUrl}");
                yield break;
            }

            Texture2D myTexture = ((DownloadHandlerTexture)webRequest.downloadHandler).texture;
            myTexture.Compress(false);
            myTexture.wrapMode = TextureWrapMode.Clamp;

            onTextureLoaded.Invoke(myTexture);
        }
    }
}
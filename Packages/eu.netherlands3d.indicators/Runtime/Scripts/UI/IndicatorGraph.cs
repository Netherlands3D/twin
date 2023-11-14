using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Netherlands3D.Indicators.UI
{
    [RequireComponent(typeof(Image))]
    public class IndicatorGraph : MonoBehaviour
    {
        private Image image;
        public UnityEvent<Uri> onStartedLoading = new();
        public UnityEvent<Uri> onFailedLoading = new();
        public UnityEvent<Texture> onCompletedLoading = new();
        private Color cachedColor = Color.white;
        private Coroutine coroutine = null;

        private void Awake()
        {
            image = gameObject.GetComponent<Image>();
            cachedColor = image.color;

            onStartedLoading.AddListener(OnStartedLoading);
            onFailedLoading.AddListener(OnFailedLoading);
            onCompletedLoading.AddListener(OnCompletedLoading);
        }

        public void Render(ProjectAreaVisualisation visualisation)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
                CleanUpAfterLoading();
            }

            coroutine = StartCoroutine(DoRender(visualisation));
        }

        private void OnStartedLoading(Uri url)
        {
            // Clear any previous sprite, if loading failed we just do not show anything
            cachedColor = image.color;
            image.sprite = null;
            image.color = Color.clear;
        }

        private void OnFailedLoading(Uri url)
        {
            Debug.Log($"No texture found for graph {url}");
            CleanUpAfterLoading();
        }

        private void OnCompletedLoading(Texture texture)
        {
            Texture2D loadedTexture = (Texture2D)texture;
            image.sprite = Sprite.Create(
                loadedTexture, 
                new Rect(0, 0, loadedTexture.width, loadedTexture.height),
                Vector2.zero
            );
            CleanUpAfterLoading();
        }

        private void CleanUpAfterLoading()
        {
            image.color = cachedColor;
            coroutine = null;
        }

        private IEnumerator DoRender(ProjectAreaVisualisation visualisation)
        {
            yield return null;
            // var url = visualisation.ProjectArea.indicators.graph;
            // onStartedLoading.Invoke(url);
            //
            // using UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url);
            // yield return uwr.SendWebRequest();
            //
            // if (uwr.result != UnityWebRequest.Result.Success)
            // {
            //     onFailedLoading.Invoke(url);
            //     yield break;
            // }
            //
            // onCompletedLoading.Invoke(DownloadHandlerTexture.GetContent(uwr));
        }
    }
}
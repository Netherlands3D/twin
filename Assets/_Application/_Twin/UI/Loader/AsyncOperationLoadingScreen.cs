using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Netherlands3D.Twin.UI.Loader
{
    public class AsyncOperationLoadingScreen : MonoBehaviour
    {
        [SerializeField] private UI_ProgressIndicator progressIndicator;
        [SerializeField] private TMP_Text labelField;
        [SerializeField] private float fadeDuration = 0.4f;
        [SerializeField] private string overlayCanvasTag = "ScreenOverlayCanvas";

        private AsyncOperationHandle handle;

        public AsyncOperationHandle Handle
        {
            set
            {
                transform.SetParent(GameObject.FindWithTag(overlayCanvasTag)?.transform, false);
                progressIndicator.ShowProgress(0.1f);

                handle = value;
                handle.Completed += OnCompleted;
            }
        }

        public string Label
        {
            get => labelField.text;
            set => labelField.text = value;
        }

        private void OnCompleted(AsyncOperationHandle obj)
        {
            // The handle is not always destroyed - let's remove this event listener to be sure
            obj.Completed -= OnCompleted;

            progressIndicator.ShowProgress(0.99f);
            progressIndicator.GetComponent<CanvasGroup>()
                .DOFade(0, fadeDuration)
                .OnComplete(() =>
                {
                    Destroy(gameObject);
                });
        }

        private void Update()
        {
            // Prevent the progress indicator from being 0 or 1, because that will trigger a side-effect in 
            // UI_ProgressIndicator
            progressIndicator.ShowProgress(Mathf.Clamp(handle.PercentComplete, 0.01f, 0.99f));
        }
    }
}
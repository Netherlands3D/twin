using DG.Tweening;
using Netherlands3D.Services;
using System;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.UI
{
    public class ViewerModulebar : MonoBehaviour
    {
        private RectTransform rect;

        [SerializeField] private RectTransform underBar;
        private float underBarYSize;
        [SerializeField] private RectTransform contentParent;
        [SerializeField] private RectTransform parentRect;

        private bool isOpen;
        private bool isAnimating;
        private Sequence currentSequence;
        private ViewerModuleButton currentTool;

        public event Action<ViewerModuleButton> OnViewerToolChanged;

        private void Start()
        {
            rect = GetComponent<RectTransform>();

            underBarYSize = underBar.sizeDelta.y;

            ServiceLocator.GetService<FirstPersonViewer>().OnViewerExited.AddListener(ViewerExited);
        }

        private void OnDestroy()
        {
            ServiceLocator.GetService<FirstPersonViewer>()?.OnViewerExited.RemoveListener(ViewerExited);
        }

        public void OpenWindow(RectTransform windowPrefab, ViewerModuleButton viewTool)
        {
            if (isAnimating) return;
            if (windowPrefab != null) isAnimating = true;

            currentSequence?.Kill();
            currentSequence = DOTween.Sequence();

            if (isOpen)
            {
                currentSequence.Append(rect.DOAnchorPosY(-rect.sizeDelta.y - 5, .5f).SetEase(Ease.InSine));

                currentSequence.AppendCallback(() =>
                {
                    foreach (Transform t in contentParent)
                    {
                        Destroy(t.gameObject);
                    }
                });
                currentSequence.AppendInterval(Time.deltaTime);
            }

            //Switch
            if (viewTool != currentTool)
            {
                if (windowPrefab != null)
                {
                    currentSequence.AppendCallback(() =>
                    {
                        RectTransform windowPanel = Instantiate(windowPrefab, contentParent);

                        windowPanel.anchoredPosition = new Vector2(windowPanel.anchoredPosition.x, -rect.sizeDelta.y);
                    });

                    underBar.DOAnchorPosY(0, .4f).SetEase(Ease.OutSine);

                    currentSequence.AppendInterval(Time.deltaTime);
                    currentSequence.Append(rect.DOAnchorPosY(65, .5f)).SetEase(Ease.OutSine);
                }

                currentTool = viewTool;
                isOpen = true;
            }
            else
            {
                isOpen = false;
                currentTool = null;
                underBar.DOAnchorPosY(-underBarYSize, .5f).SetEase(Ease.InSine);
            }

            OnViewerToolChanged?.Invoke(currentTool);
            currentSequence.OnComplete(() =>
            {
                isAnimating = false;
                parentRect.sizeDelta = new Vector2(323, 65) + new Vector2(0, rect.sizeDelta.y);
            });
            currentSequence.Play();
        }

        private void ViewerExited(bool modified)
        {
            OpenWindow(null, currentTool);
        }
    }
}

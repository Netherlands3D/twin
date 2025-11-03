using DG.Tweening;
using Netherlands3D.Twin.UI;
using System;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.UI
{
    public class ViewerModulebar : MonoBehaviour
    {
        private RectTransform rect;
        private ContentFitterRefresh contentFilterRefresh; //TWIN Dependent

        [SerializeField] private RectTransform underBar;
        private float underBarYSize;
        [SerializeField] private RectTransform contentParent;

        private bool isOpen;
        private bool isAnimating;
        private Sequence currentSequence;
        private ViewerModuleButton currentTool;


        public event Action<ViewerModuleButton> OnViewerToolChanged;

        private void Start()
        {
            rect = GetComponent<RectTransform>();
            contentFilterRefresh = GetComponent<ContentFitterRefresh>(); //TWIN Dependent

            underBarYSize = underBar.sizeDelta.y;

            FirstPersonViewer.OnViewerExited += ViewerExited;
        }

        private void OnDestroy()
        {
            FirstPersonViewer.OnViewerExited -= ViewerExited;
        }

        public void OpenWindow(RectTransform windowPrefab, ViewerModuleButton viewTool)
        {
            if (isAnimating) return;
            if(windowPrefab != null) isAnimating = true;

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
                    contentFilterRefresh.RefreshContentFitters(); //TWIN Dependent
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

                        contentFilterRefresh.RefreshContentFitters(); //TWIN Dependent

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
            currentSequence.OnComplete(() => isAnimating = false);
            currentSequence.Play();
        }

        private void ViewerExited(bool modified)
        {
            OpenWindow(null, currentTool);
        }
    }
}

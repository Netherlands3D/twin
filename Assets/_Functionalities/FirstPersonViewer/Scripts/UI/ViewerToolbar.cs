using DG.Tweening;
using Netherlands3D.Twin.UI;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.FirstPersonViewer.UI
{
    public class ViewerToolbar : MonoBehaviour
    {
        private RectTransform rect;
        private ContentFitterRefresh contentFilterRefresh; //TWIN Dependent

        [SerializeField] private RectTransform contentParent;

        private bool isOpen;
        private bool isAnimating;
        private Sequence currentSequence;
        private ViewerTool currentTool;


        public event Action<ViewerTool> OnViewerToolChanged;

        private void Start()
        {
            rect = GetComponent<RectTransform>();
            contentFilterRefresh = GetComponent<ContentFitterRefresh>(); //TWIN Dependent
        }

        public void OpenWindow(RectTransform windowPrefab, ViewerTool viewTool)
        {
            if (isAnimating) return;
            isAnimating = true;

            currentSequence?.Kill();
            currentSequence = DOTween.Sequence();

            if (isOpen)
            {
                currentSequence.Append(rect.DOAnchorPosY(-rect.sizeDelta.y - 5, .75f).SetEase(Ease.InSine));

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
                currentSequence.AppendCallback(() =>
                {
                    RectTransform windowPanel = Instantiate(windowPrefab, contentParent);

                    contentFilterRefresh.RefreshContentFitters(); //TWIN Dependent

                    windowPanel.anchoredPosition = new Vector2(windowPanel.anchoredPosition.x, -rect.sizeDelta.y);

                });

                currentSequence.AppendInterval(Time.deltaTime);
                currentSequence.Append(rect.DOAnchorPosY(56, .75f)).SetEase(Ease.OutSine);

                currentTool = viewTool;

                isOpen = true;
            }
            else
            {
                isOpen = false;
                currentTool = null;
            }

            OnViewerToolChanged?.Invoke(currentTool);
            currentSequence.OnComplete(() => isAnimating = false);
            currentSequence.Play();
        }
    }
}

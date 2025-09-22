using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.FirstPersonViewer.UI
{
    public class ViewerToolbar : MonoBehaviour
    {
        private RectTransform rect;
        private bool isOpen;

        private RectTransform opendPrefab;
        [SerializeField] private RectTransform contentParent;

        private bool isAnimating;
        private Sequence currentSequence;

        private void Start()
        {
            rect = GetComponent<RectTransform>();
        }

        public void OpenWindow(RectTransform windowPrefab)
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
                });
            }

            //Switch
            if (windowPrefab != opendPrefab)
            {
                currentSequence.AppendCallback(() =>
                {
                    RectTransform windowPanel = Instantiate(windowPrefab, contentParent);

                    LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent);
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                    Canvas.ForceUpdateCanvases();

                    windowPanel.anchoredPosition = new Vector2(windowPanel.anchoredPosition.x, -rect.sizeDelta.y);

                });

                currentSequence.AppendInterval(Time.deltaTime);
                currentSequence.Append(rect.DOAnchorPosY(56, .75f)).SetEase(Ease.OutSine);

                opendPrefab = windowPrefab;

                isOpen = true;
            }
            else
            {
                isOpen = false;
                opendPrefab = null;
            }

            currentSequence.OnComplete(() => isAnimating = false);
            currentSequence.Play();
        }
    }
}

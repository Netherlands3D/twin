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

        private void Start()
        {
            rect = GetComponent<RectTransform>();
        }

        public void OpenWindow(RectTransform windowPrefab)
        {
            Sequence openSequence = DOTween.Sequence();

            if (isOpen)
            {
                openSequence.Append(rect.DOAnchorPosY(-rect.sizeDelta.y - 5, 1f));

                openSequence.AppendCallback(() =>
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
                openSequence.AppendCallback(() => Instantiate(windowPrefab, contentParent));
                openSequence.AppendInterval(0f);
                openSequence.AppendCallback(() =>
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent);
                    Canvas.ForceUpdateCanvases();
                    transform.localPosition = new Vector3(-135, -rect.sizeDelta.y, 0);
                });
                openSequence.Append(rect.DOAnchorPosY(56, 1f));

                opendPrefab = windowPrefab;
            }
            else
            {
                isOpen = false;
                opendPrefab = null;
            }

            openSequence.Play();
            isOpen = true;
        }
    }
}

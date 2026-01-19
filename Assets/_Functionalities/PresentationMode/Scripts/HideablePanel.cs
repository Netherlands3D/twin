using DG.Tweening;
using Netherlands3D.Services;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.PresentationModus.UIHider
{
    public class HideablePanel : MonoBehaviour, IPanelHider
    {
        private RectTransform rt;

        private bool pinned;
        public bool Pinned => pinned;

        private bool isHidden;
        public bool IsHidden => isHidden;

        [SerializeField] private Toggle pinToggle;

        [SerializeField] private Vector2 rectSize;

        [Header("Animation")]
        [SerializeField] private UIExitDirection exitDirection;
        [SerializeField] private float duration = .25f;
        [SerializeField] private Ease easeType = Ease.InOutSine;
        [SerializeField] private float offscreenMargin;
        [SerializeField, Tooltip("Buffer reveal size")] private float bufferInPixels = 50;
        private Tween currentTween;

        private Vector2 shownPosition;
        private Vector2 hiddenPosition;

        private void Awake()
        {
            rt = GetComponent<RectTransform>();
            shownPosition = rt.anchoredPosition;

            if (pinToggle != null)
            {
                pinToggle.onValueChanged.AddListener(TogglePin);
                pinToggle.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            ServiceLocator.GetService<UIHider>()?.Register(this);

            if (hiddenPosition == Vector2.zero)
            {
                StartCoroutine(SetupElement());
            }
        }

        private IEnumerator SetupElement()
        {
            //When the rectsize is not set. Use the size delta instead.
            if (rectSize == Vector2.zero)
            {
                //We need to wait 1 frame for the rect calculation.
                yield return new WaitForEndOfFrame();
                rectSize = rt.sizeDelta;
            }

            CalculateHiddenPos();
        }

        private void OnDisable()
        {
            ServiceLocator.GetService<UIHider>()?.Unregister(this);
        }

        private void CalculateHiddenPos()
        {
            Vector2 offset = exitDirection switch
            {
                UIExitDirection.Left => Vector2.left * (rectSize.x - rt.anchoredPosition.x + offscreenMargin),
                UIExitDirection.Right => Vector2.right * (rectSize.x + rt.anchoredPosition.x - offscreenMargin),
                UIExitDirection.Up => Vector2.up * (rectSize.y - rt.anchoredPosition.y + offscreenMargin),
                UIExitDirection.Down => Vector2.down * (rectSize.y + rt.anchoredPosition.y - offscreenMargin),
                _ => Vector2.zero
            };

            hiddenPosition = shownPosition + offset;
        }

        private void Update()
        {
            if (rt.hasChanged)
            {
                rt.hasChanged = false;
                CalculateHiddenPos();
            }
        }

        public void HideUI(bool hideUI)
        {
            if (pinToggle != null) pinToggle.gameObject.SetActive(hideUI);
        }

        public void Show()
        {
            currentTween?.Kill();
            isHidden = false;

            currentTween = rt.DOAnchorPos(shownPosition, duration).SetEase(easeType);
        }

        public void Hide()
        {
            currentTween?.Kill();
            isHidden = true;

            currentTween = rt.DOAnchorPos(hiddenPosition, duration).SetEase(easeType);
        }

        public void TogglePin(bool enable) => pinned = enable;

        public bool IsMouseOver(Vector2 mousePos)
        {
            //We move the object to get an accurate calculation.
            Vector2 original = rt.anchoredPosition;
            rt.anchoredPosition = shownPosition;

            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);

            rt.anchoredPosition = original;

            Rect rect = new Rect(
                corners[0].x - bufferInPixels,
                corners[0].y - bufferInPixels,
                corners[2].x - corners[0].x + bufferInPixels * 2,
                corners[2].y - corners[0].y + bufferInPixels * 2
            );

            return rect.Contains(mousePos);
        }
    }
}

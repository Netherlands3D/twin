using DG.Tweening;
using Netherlands3D.Coordinates;
using Netherlands3D.Services;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Netherlands3D.FirstPersonViewer.UI
{
    public class FirstPersonOverlay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private FirstPersonViewer firstPersonViewer;

        [SerializeField] private InputActionReference openOverlayButton;
        [SerializeField] private TextMeshProUGUI overlayText;

        [Header("Copy Button")]
        [SerializeField] private CanvasGroup copyButtonGroup;
        private Coordinate currentCoordinates;
        [SerializeField] private string copySnackbarText;
        [SerializeField] private UnityEvent<string> onShowCopyText;

        private void Start()
        {
            firstPersonViewer = ServiceLocator.GetService<FirstPersonViewer>();

            openOverlayButton.action.performed += ToggleOverlay;
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            openOverlayButton.action.performed -= ToggleOverlay;
        }

        private void ToggleOverlay(InputAction.CallbackContext context)
        {
            bool wasActive = gameObject.activeSelf;
            gameObject.SetActive(!wasActive);

            if (!wasActive) firstPersonViewer.OnPositionUpdated += UpdateInfoMenu;
            else firstPersonViewer.OnPositionUpdated -= UpdateInfoMenu;
        }

        private void UpdateInfoMenu(Coordinate playerCoords)
        {
            currentCoordinates = playerCoords;

            overlayText.text =
                $"Coordinaten: {playerCoords.ToString()}\n" +
                $"Hoogte t.o.v. NAP: {playerCoords.value3.ToString("F2")}m\n" +
                $"<i><size=14>Hoogtedata is een benadering en kan afwijken van de werkelijkheid.</i>";
        }

        public void CopyCoordinates()
        {
            onShowCopyText.Invoke(copySnackbarText);
            WebGLClipboard.Copy(currentCoordinates.ToString());
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            copyButtonGroup.alpha = 0;
            copyButtonGroup.gameObject.SetActive(true);

            copyButtonGroup.DOKill();
            copyButtonGroup.DOFade(1, .5f);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            copyButtonGroup.DOKill();
            copyButtonGroup.DOFade(0, .5f).OnComplete(() => copyButtonGroup.gameObject.SetActive(false));
        }
    }
}

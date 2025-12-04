using DG.Tweening;
using GG.Extensions;
using Netherlands3D.Coordinates;
using Netherlands3D.Events;
using Netherlands3D.Services;
using Netherlands3D.Twin.Samplers;
using System.Windows.Forms;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Netherlands3D.FirstPersonViewer.UI
{
    public class FirstPersonOverlay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private FirstPersonViewer firstPersonViewer;
        private HeightMap heightMap;

        [SerializeField] private InputActionReference openOverlayButton;
        [SerializeField] private TextMeshProUGUI overlayText;

        [Header("Copy Button")]
        [SerializeField] private CanvasGroup copyButtonGroup;
        private Coordinate currentCoordinates;
        [SerializeField] private StringEvent snackbarEvent;
        [SerializeField] private string copySnackbarText;

        private void Start()
        {
            firstPersonViewer = ServiceLocator.GetService<FirstPersonViewer>();
            heightMap = ServiceLocator.GetService<HeightMap>();

            openOverlayButton.action.performed += ToggleOverlay;
            gameObject.SetActive(false);
            
            //For some weird reason Unity keeps reseting the set anchor. So we set it with code.
            RectTransform rect = copyButtonGroup.RectTransform();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(45, -5);
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


        private void UpdateInfoMenu(Coordinate playerCoords, float groundPos)
        {
            Coordinate rdNapCoords = playerCoords.Convert(CoordinateSystem.RDNAP);
            currentCoordinates = rdNapCoords;

            float heightMapValue = heightMap.GetHeight(rdNapCoords, true);

            float napHeight = Mathf.Round(heightMapValue * 100f) / 100f;

            float dstToGround = Mathf.Round(((float)rdNapCoords.value3 - heightMapValue) * 100f) / 100f;
            float dstToObject = Mathf.Round((rdNapCoords.ToUnity().y - groundPos) * 100f) / 100f;

            overlayText.text =
                $"Coördinaten: {rdNapCoords.ToString()}\n" +
                $"Afstand tot Grond: {dstToGround}m\n" +
                $"Afstand tot Object: {dstToObject}m\n" +
                $"NAP Hoogte: {napHeight}m\n" +
                $"<i><size=10>Hoogtedata is een geschatte benadering en kan afwijken van de werkelijkheid.</i>";
        }

        public void CopyCoordinates()
        {
            snackbarEvent.InvokeStarted(copySnackbarText);
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

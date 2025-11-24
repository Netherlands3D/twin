using Netherlands3D.Coordinates;
using Netherlands3D.Services;
using Netherlands3D.Twin.Samplers;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Netherlands3D.FirstPersonViewer.UI
{
    public class FirstPersonOverlay : MonoBehaviour
    {
        private FirstPersonViewer firstPersonViewer;
        private HeightMap heightMap;

        [SerializeField] private InputActionReference openOverlayButton;
        [SerializeField] private TextMeshProUGUI overlayText;

        private void Start()
        {
            firstPersonViewer = ServiceLocator.GetService<FirstPersonViewer>();
            heightMap = ServiceLocator.GetService<HeightMap>();

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


        private void UpdateInfoMenu(Coordinate playerCoords, float groundPos)
        {
            float heightMapValue = heightMap.GetHeight(playerCoords, true);

            float napHeight = Mathf.Round(heightMapValue * 100f) / 100f;

            float dstToGround = Mathf.Round(((float)playerCoords.value3 - heightMapValue) * 100f) / 100f;
            float dstToObject = Mathf.Round((playerCoords.ToUnity().y - groundPos) * 100f) / 100f;

            overlayText.text =
                $"Coordinaten: {playerCoords.Convert(CoordinateSystem.RDNAP).ToString()}\n" +
                $"Gelezen hoogtekaart: {heightMapValue}\n" +
                $"Afstand Grond: {dstToGround}m\n" +
                $"Afstand tot Object: {dstToObject}m\n" +
                $"NAP Hoogte: {napHeight + .6f}m\n" +
                $"<i><size=10>Data is een geschatte benadering en kan afwijken van de werkelijkheid.</i>";
        }
    }
}

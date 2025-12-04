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
            Coordinate rdNapCoords = playerCoords.Convert(CoordinateSystem.RDNAP);

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
    }
}

using Netherlands3D.FirstPersonViewer.UI;
using Netherlands3D.FirstPersonViewer.ViewModus;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.FirstPersonViewer.UI
{
    public class MovementModusButton : MonoBehaviour
    {
        private MovementPresets movementPreset;
        private MovementModusSwitcher movementSwitcher;

        [SerializeField] private Image movementIcon;
        [SerializeField] private TextMeshProUGUI movementText;

        public void SetupButton(MovementPresets preset, MovementModusSwitcher switcher)
        {
            movementPreset = preset;
            movementSwitcher = switcher;
            movementIcon.sprite = preset.viewIcon;
            movementText.text = preset.viewName;
        }

        public void ButtonClicked()
        {
            movementSwitcher.LoadMoveModus(movementPreset);
            movementSwitcher.SetMovementVisible();
        }
    }
}

using Netherlands3D.FirstPersonViewer.UI;
using Netherlands3D.FirstPersonViewer.ViewModus;
using Netherlands3D.Twin.UI.Tooltips;
using System.Collections.Generic;
using System.Windows.Forms;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.FirstPersonViewer.UI
{
    public class MovementModusButton : MonoBehaviour
    {
        private MovementPresets movementPreset;
        private ViewerSettings viewerSettings;

        [SerializeField] private List<Image> movementIcons;

        [Header("Button State")]
        [SerializeField] private GameObject regular;
        [SerializeField] private GameObject selected;

        public void Init(MovementPresets preset, ViewerSettings viewerSettings)
        {
            movementPreset = preset;
            this.viewerSettings = viewerSettings;

            movementIcons.ForEach(i => i.sprite = preset.viewIcon);

            GetComponent<TooltipTrigger>().TooltipText = preset.viewName;
        }

        public void ButtonClicked()
        {
            //movementSwitcher.LoadMoveModus(movementPreset);
            //movementSwitcher.SetMovementVisible();
        }
    }
}

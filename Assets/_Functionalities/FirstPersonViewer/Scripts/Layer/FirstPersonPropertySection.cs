using Netherlands3D.FirstPersonViewer.ViewModus;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.Properties;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.Layer
{
    [PropertySection(typeof(FirstPersonLayerPropertyData))]
    public class FirstPersonPropertySection : MonoBehaviour, IVisualizationWithPropertyData
    {
        private FirstPersonLayerPropertyData firstPersonData;

        [SerializeField] private TMP_Dropdown modusDropdown;

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            firstPersonData = properties.Get<FirstPersonLayerPropertyData>();
            if (firstPersonData == null) return;

            InitalizeDropdown(firstPersonData.MovementID);
        }

        private void InitalizeDropdown(int selectedID)
        {
            MovementModusSwitcher switcher = ServiceLocator.GetService<FirstPersonViewer>().MovementSwitcher;

            int optionIndex = 0;
            List<string> moveOptions = new List<string>();
            for (int i = 0; i < switcher.MovementPresets.Count; i++)
            {
                ViewerState modus = switcher.MovementPresets[i];

                if (modus.id == selectedID) optionIndex = i;
                moveOptions.Add(modus.viewName);
            }

            modusDropdown.AddOptions(moveOptions);
            modusDropdown.SetValueWithoutNotify(optionIndex);
            modusDropdown.onValueChanged.AddListener(OnMovementModeChanged);
        }

        private void OnMovementModeChanged(int index)
        {
            MovementModusSwitcher switcher = ServiceLocator.GetService<FirstPersonViewer>().MovementSwitcher;
            ViewerState state = switcher.MovementPresets[index];

            firstPersonData.SetMovementID(state.id);
            CreateSettings(state);
        }

        private void CreateSettings(ViewerState state)
        {

        }


    }
}

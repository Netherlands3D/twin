using Netherlands3D.FirstPersonViewer.ViewModus;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons.Properties;
using Netherlands3D.Twin.Layers.Properties;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.Layer
{
    public class FirstPersonPropertySection : PropertySectionWithLayerGameObject
    {
        [SerializeField] private TMP_Dropdown modusDropdown;

        private LayerGameObject layer;
        public override LayerGameObject LayerGameObject
        {
            get => layer;
            set => Initialize(value);
        }

        private void Initialize(LayerGameObject layer)
        {
            this.layer = layer;

            List<string> moveOptions = new List<string>();
            foreach (ViewerState modus in ServiceLocator.GetService<FirstPersonViewer>().MovementSwitcher.MovementPresets)
            {
                moveOptions.Add(modus.viewName);
            }

            modusDropdown.AddOptions(moveOptions);
            modusDropdown.onValueChanged.AddListener(OnMovementModeChanged);
        }

        private void OnMovementModeChanged(int index)
        {
            //LayerGameObject.
        }




    }
}

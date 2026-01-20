using Netherlands3D.Coordinates;
using Netherlands3D.FirstPersonViewer.ViewModus;
using Netherlands3D.Services;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Netherlands3D.Twin.Layers.Properties;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.Layers
{
    public class FirstPersonViewerLayerSave : MonoBehaviour
    {
        [SerializeField] private LayerGameObject prefab;

        public void SaveLayer()
        {
            FirstPersonViewer fpv = ServiceLocator.GetService<FirstPersonViewer>();
            Dictionary<string, object> settings = new Dictionary<string, object>();
            foreach (ViewerSetting setting in fpv.MovementSwitcher.CurrentMovement.editableSettings.list)
            {
                if (!setting.isVisible) continue;
                if (setting is ViewerSettingLabel label) continue;

                settings.Add(setting.GetSettingName(), setting.GetValue());
            }

            Vector3 rotationToSave = fpv.FirstPersonCamera.GetStateRotation();

            LayerPropertyData[] propertiesToAdd = {
                new TransformLayerPropertyData(new Coordinate(fpv.transform.position), rotationToSave, fpv.transform.localScale),
                new FirstPersonLayerPropertyData(fpv.MovementSwitcher.CurrentMovement.id, settings)
            };

            ILayerBuilder layerBuilder = LayerBuilder.Create().OfType(prefab.PrefabIdentifier).NamedAs("First person positie").AddProperties(propertiesToAdd);
            App.Layers.Add(layerBuilder);
        }
    }
}

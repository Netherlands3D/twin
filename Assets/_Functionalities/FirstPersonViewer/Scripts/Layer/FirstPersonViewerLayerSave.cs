using Netherlands3D.Coordinates;
using Netherlands3D.FirstPersonViewer.ViewModus;
using Netherlands3D.Services;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Services;
using System.Collections;
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

            //Using this weird way of doing things. Because PositionedAt and Rotated aren't working.
            prefab.SpawnLocation = SpawnLocation.PrefabPosition;
            prefab.transform.position = fpv.transform.position;
            prefab.transform.rotation = fpv.transform.rotation;

            Dictionary<string, object> settings = new Dictionary<string, object>();
            foreach (ViewerSetting setting in fpv.MovementSwitcher.CurrentMovement.editableSettings.list)
            {
                if (!setting.isVisible) continue;
                if (setting is ViewerSettingLabel label) continue;

                settings.Add(setting.GetSettingName(), setting.GetValue());
            }

            LayerPropertyData[] propertiesToAdd = {
                new TransformLayerPropertyData(new Coordinate(fpv.transform.position), fpv.transform.eulerAngles, fpv.transform.localScale),
                new FirstPersonLayerPropertyData(fpv.MovementSwitcher.CurrentMovement.id, settings)
            };

            ILayerBuilder layerBuilder = LayerBuilder.Create().OfType(prefab.PrefabIdentifier).NamedAs("First person positie").AddProperties(propertiesToAdd);
            Layer layer = App.Layers.Add(layerBuilder);
            StartCoroutine(ResetPrefab());
        }

        //Fix for resetting layer prefab, because resetting it too early will still set the values. 
        private IEnumerator ResetPrefab()
        {
            yield return new WaitForEndOfFrame();
            prefab.SpawnLocation = SpawnLocation.OpticalCenter;
            prefab.transform.position = Vector3.zero;
            prefab.transform.rotation = Quaternion.identity;
        }
    }
}

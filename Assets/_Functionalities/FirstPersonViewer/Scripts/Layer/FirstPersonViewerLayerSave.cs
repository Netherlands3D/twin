using Netherlands3D.Coordinates;
using Netherlands3D.FirstPersonViewer.ViewModus;
using Netherlands3D.Services;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
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

            ILayerBuilder layerBuilder = LayerBuilder.Create().OfType(prefab.PrefabIdentifier).NamedAs("First person positie");
            layerBuilder.WhenBuilt(LayerCallback);
            Layer layer = App.Layers.Add(layerBuilder);
        }

        private void LayerCallback(LayerData layerData)
        {
            FirstPersonViewer fpv = ServiceLocator.GetService<FirstPersonViewer>();

            layerData.SetProperty<TransformLayerPropertyData>(new TransformLayerPropertyData(new Coordinate(fpv.transform.position), fpv.transform.eulerAngles, fpv.transform.localScale));

            Dictionary<string, object> settings = new Dictionary<string, object>();
            foreach (ViewerSetting setting in fpv.MovementSwitcher.CurrentMovement.editableSettings.list)
            {
                if (!setting.isVisible) continue;
                if (setting is ViewerSettingLabel label) continue;

                settings.Add(setting.GetSettingName(), setting.GetValue());
            }

            layerData.SetProperty<FirstPersonLayerPropertyData>(new FirstPersonLayerPropertyData(fpv.MovementSwitcher.CurrentMovement.id, settings));

            StartCoroutine(ResetPrefab(layerData));
        }

        //Fix for resetting layer, because setting it in LayerCallback will do it too early. 
        private IEnumerator ResetPrefab(LayerData layerData)
        {
            yield return new WaitForEndOfFrame();
            prefab.SpawnLocation = SpawnLocation.OpticalCenter;
            prefab.transform.position = Vector3.zero;
            prefab.transform.rotation = Quaternion.identity;
        }
    }
}

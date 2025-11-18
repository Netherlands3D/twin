using Netherlands3D.Coordinates;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.Layer
{
    public class FirstPersonCameraLayerGameObject : HierarchicalObjectLayerGameObject
    {
        private FirstPersonLayerPropertyData firstPersonPropertyData => LayerData.GetProperty<FirstPersonLayerPropertyData>();

        protected override void InitializePropertyData()
        {
            if (firstPersonPropertyData != null) return;

            LayerData.SetProperty(
               new FirstPersonLayerPropertyData(
                   new Coordinate(transform.position),
                   transform.eulerAngles,
                   transform.localScale
               )
           );
        }

        protected override void OnDoubleClick(LayerData layer)
        {
            FirstPersonViewer fpv = ServiceLocator.GetService<FirstPersonViewer>();
            fpv.transform.position = transform.position;
            fpv.transform.rotation = transform.rotation;
            fpv.OnViewerEntered?.Invoke();
        }
    }
}

using Netherlands3D.Services;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.Layer
{
    public class FirstPersonCameraLayerGameObject : HierarchicalObjectLayerGameObject
    {
        protected override void OnDoubleClick(LayerData layer)
        {
            FirstPersonViewer fpv = ServiceLocator.GetService<FirstPersonViewer>();
            fpv.transform.position = transform.position;
            fpv.transform.rotation = transform.rotation;
            fpv.OnViewerEntered?.Invoke();
        }
    }
}

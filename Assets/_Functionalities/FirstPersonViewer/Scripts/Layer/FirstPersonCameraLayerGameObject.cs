using Netherlands3D.Coordinates;
using Netherlands3D.FirstPersonViewer.ViewModus;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Tools;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.Layer
{
    public class FirstPersonCameraLayerGameObject : HierarchicalObjectLayerGameObject
    {
        [SerializeField] private Tool layerTool;
        private FirstPersonLayerPropertyData firstPersonPropertyData => LayerData.GetProperty<FirstPersonLayerPropertyData>();

        //public override bool IsMaskable => false;

        //protected override void InitializePropertyData()
        //{
        //    if (firstPersonPropertyData != null) return;

        //    LayerData.SetProperty(
        //       new FirstPersonLayerPropertyData(
        //           new Coordinate(transform.position),
        //           transform.eulerAngles,
        //           transform.localScale
        //       )
        //   );
        //}

        protected override void OnDoubleClick(LayerData layer)
        {
            layerTool.CloseInspector();

            FirstPersonViewer fpv = ServiceLocator.GetService<FirstPersonViewer>();
            
            //ViewerState viewerState = fpv.MovementSwitcher.MovementPresets.Find(m => m.viewName == firstPersonPropertyData.MovementName);
            ViewerState  viewerState = fpv.MovementSwitcher.MovementPresets[0];

            fpv.transform.position = transform.position;
            fpv.transform.rotation = transform.rotation;
            fpv.OnViewerEntered?.Invoke(viewerState);
        }
    }
}

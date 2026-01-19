using Netherlands3D.Coordinates;
using Netherlands3D.FirstPersonViewer.ViewModus;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Tools;
using System.Collections.Generic;
using Netherlands3D.Twin.Utility;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.Layers
{
    public class FirstPersonCameraLayerGameObject : HierarchicalObjectLayerGameObject
    {
        [SerializeField] private Tool layerTool;
        [SerializeField] private MovementFloatSetting heightSetting;
        private FirstPersonLayerPropertyData firstPersonPropertyData => LayerData.GetProperty<FirstPersonLayerPropertyData>();
        public override BoundingBox Bounds => new BoundingBox(new Bounds(transform.position + Vector3.up*heightSetting.Value/2, new Vector3(0.3f, heightSetting.Value, 0.3f)));
        
        public override void LoadProperties(List<LayerPropertyData> properties)
        {
            InitProperty<TransformLayerPropertyData>(properties, null, new Coordinate(transform.position), transform.eulerAngles,  transform.localScale, "%");
            InitProperty<FirstPersonLayerPropertyData>(properties);
        }

        protected override void OnDoubleClick(LayerData layer)
        {
            layerTool.CloseInspector();

            FirstPersonViewer fpv = ServiceLocator.GetService<FirstPersonViewer>();
            
            ViewerState viewerState = fpv.MovementSwitcher.MovementPresets.Find(m => m.id == firstPersonPropertyData.MovementID);

            fpv.transform.position = transform.position;
            fpv.transform.rotation = transform.rotation;
            fpv.EnterViewer(viewerState, firstPersonPropertyData.settingValues);
        }
    }
}

using System.Collections.Generic;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D.Functionalities.UrbanReLeaf
{
    [RequireComponent(typeof(SensorProjectionLayer))]
    public class SensorLayerGameObject : CartesianTileLayerGameObject, ILayerWithPropertyData, ILayerWithPropertyPanels
    {
        private SensorProjectionLayer SensorProjectionLayer { get; set; }

        public LayerPropertyData PropertyData => URLPropertyData;

        private LayerURLPropertyData URLPropertyData => LayerData.GetProperty<LayerURLPropertyData>();

        protected override void OnLayerInitialize()
        {
            SensorProjectionLayer = GetComponent<SensorProjectionLayer>();

            base.OnLayerInitialize();
        }

        protected override void OnLayerReady()
        {
            LayerData.LayerOrderChanged.AddListener(SetRenderOrder);
            SetRenderOrder(LayerData.RootIndex);
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
        }

        //a higher order means rendering over lower indices
        private void SetRenderOrder(int order)
        {
            //we have to flip the value because a lower layer with a higher index needs a lower render index
            SensorProjectionLayer.RenderIndex = -order;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            LayerData.LayerOrderChanged.RemoveListener(SetRenderOrder);
        }
    }
}
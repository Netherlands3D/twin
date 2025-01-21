using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles;
using Netherlands3D.Twin.Layers.Properties;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Netherlands3D.Functionalities.UrbanReLeaf
{
    public class SensorLayerGameObject : CartesianTileLayerGameObject, ILayerWithPropertyData, ILayerWithPropertyPanels
    {
        public SensorProjectionLayer SensorProjectionLayer => sensorProjectionLayer;

        public LayerPropertyData PropertyData => urlPropertyData;
        protected LayerURLPropertyData urlPropertyData = new();

        private SensorProjectionLayer sensorProjectionLayer;

        private List<IPropertySectionInstantiator> propertySections = new();

        public List<IPropertySectionInstantiator> GetPropertySections()
        {
            propertySections = GetComponents<IPropertySectionInstantiator>().ToList();
            return propertySections;
        }

        protected override void Awake()
        {
            base.Awake();
            sensorProjectionLayer = GetComponent<SensorProjectionLayer>();
        }

        protected override void Start()
        {
            base.Start();           
            LayerData.LayerOrderChanged.AddListener(SetRenderOrder);
            SetRenderOrder(LayerData.RootIndex);
        }

        //a higher order means rendering over lower indices
        public void SetRenderOrder(int order)
        {
            //we have to flip the value because a lower layer with a higher index needs a lower render index
            SensorProjectionLayer.RenderIndex = -order;
        }

        public virtual void LoadProperties(List<LayerPropertyData> properties)
        {
            var urlProperty = (LayerURLPropertyData)properties.FirstOrDefault(p => p is LayerURLPropertyData);
            if (urlProperty != null)
            {
                this.urlPropertyData = urlProperty;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            LayerData.LayerOrderChanged.RemoveListener(SetRenderOrder);
        }
    }
}
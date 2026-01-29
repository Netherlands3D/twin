using System;
using System.Collections.Generic;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject
{
    public class GroundPlaneLayerGameObject : HierarchicalObjectLayerGameObject
    {
        public const int dimension = 10000;
        public const float height = -200;
        
        protected override void OnVisualizationInitialize()
        {
            base.OnVisualizationInitialize();
            transform.localScale = Vector3.one * dimension; 
            transform.position = Vector3.up * height;
        }

        public override void LoadProperties(List<LayerPropertyData> properties)
        {
            InitProperty<ColorPropertyData>(properties);
        }

        protected override void Update()
        {
            //override the transform propertydata
            //base.Update();
        }
    }
}
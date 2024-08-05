using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin
{
    public class PolygonPropertySectionInstantiator : MonoBehaviour, IPropertySectionInstantiator
    {
        public PolygonSelectionLayer PolygonLayer
        {
            get;
            set;
        }
        
        public void AddToProperties(RectTransform properties)
        {
            if (!PolygonInputToLayer.PolygonPropertySectionPrefab) return;

            var settings = Instantiate(PolygonInputToLayer.PolygonPropertySectionPrefab, properties);
            settings.PolygonLayer = PolygonLayer;
        }
    }
}

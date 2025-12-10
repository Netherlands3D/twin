using System.Collections.Generic;
using Netherlands3D.Functionalities.ObjectLibrary;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D.Twin.layers.properties
{
    //[RequireComponent(typeof(LayerGameObject))]
    public class HiddenObject : MonoBehaviour, IVisualizationWithPropertyData
    {
        public void LoadProperties(List<LayerPropertyData> properties)
        {
            GetComponent<LayerGameObject>().InitProperty<HiddenObjectsPropertyData>(properties);
        }
    }
}
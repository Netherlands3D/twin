using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Functionalities.ObjectLibrary;
using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D.Twin.layers.properties
{
    [RequireComponent(typeof(LayerGameObject))]
    public class HiddenObject : MonoBehaviour, IVisualizationWithPropertyData
    {
        public void LoadProperties(List<LayerPropertyData> properties)
        {
            LayerGameObject visualization = GetComponent<LayerGameObject>();
            visualization.InitProperty<HiddenObjectsPropertyData>(properties);
            
            //BC
            visualization.ConvertOldStylingDataIntoProperty(properties, HiddenObjectsPropertyData.VisibilityIdentifier, visualization.LayerData.GetProperty<HiddenObjectsPropertyData>());
        }
    }
}
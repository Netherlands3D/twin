using System.Collections.Generic;
using Netherlands3D.Functionalities.ObjectLibrary;
using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D.Twin.layers.properties
{
    [RequireComponent(typeof(LayerGameObject))]
    public class CartesianTileLayerFeatureColoring : MonoBehaviour, IVisualizationWithPropertyData
    {
        public void LoadProperties(List<LayerPropertyData> properties)
        {
            LayerGameObject visualization = GetComponent<LayerGameObject>();
            visualization.InitProperty<LayerFeatureColorPropertyData>(properties);
            
            //BC
            visualization.ConvertOldStylingDataIntoProperty(properties, LayerFeatureColorPropertyData.ColoringIdentifier, visualization.LayerData.GetProperty<LayerFeatureColorPropertyData>());
        }
    }
}
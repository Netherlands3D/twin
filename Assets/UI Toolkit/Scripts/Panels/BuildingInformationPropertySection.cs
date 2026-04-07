using System.Collections.Generic;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles.Properties;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    [PropertySection(typeof(BuildingPropertyData))]
    public partial class BuildingInformationPropertySection : VisualElement, IVisualizationWithPropertyData
    {
        private BuildingPropertyData buildingPropertyData;

        public BuildingInformationPropertySection()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            buildingPropertyData = properties.Get<BuildingPropertyData>();
         
        }
    }
}